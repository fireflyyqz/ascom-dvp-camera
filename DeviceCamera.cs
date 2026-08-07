// All lines from line 1 to the device interface implementation region will be discarded by the project wizard when the template is used
// Required code must lie within the device implementation region
// The //ENDOFINSERTEDFILE tag must be the last but one line in this file

using ASCOM;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.DeviceInterface;
using ASCOM.MyCamera;
using ASCOM.Utilities;
using DVPCameraType;
using MyCameraAPI;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

[ComVisible(true)]
[Guid("3332BFDC-4560-4A14-71E1-51A0D79B52A6")]
[ClassInterface(ClassInterfaceType.None)]
[ProgId("ASCOM.DVPCamera.Camera")]
public class DeviceCamera:ICameraV4
{

    Util util = new Util();
    TraceLogger tl = new TraceLogger();

    #region ICamera Implementation

    private const int ccdWidth = 4088; // Constants to define the CCD pixel dimensions
    private const int ccdHeight = 3072;
    private const double pixelSize = 3.1; // Constant for the pixel physical dimension

    private int cameraNumX = ccdWidth; // Initialise variables to hold values required for functionality tested by Conform
    private int cameraNumY = ccdHeight;
    private int cameraStartX = 0;
    private int cameraStartY = 0;
    private DateTime exposureStart = DateTime.MinValue;
    private double cameraLastExposureDuration = 1.0;
    private bool cameraImageReady = false;
    private int[,] cameraImageArray;
    private object[,] cameraImageArrayVariant;
    int[,] currentImageArray;
    //-----------------------------
    uint cameraHandle;
    public bool IsOnline = false;
    public CameraConfig cameraConfig;
    private CameraCapture camera = new CameraCapture();//图像缓存读取容器
    string cameraName = "USB3_12M@UB122001229";//相机参数配置，传感器为GRBG类型，相对RGGB有1像素水平偏移，4088,3072分辨率，3.1um像元尺寸
    short binmode = 1;//normal模式
    CameraStates cameraState= CameraStates.cameraError;
    short currentPercent = 0;


    //---------------------
    internal static string driverID = "ASCOM.DVPCamera.Camera";
    private static string driverDescription = "ASCOM Camera Driver for MyCamera.";
    internal static string comPortProfileName = "COM Port"; // Constants used for Profile persistence
    internal static string comPortDefault = "COM1";
    internal static string traceStateProfileName = "Trace Level";
    internal static string traceStateDefault = "false";
    internal static string comPort; // Variables to hold the current device configuration
    private bool connectedState;
    private Util utilities;
    private AstroUtils astroUtilities;
    public ImageMatrix currentFrame=null;


    public DeviceCamera()
    {
        System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"{DateTime.Now}: DeviceCamera callback\n");
        tl = new TraceLogger("", "MyCamera");
        ReadProfile();
        tl.LogMessage("MyCamera", "Starting initialisation");
        connectedState = false;
        utilities = new Util();
        astroUtilities = new AstroUtils();
        tl.LogMessage("MyCamera", "Completed initialisation");
    }

    //获取ms级时间戳
    public static Int64 GetTimeStamp()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        var ret = Convert.ToInt64(ts.TotalMilliseconds); // 毫秒级时间戳
        return ret;
    }
    Int64 lastTimeStamp = GetTimeStamp();

    public bool ConnectCamera(bool connect)
    {
        if (connect)
        {
            try
            {
                System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"{DateTime.Now}: ConnectCamera callback\n");
                dvpStatus status = DVPCamera.dvpOpenByName(cameraName, dvpOpenMode.OPEN_NORMAL, ref cameraHandle);//相机打开
                if (status != dvpStatus.DVP_STATUS_OK)
                {
                    return false;
                }
                cameraConfig = new CameraConfig(cameraHandle);//相机配置函数
                Thread.Sleep(100);
                bool defaultConfig = cameraConfig.DefaultConfig();//启用默认参数
                cameraConfig.SetResolutionModeSel(0);//设置normal分辨率模式
                Thread.Sleep(50);
                cameraConfig.SetSourceFormatSel(1);//设置raw-12格式
                Thread.Sleep(50);
                cameraConfig.SetTargetFormatSel(2);//设置raw-16格式
                Thread.Sleep(50);
                cameraConfig.StartExposure();
                cameraConfig.SetExposureTime(1000000);//设置曝光时间为1s
                cameraState = CameraStates.cameraIdle;
                IsOnline =true;
                connectedState=true;

                return true;
            }
            catch (Exception)
            {
                return false;
            }   
        }
        else
        {
            try
            {
                dvpStatus status = DVPCamera.dvpClose(cameraHandle);
                if (status != dvpStatus.DVP_STATUS_OK)
                {
                    return false;
                }
                cameraState = CameraStates.cameraError;
                IsOnline = false;
                connectedState=false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public bool CheckConnection()
    {
        DVPCamera.dvpIsOnline(cameraHandle,ref IsOnline);
        connectedState = IsOnline;
        return IsOnline;
    }

    public void AbortExposure()
    {   
        cameraImageReady = false;
        IsOnline = CheckConnection();
        if (!IsOnline)
        {
            tl.LogMessage("AbortExposure", "disconnected");
            cameraState = CameraStates.cameraError;
            throw new MethodNotImplementedException("camera disconnected");
        }
        try
        {
            cameraState = CameraStates.cameraIdle;
        }
        catch (Exception)
        {
            tl.LogMessage("AbortExposure", "error");
            throw new MethodNotImplementedException("AbortExposure error");
        }
        
    }

    public short BayerOffsetX
    {
        get
        {
            return 1;
        }
    }

    public short BayerOffsetY
    {
        get
        {
            return 0;
        }
    }

    public short BinX
    {
        get
        {
            tl.LogMessage("BinX Get", "1");
            return binmode;
        }
        set
        {
            tl.LogMessage("BinX Set", value.ToString());
            switch (value)//0 - 6范围，对应normal bin2 sum2 bin3 sum3 bin4 sum4
            {
                case 1://bin1
                    binmode = 1;
                    try { cameraConfig.SetResolutionModeSel(0); }
                    catch (Exception) { }
                    break;
                case 2://bin2
                    binmode = 2;
                    try { cameraConfig.SetResolutionModeSel(1); }
                    catch (Exception) { }
                    break;
                case 3://bin3
                    binmode = 4;
                    try { cameraConfig.SetResolutionModeSel(3); }
                    catch (Exception) {  }
                    break;
                case 4://bin4
                    binmode = 4;
                    try { cameraConfig.SetResolutionModeSel(5); }
                    catch (Exception) {  }
                    break;
                default:
                    throw new ASCOM.InvalidValueException("BinX", value.ToString(), "out of range:1, 2, 3, 4");
            }
        }
    }

    public short BinY
    {
        get
        {
            tl.LogMessage("BinY Get", "1");
            return binmode;
        }
        set
        {
            tl.LogMessage("BinY Set", value.ToString());
            if (value!=binmode)
            {
                switch (value)//0 - 6范围，对应normal bin2 sum2 bin3 sum3 bin4 sum4
                {
                    case 1://bin1
                        binmode = 1;
                        try { cameraConfig.SetResolutionModeSel(0); }
                        catch (Exception) {  }
                        break;
                    case 2://bin2
                        binmode = 2;
                        try { cameraConfig.SetResolutionModeSel(1); }
                        catch (Exception) {  }
                        break;
                    case 3://bin3
                        binmode = 4;
                        try { cameraConfig.SetResolutionModeSel(3); }
                        catch (Exception) {  }
                        break;
                    case 4://bin4
                        binmode = 4;
                        try { cameraConfig.SetResolutionModeSel(5); }
                        catch (Exception) {  }
                        break;
                    default:
                        throw new ASCOM.InvalidValueException("BinY", value.ToString(), "out of range:1, 2, 3, 4");
                }
            }
        }
    }

    public double CCDTemperature
    {
        get
        {
            tl.LogMessage("CCDTemperature Get", "not implemented");
            return 20.0;
        }
    }

    public CameraStates CameraState
    {
        get
        {
            tl.LogMessage("CameraState Get", cameraState.ToString());
            return cameraState;
        }
    }

    public int CameraXSize
    {
        get
        {
            tl.LogMessage("CameraXSize Get", ccdWidth.ToString());
            return ccdWidth;
        }
    }

    public int CameraYSize
    {
        get
        {
            tl.LogMessage("CameraYSize Get", ccdHeight.ToString());
            return ccdHeight;
        }
    }

    //停止曝光，丢弃当前图像数据
    public bool CanAbortExposure
    {
        get
        {
            tl.LogMessage("CanAbortExposure Get", true.ToString());
            return false;
        }
    }
    //非对称binning
    public bool CanAsymmetricBin
    {
        get
        {
            tl.LogMessage("CanAsymmetricBin Get", false.ToString());
            return false;
        }
    }

    public bool CanFastReadout
    {
        get
        {
            tl.LogMessage("CanFastReadout Get", false.ToString());
            return false;
        }
    }

    public bool CanGetCoolerPower
    {
        get
        {
            tl.LogMessage("CanGetCoolerPower Get", false.ToString());
            return false;
        }
    }

    public bool CanPulseGuide
    {
        get
        {
            tl.LogMessage("CanPulseGuide Get", false.ToString());
            return false;
        }
    }

    public bool CanSetCCDTemperature
    {
        get
        {
            tl.LogMessage("CanSetCCDTemperature Get", false.ToString());
            return false;
        }
    }

    public bool CanStopExposure
    {
        get
        {
            tl.LogMessage("CanStopExposure Get", false.ToString());
            return false;
        }
    }

    public bool CoolerOn
    {
        get
        {
            tl.LogMessage("CoolerOn Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("CoolerOn", false);
        }
        set
        {
            tl.LogMessage("CoolerOn Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("CoolerOn", true);
        }
    }

    public double CoolerPower
    {
        get
        {
            tl.LogMessage("CoolerPower Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("CoolerPower", false);
        }
    }

    public double ElectronsPerADU
    {
        get
        {
            tl.LogMessage("ElectronsPerADU Get", "implemented");
            return 1.0;
        }
    }

    public double ExposureMax
    {
        get
        {
            IsOnline = CheckConnection();
            if (!IsOnline)
            {
                tl.LogMessage("ExposureMax Get", "disconnected");
                cameraState = CameraStates.cameraError;
                throw new ASCOM.PropertyNotImplementedException("ExposureMax", false);
            }
            dvpDoubleDescr exposureDescr = cameraConfig.GetExposureDescr();
            tl.LogMessage("ExposureMax Get", "implemented");

            //return (exposureDescr.fMax)/1000000.0;
            return 600;
        }
    }

    public double ExposureMin
    {
        get
        {
            IsOnline = CheckConnection();
            if (!IsOnline)
            {
                tl.LogMessage("ExposureMin Get", "disconnected");
                cameraState = CameraStates.cameraError;
                throw new ASCOM.PropertyNotImplementedException("ExposureMin", false);
            }
            dvpDoubleDescr exposureDescr = cameraConfig.GetExposureDescr();
            tl.LogMessage("ExposureMin Get", "implemented");
            return exposureDescr.fMin/1000000.0;
        }
    }

    public double ExposureResolution
    {
        get
        {
            IsOnline = CheckConnection();
            if (!IsOnline)
            {
                tl.LogMessage("ExposureResolution Get", "disconnected");
                cameraState = CameraStates.cameraError;
                throw new ASCOM.PropertyNotImplementedException("ExposureResolution", false);
            }
            dvpDoubleDescr exposureDescr = cameraConfig.GetExposureDescr();
            tl.LogMessage("ExposureResolution Get", "implemented");
            return exposureDescr.fStep / 1000000.0;
        }
    }

    public bool FastReadout
    {
        get
        {
            tl.LogMessage("FastReadout Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("FastReadout", false);
        }
        set
        {
            tl.LogMessage("FastReadout Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("FastReadout", true);
        }
    }

    public double FullWellCapacity
    {
        get
        {
            tl.LogMessage("FullWellCapacity Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("FullWellCapacity", false);
        }
    }

    public short Gain
    {
        get
        {
            IsOnline = CheckConnection();
            if (!IsOnline)
            {
                tl.LogMessage("Gain Get", "disconnected");
                cameraState = CameraStates.cameraError;
                throw new ASCOM.PropertyNotImplementedException("Gain", false);
            }
            dvpFloatDescr gainDescr = cameraConfig.GetGainDescr();
            float rawGain = (cameraConfig.GetGain() - gainDescr.fMin) / gainDescr.fStep;
            tl.LogMessage("Gain Get", "implemented");
            return (short)rawGain;
        }
        set
        {
            IsOnline = CheckConnection();
            if (!IsOnline)
            {
                tl.LogMessage("Gain Set", "disconnected");
                cameraState = CameraStates.cameraError;
                throw new ASCOM.PropertyNotImplementedException("Gain", false);
            }
            dvpFloatDescr gainDescr = cameraConfig.GetGainDescr();
            float gainVal = ((float)value) * gainDescr.fStep + gainDescr.fMin;
            System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"{DateTime.Now}: gain set {gainVal} callback\n");
            cameraConfig.SetGain(gainVal);
            tl.LogMessage("Gain Set", "implemented");
        }
    }

    public short GainMax
    {
        get
        {
            IsOnline = CheckConnection();
            if (!IsOnline) {
                tl.LogMessage("GainMax Get", "disconnected");
                cameraState = CameraStates.cameraError;
                throw new ASCOM.PropertyNotImplementedException("GainMax", false);
            }
            dvpFloatDescr gainDescr = cameraConfig.GetGainDescr();
            short gainMax = (short)((gainDescr.fMax - gainDescr.fMin) / gainDescr.fStep);
            tl.LogMessage("GainMax Get", "not implemented");
            //throw new ASCOM.PropertyNotImplementedException("GainMax", false);
            return gainMax;
        }
    }

    public short GainMin
    {
        get
        {
            IsOnline = CheckConnection();
            if (!IsOnline)
            {
                tl.LogMessage("GainMin Get", "disconnected");
                cameraState = CameraStates.cameraError;
                throw new ASCOM.PropertyNotImplementedException("GainMin", false);
            }
            dvpFloatDescr gainDescr = cameraConfig.GetGainDescr();
            tl.LogMessage("GainMin Get", "not implemented");
            //throw new ASCOM.PropertyNotImplementedException("GainMin", false);
            return 1;
        }
    }

    public ArrayList Gains
    {
        get
        {
            //tl.LogMessage("Gains Get", "implemented");
            //throw new ASCOM.PropertyNotImplementedException("Gains", false);
            IsOnline = CheckConnection();
            if (!IsOnline)
            {
                tl.LogMessage("Gains Get", "disconnected");
                cameraState = CameraStates.cameraError;
                throw new ASCOM.PropertyNotImplementedException("Gains", false);
            }
            dvpFloatDescr gainDescr = cameraConfig.GetGainDescr();
            ArrayList gainsList = new ArrayList();
            for (int gain = 1; gain <= (int)((gainDescr.fMax - gainDescr.fMin) / gainDescr.fStep); gain += 1)
            {
                gainsList.Add(gain.ToString());
            }
            tl.LogMessage("Gains Get", "implemented");
            return gainsList;
        }
    }

    public bool HasShutter
    {
        get
        {
            tl.LogMessage("HasShutter Get", false.ToString());
            return false;
        }
    }

    public double HeatSinkTemperature
    {
        get
        {
            tl.LogMessage("HeatSinkTemperature Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("HeatSinkTemperature", false);
        }
    }

    public object ImageArray
    {
        get
        {
            System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"{DateTime.Now}: ImageArray callback\n");
            if (!cameraImageReady)
            {
                tl.LogMessage("ImageArray Get", "Throwing InvalidOperationException because of a call to ImageArray before the first image has been taken!");
                throw new ASCOM.InvalidOperationException("Call to ImageArray before the first image has been taken!");
            }
            cameraImageArray = new int[cameraNumX, cameraNumY];
            cameraImageArray = currentImageArray;
            cameraState = CameraStates.cameraIdle;
            cameraImageReady = false;
            currentImageArray = null;
            return cameraImageArray;
        }
    }

    public object ImageArrayVariant
    {
        get
        {
            System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"{DateTime.Now}: ImageArrayVariant callback\n");
            if (!cameraImageReady)
            {
                tl.LogMessage("ImageArrayVariant Get", "Throwing InvalidOperationException because of a call to ImageArrayVariant before the first image has been taken!");
                throw new ASCOM.InvalidOperationException("Call to ImageArrayVariant before the first image has been taken!");
            }
            cameraImageArrayVariant = new object[cameraNumX, cameraNumY];
            for (int i = 0; i < cameraImageArray.GetLength(1); i++)
            {
                for (int j = 0; j < cameraImageArray.GetLength(0); j++)
                {
                    cameraImageArrayVariant[j, i] = cameraImageArray[j, i];
                }
            }
            return cameraImageArrayVariant;
        }
    }

    public bool ImageReady
    {
        get
        {
            if ((cameraState == CameraStates.cameraWaiting)&&!cameraImageReady)
            {
                short currentPercent = (short)((100 * GetTimeStamp() - 100 * lastTimeStamp) / ((long)(cameraLastExposureDuration * 1000)));
                System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"{DateTime.Now}: ImageReady check {currentPercent} callback\n");
                if (currentPercent >= 100) 
                {
                    if (currentFrame.Data2D != null) 
                    {
                        currentImageArray = new int[cameraNumX, cameraNumY];
                        currentImageArray= currentFrame.Data2D;
                        cameraState = CameraStates.cameraReading;
                        currentFrame=null;
                        cameraImageReady = true;
                        
                    }
                }
            }
            tl.LogMessage("ImageReady Get", cameraImageReady.ToString());
            return cameraImageReady;
        }
    }

    public bool IsPulseGuiding
    {
        get
        {
            tl.LogMessage("IsPulseGuiding Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("IsPulseGuiding", false);
        }
    }

    public double LastExposureDuration
    {
        get
        {
            if (!cameraImageReady)
            {
                tl.LogMessage("LastExposureDuration Get", "Throwing InvalidOperationException because of a call to LastExposureDuration before the first image has been taken!");
                throw new ASCOM.InvalidOperationException("Call to LastExposureDuration before the first image has been taken!");
            }
            tl.LogMessage("LastExposureDuration Get", cameraLastExposureDuration.ToString());
            return cameraLastExposureDuration;
        }
    }

    public string LastExposureStartTime
    {
        get
        {
            if (!cameraImageReady)
            {
                tl.LogMessage("LastExposureStartTime Get", "Throwing InvalidOperationException because of a call to LastExposureStartTime before the first image has been taken!");
                throw new ASCOM.InvalidOperationException("Call to LastExposureStartTime before the first image has been taken!");
            }
            string exposureStartString = exposureStart.ToString("yyyy-MM-ddTHH:mm:ss");
            tl.LogMessage("LastExposureStartTime Get", exposureStartString.ToString());
            return exposureStartString;
        }
    }

    public int MaxADU
    {
        get
        {
            tl.LogMessage("MaxADU Get", "65535");
            return 65535;
        }
    }

    public short MaxBinX
    {
        get
        {
            tl.LogMessage("MaxBinX Get", "1");
            return 4;
        }
    }

    public short MaxBinY
    {
        get
        {
            tl.LogMessage("MaxBinY Get", "1");
            return 4;
        }
    }

    public int NumX
    {
        get
        {
            switch (binmode)
            {
                case 1:
                    cameraNumX = ccdWidth;
                    break;
                case 2:
                    cameraNumX = ccdWidth / 2;
                    break;
                case 3:
                    cameraNumX = ccdWidth / 3;
                    break;
                case 4:
                    cameraNumX = ccdWidth / 4;
                    break;
            }
            tl.LogMessage("NumX Get", cameraNumX.ToString());
            return cameraNumX;
        }
        set
        {
            cameraNumX = value;
            tl.LogMessage("NumX set", value.ToString());
            throw new ASCOM.InvalidValueException("NumY setting unsupport");
        }
    }

    public int NumY
    {
        get
        {
            switch (binmode)
            {
                case 1:
                    cameraNumY = ccdHeight;
                    break;
                case 2:
                    cameraNumY = ccdHeight / 2;
                    break;
                case 3:
                    cameraNumY = ccdHeight / 3;
                    break;
                case 4:
                    cameraNumY = ccdHeight / 4;
                    break;
            }
            tl.LogMessage("NumY Get", cameraNumY.ToString());
            return cameraNumY;
        }
        set
        {
            cameraNumY = value;
            tl.LogMessage("NumY set", value.ToString());
            throw new ASCOM.InvalidValueException("NumY setting unsupport");
        }
    }

    public int Offset
    {
        get
        {
            tl.LogMessage("Offset Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("Offset", false);
        }
        set
        {
            tl.LogMessage("Offset Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("Offset", true);
        }
    }

    public int OffsetMax
    {
        get
        {
            tl.LogMessage("OffsetMax Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("OffsetMax", false);
        }
    }

    public int OffsetMin
    {
        get
        {
            tl.LogMessage("OffsetMin Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("OffsetMin", true);
        }
    }

    public ArrayList Offsets
    {
        get
        {
            tl.LogMessage("Offsets Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("Offsets", true);
        }
    }

    public short PercentCompleted
    {
        get
        {
            short Percent = (short)((100 * GetTimeStamp() - 100 * lastTimeStamp) / ((long)(cameraLastExposureDuration * 1000)));
            
            tl.LogMessage("PercentCompleted Get", "implemented");
            System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"{DateTime.Now}: PercentCompleted {Percent} callback\n");
       
            return Percent;
        }
    }

    public double PixelSizeX
    {
        get
        {
            tl.LogMessage("PixelSizeX Get", pixelSize.ToString());
            return pixelSize;
        }
    }

    public double PixelSizeY
    {
        get
        {
            tl.LogMessage("PixelSizeY Get", pixelSize.ToString());
            return pixelSize;
        }
    }

    public void PulseGuide(GuideDirections Direction, int Duration)
    {
        tl.LogMessage("PulseGuide", "Not implemented");
        throw new ASCOM.MethodNotImplementedException("PulseGuide");
    }

    public short ReadoutMode
    {
        get
        {
            tl.LogMessage("ReadoutMode Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("ReadoutMode", false);
        }
        set
        {
            tl.LogMessage("ReadoutMode Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("ReadoutMode", true);
        }
    }

    public ArrayList ReadoutModes
    {
        get
        {
            tl.LogMessage("ReadoutModes Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("ReadoutModes", false);
        }
    }

    public string SensorName
    {
        get
        {
            tl.LogMessage("SensorName Get", "Not implemented");
            return "unknown cmos";
        }
    }

    public SensorType SensorType
    {
        get
        {
            tl.LogMessage("SensorType Get", "implemented");
            return SensorType.RGGB;
        }
    }

    public double SetCCDTemperature
    {
        get
        {
            tl.LogMessage("SetCCDTemperature Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SetCCDTemperature", false);
        }
        set
        {
            tl.LogMessage("SetCCDTemperature Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SetCCDTemperature", true);
        }
    }

    public void StartExposure(double Duration, bool Light)
    {
        if (Duration < 0.0) throw new InvalidValueException("StartExposure", Duration.ToString(), "0.0 upwards");
        if (cameraNumX > ccdWidth) throw new InvalidValueException("StartExposure", cameraNumX.ToString(), ccdWidth.ToString());
        if (cameraNumY > ccdHeight) throw new InvalidValueException("StartExposure", cameraNumY.ToString(), ccdHeight.ToString());
        if (cameraStartX > ccdWidth) throw new InvalidValueException("StartExposure", cameraStartX.ToString(), ccdWidth.ToString());
        if (cameraStartY > ccdHeight) throw new InvalidValueException("StartExposure", cameraStartY.ToString(), ccdHeight.ToString());
        currentFrame=null;
        IsOnline = CheckConnection();
        if (!IsOnline)
        {
            tl.LogMessage("StopExposure", "disconnected");
            cameraState = CameraStates.cameraError;
            throw new MethodNotImplementedException("StopExposure");
        }
        System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"{DateTime.Now}: StartExposure callback\n");
        cameraImageReady =false;
        cameraState = CameraStates.cameraWaiting;
        System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"{DateTime.Now}: SetExposureTime callback\n");
        
        exposureStart = DateTime.Now;
        lastTimeStamp = GetTimeStamp();//记录开始的时间戳
        currentFrame = cameraConfig.StartMultiExposure(Duration);
        if (Duration >= 10) 
        {
            Duration = ((int)Duration / 10) * 10;
        }
        cameraLastExposureDuration = Duration;
        tl.LogMessage("StartExposure", Duration.ToString() + " " + Light.ToString());
    }

    public int StartX
    {
        get
        {
            tl.LogMessage("StartX Get", cameraStartX.ToString());
            return cameraStartX;
        }
        set
        {
            cameraStartX = 0;
            tl.LogMessage("StartX Set", value.ToString());
            throw new ASCOM.InvalidValueException("StartX setting unsupport");
        }
    }

    public int StartY
    {
        get
        {
            tl.LogMessage("StartY Get", cameraStartY.ToString());
            return cameraStartY;
        }
        set
        {
            cameraStartY = 0;
            tl.LogMessage("StartY set", value.ToString());
            throw new ASCOM.InvalidValueException("StartY setting unsupport");
        }
    }

    public void StopExposure()
    {
        IsOnline = CheckConnection();
        if (!IsOnline)
        { 
            tl.LogMessage("StopExposure", "disconnected");
            cameraState = CameraStates.cameraError;
            throw new MethodNotImplementedException("StopExposure");
        }
        System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"{DateTime.Now}: StopExposure callback\n");
    }

    public string Action(string ActionName, string ActionParameters)
        => throw new ASCOM.ActionNotImplementedException("Action未实现");
    public void CommandBlind(string Command, bool Raw = false)
        => throw new ASCOM.MethodNotImplementedException("CommandBlind");
    public bool CommandBool(string Command, bool Raw = false)
    => throw new ASCOM.MethodNotImplementedException("CommandBool");
    public string CommandString(string Command, bool Raw = false)
    => throw new ASCOM.MethodNotImplementedException("CommandString");
    public void Dispose() {}
    public void Connect()
    {
        ConnectCamera(true);
        //System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"Connect callback\n");
    }

    public void Disconnect()
    {
        ConnectCamera(false);
        //System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"Disconnect callback\n");
    }

    public double SubExposureDuration
    {
        get
        {
            tl.LogMessage("SubExposureDuration Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SubExposureDuration", false);
        }
        set
        {
            tl.LogMessage("SubExposureDuration Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SubExposureDuration", true);
        }
    }

    public string Description => "ASCOM MyCamera Camera Driver";
    public string DriverInfo => "MyCameraDriver64";
    public string DriverVersion => "1.0";
    public short InterfaceVersion => 2;
    public string Name => "MyDVPCamera";
    public ArrayList SupportedActions => new ArrayList();
    //public bool Connecting => throw new System.NotImplementedException();
    public bool Connecting => throw new System.NotImplementedException();

    public bool Connected
    {
        get
        {
            LogMessage("Connected", "Get {0}", IsConnected);
            //System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"Get Connect {IsConnected.ToString()} callback\n");
            return IsConnected;
        }
        set
        {
            tl.LogMessage("Connected", "Set {0}", value);
            if (value == IsConnected)
                return;

            if (value)
            {
                ConnectCamera(true);
                //System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"Set Connect {value.ToString()} callback\n");
                LogMessage("Connected Set", "Connecting to port camera");
                // TODO connect to the device
            }
            else
            {
                ConnectCamera(false);
                //System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"Set Connect {value.ToString()} callback\n");
                LogMessage("Connected Set", "Disconnecting camera");
                // TODO disconnect from the device
            }
        }
    }

    private bool IsConnected
    {
        get
        {
            IsOnline = CheckConnection();
            connectedState = IsOnline;
            //System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"Check Connect {connectedState.ToString()} callback\n");
            // TODO check that the driver hardware connection exists and is connected to the hardware
            return connectedState;
        }
    }

    internal void ReadProfile()
    {
        using (Profile driverProfile = new Profile())
        {
            driverProfile.DeviceType = "Camera";
            tl.Enabled = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, string.Empty, traceStateDefault));
            comPort = driverProfile.GetValue(driverID, comPortProfileName, string.Empty, comPortDefault);
        }
    }

    internal void WriteProfile()
    {
        using (Profile driverProfile = new Profile())
        {
            //System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"Write Profile callback\n");
            driverProfile.DeviceType = "Camera";
            driverProfile.WriteValue(driverID, traceStateProfileName, tl.Enabled.ToString());
            driverProfile.WriteValue(driverID, comPortProfileName, comPort.ToString());
        }
    }

    public void SetupDialog()
    {
        //System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"Setup Dialog callback\n");
        if (IsConnected)
            System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

        using (SetupDialogForm F = new SetupDialogForm(tl))
        {
            DialogResult result = F.ShowDialog();
            if (result == DialogResult.OK)
            {
                WriteProfile();
            }
        }
    }
    internal void LogMessage(string identifier, string message, params object[] args)
    {
        var msg = string.Format(message, args);
        tl.LogMessage(identifier, msg);
    }


    public IStateValueCollection DeviceState => throw new System.NotImplementedException();
    #endregion


    //ENDOFINSERTEDFILE
}