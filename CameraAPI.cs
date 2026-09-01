using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DVPCameraType;

namespace MyCameraAPI
{
    //线程安全的调试日志辅助类：加锁串行写入，日志失败（文件被占用等）绝不影响相机驱动运行
    public static class CameraLog
    {
        private static readonly object sync = new object();

        public static void Write(string msg)
        {
            try
            {
                lock (sync)
                {
                    System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", msg);
                }
            }
            catch (Exception)
            {
                // 调试日志失败不影响驱动
            }
        }
    }

    public class CameraConfig
    {
        private uint handle;
        private CameraCapture camera = new CameraCapture();
        public bool stopexposure = false;
        //private DVPCameraType.dvpDoubleDescr cameraDescr = new DVPCameraType.dvpDoubleDescr();
        public CameraConfig(uint cameraHandle)
        {
            handle = cameraHandle;
        }
        //开启视频流
        public bool StartExposure()
        {
            try
            {
                dvpStatus startStatus = DVPCamera.dvpStart(handle);
                CameraLog.Write($"{DateTime.Now}: dvpStart status={startStatus} callback\n");
                return true;
            }
            catch (Exception ex)
            {
                CameraLog.Write($"{DateTime.Now}: dvpStart EXCEPTION {ex.Message} callback\n");
                return false;
            }
        }
        //关闭视频流
        public bool CloseExposure()
        {
            try
            {
                DVPCamera.dvpStop(handle);
                return true;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"设置曝光时间异常: {ex.Message}");
                return false;
            }
        }

        //设置曝光时间（微秒）
        public bool SetExposureTime(double exposureUs)
        {
            try
            {
                DVPCamera.dvpSetExposure(handle, exposureUs);
                return true;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"设置曝光时间异常: {ex.Message}");
                return false;
            }
        }
        //获取曝光时间
        public double GetExposure()
        {
            double refExposure = 0;
            try
            {
                DVPCamera.dvpGetExposure(handle, ref refExposure);
                return refExposure;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"获取曝光时间异常: {ex.Message}");
                return 0.0;
            }
        }
        //获取曝光参数
        public dvpDoubleDescr GetExposureDescr()
        {
            DVPCameraType.dvpDoubleDescr GetExposureDescr = new DVPCameraType.dvpDoubleDescr();
            try
            {
                DVPCamera.dvpGetExposureDescr(handle, ref GetExposureDescr);
                return GetExposureDescr;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"获取曝光参数异常: {ex.Message}");
                return GetExposureDescr;
            }
        }

        //设置增益值
        public bool SetGain(float gain)
        {
            try
            {
                DVPCamera.dvpSetAnalogGain(handle, gain);
                return false;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"设置增益异常: {ex.Message}");
                return false;
            }
        }
        //获取模拟增益
        public float GetGain()
        {
            float refAnalogGain = 0;
            try
            {
                DVPCamera.dvpGetAnalogGain(handle, ref refAnalogGain);
                return refAnalogGain;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"获取增益异常: {ex.Message}");
                return 0F;
            }
        }
        //获取模拟增益描述信息
        public dvpFloatDescr GetGainDescr()
        {
            dvpFloatDescr refAnalogGainDescr = new dvpFloatDescr();
            try
            {
                DVPCamera.dvpGetAnalogGainDescr(handle, ref refAnalogGainDescr);
                return refAnalogGainDescr;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"获取增益异常: {ex.Message}");
                return refAnalogGainDescr;
            }
        }

        //关闭触发模式控制
        public bool CloseTriggerMode()
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetTriggerState(handle, false);

                if (status == dvpStatus.DVP_STATUS_OK)
                {
                    //Console.WriteLine($"触发模式设置成功: 关闭");
                    return true;
                }
                else
                {
//Console.WriteLine($"触发模式设置失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
              //Console.WriteLine($"设置触发模式异常: {ex.Message}");
                return false;
            }
        }
        //关闭伽马模式

        public bool CloseGammaMode()
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetGammaState(handle, false);

                if (status == dvpStatus.DVP_STATUS_OK)
                {
                    //Console.WriteLine($"伽马模式设置成功: 关闭");
                    return true;
                }
                else
                {
                    //Console.WriteLine($"伽马模式设置失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"伽马模式异常: {ex.Message}");
                return false;
            }
        }
        //设置ROI使能
        public bool SetROIStatus(bool enable)
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetRoiState(handle, enable);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                    //Console.WriteLine($"ROI打开成功: 关闭");
                    return true;
                }
                else
                {
                    //Console.WriteLine($"ROI打开失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"ROI打开异常: {ex.Message}");
                return false;
            }
        }
        //设置ROI
        public bool SetROI(dvpRegion Roi)
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetRoi(handle, Roi);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                    //Console.WriteLine($"ROI设置成功");
                    return true;
                }
                else
                {
                    //Console.WriteLine($"ROI设置失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"ROI设置异常: {ex.Message}");
                return false;
            }
        }
        //锐化失能
        public bool CloseSharp()
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetSharpnessState(handle, false);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                    //Console.WriteLine($"锐化失能");
                    return true;
                }
                else
                {
                    //Console.WriteLine($"锐化失能失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"锐化失能异常: {ex.Message}");
                return false;
            }
        }
        //饱和度失能
        public bool CloseSaturation()
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetSaturationState(handle, false);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                    //Console.WriteLine($"饱和度失能");
                    return true;
                }
                else
                {
                    //Console.WriteLine($"饱和度失能失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"饱和度失能异常: {ex.Message}");
                return false;
            }
        }
        //2D降噪失能
        public bool CloseNoiseReduct2d()
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetNoiseReduct2dState(handle, false);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                    //Console.WriteLine($"2D降噪失能");
                    return true;
                }
                else
                {
                    //Console.WriteLine($"2D降噪失能失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"2D降噪失能异常: {ex.Message}");
                return false;
            }
        }
        //3D降噪失能
        public bool CloseNoiseReduct3d()
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetNoiseReduct3dState(handle, false);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                    //Console.WriteLine($"3D降噪失能");
                    return true;
                }
                else
                {
                    //Console.WriteLine($"3D降噪失能失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"3D降噪失能异常: {ex.Message}");
                return false;
            }
        }
        //黑电平失能
        public bool CloseBlackLevel()
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetBlackLevelState(handle, false);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                    //Console.WriteLine($"黑电平失能");
                    return true;
                }
                else
                {
                    //Console.WriteLine($"黑电平失能失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"黑电平失能异常: {ex.Message}");
                return false;
            }
        }

        //获取视频流状态
        public dvpStreamState GetStreamState()
        {
            DVPCameraType.dvpStreamState refStreamState = new DVPCameraType.dvpStreamState();
            try
            {
                DVPCamera.dvpGetStreamState(handle, ref refStreamState);
                return refStreamState;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"获取视频流异常: {ex.Message}");
                return refStreamState;
            }
        }
        //mono模式失能
        public bool CloseMono()
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetMonoState(handle, false);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                    //Console.WriteLine($"mono失能");
                    return true;
                }
                else
                {
                    //Console.WriteLine($"mono失能失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"mono失能异常: {ex.Message}");
                return false;
            }
        }
        //平场模式失能
        public bool CloseFlat()
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetFlatFieldState(handle, false);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                    //Console.WriteLine($"平场失能");
                    return true;
                }
                else
                {
                    //Console.WriteLine($"平场失能失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"平场失能异常: {ex.Message}");
                return false;
            }
        }
        //降彩噪失能
        public bool CloseFix()
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetDefectFixState(handle, false);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                    //Console.WriteLine($"降彩噪失能");
                    return true;
                }
                else
                {
                    //Console.WriteLine($"降彩噪失能失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"降彩噪失能异常: {ex.Message}");
                return false;
            }
        }
        //自动降彩噪失能
        public bool CloseAutoFix()
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetAutoDefectFixState(handle, false);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                    //Console.WriteLine($"自动降彩噪失能");
                    return true;
                }
                else
                {
                    //Console.WriteLine($"自动降彩噪失能失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"自动降彩噪失能异常: {ex.Message}");
                return false;
            }
        }
        //旋转失能
        public bool CloseRotate()
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetRotateState(handle, false);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                   // Console.WriteLine($"旋转失能");
                    return true;
                }
                else
                {
                   // Console.WriteLine($"旋转失能失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
              //  Console.WriteLine($"旋转失能异常: {ex.Message}");
                return false;
            }
        }
        //自动曝光失能
        public bool CloseAe()
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetAeOperation(handle, dvpAeOperation.AE_OP_OFF);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                  //  Console.WriteLine($"自动曝光失能");
                    return true;
                }
                else
                {
                   // Console.WriteLine($"自动曝光失能失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"自动曝光失能异常: {ex.Message}");
                return false;
            }
        }


        //获取原始图像格式选项的索引
        public uint GetSourceFormatSel()
        {
            try
            {
                uint refSourceFormatSel = 0;
                dvpStatus status = DVPCamera.dvpGetSourceFormatSel(handle, ref refSourceFormatSel);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                    //Console.WriteLine($"获取原始图像格式选项的索引");
                    return refSourceFormatSel;
                }
                else
                {
                  //  Console.WriteLine($"原始图像格式选项的索引获取失败: {status}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
               // Console.WriteLine($"原始图像格式选项的索引获取异常: {ex.Message}");
                return 0;
            }
        }
        //设置原始图像格式选项的索引:0-->raw8,1-->raw12
        public bool SetSourceFormatSel(uint SourceFormatSel)
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetSourceFormatSel(handle, SourceFormatSel);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                 //  Console.WriteLine($"设置原始图像格式选项的索引");
                    return true;
                }
                else
                {
                  //  Console.WriteLine($"设置原始图像格式选项的索引失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
               // Console.WriteLine($"设置原始图像格式选项的索引异常: {ex.Message}");
                return false;
            }
        }
        //获取原始图像格式选项索引的描述信息
        public dvpSelectionDescr GetSourceFormatSelDescr()
        {
            dvpSelectionDescr refSourceFormatSelDescr = new dvpSelectionDescr();
            try
            {
                dvpStatus status = DVPCamera.dvpGetSourceFormatSelDescr(handle, ref refSourceFormatSelDescr);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                  //  Console.WriteLine($"获取原始图像格式选项索引的描述信息");
                    return refSourceFormatSelDescr;
                }
                else
                {
                   // Console.WriteLine($"获原始图像格式选项索引的描述信息获取失败: {status}");
                    return refSourceFormatSelDescr;
                }
            }
            catch (Exception ex)
            {
              //  Console.WriteLine($"获原始图像格式选项索引的描述信息获取异常: {ex.Message}");
                return refSourceFormatSelDescr;
            }
        }
        //获取原始图像格式选项索引的具体信息
        public dvpFormatSelection GetSourceFormatSelDetail()
        {
            uint SourceFormatSel = GetSourceFormatSel();

            dvpFormatSelection refSourceFormatSelDetail = new dvpFormatSelection();
            try
            {
                dvpStatus status = DVPCamera.dvpGetSourceFormatSelDetail(handle, SourceFormatSel, ref refSourceFormatSelDetail);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                 //   Console.WriteLine($"获取原始图像格式选项索引的描述信息");
                    return refSourceFormatSelDetail;
                }
                else
                {
                  //  Console.WriteLine($"获原始图像格式选项索引的描述信息获取失败: {status}");
                    return refSourceFormatSelDetail;
                }
            }
            catch (Exception ex)
            {
             //   Console.WriteLine($"获原始图像格式选项索引的描述信息获取异常: {ex.Message}");
                return refSourceFormatSelDetail;
            }
        }

        //获取输出图像格式选项的索引
        public uint GetTargetFormatSel()
        {
            try
            {
                uint refTargetFormatSel = 0;
                dvpStatus status = DVPCamera.dvpGetTargetFormatSel(handle, ref refTargetFormatSel);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                  //  Console.WriteLine($"获取输出图像格式选项的索引");
                    return refTargetFormatSel;
                }
                else
                {
                  //  Console.WriteLine($"输出图像格式选项的索引获取失败: {status}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
              //  Console.WriteLine($"输出图像格式选项的索引获取异常: {ex.Message}");
                return 0;
            }
        }
        //设置原始图像格式选项的索引:0-->bgr24,1-->raw8,2-->raw16
        public bool SetTargetFormatSel(uint TargetFormatSel)
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetTargetFormatSel(handle, TargetFormatSel);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                  //  Console.WriteLine($"设置输出图像格式选项的索引");
                    return true;
                }
                else
                {
                  //  Console.WriteLine($"设置输出图像格式选项的索引失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
             //   Console.WriteLine($"设置输出图像格式选项的索引异常: {ex.Message}");
                return false;
            }
        }
        //获取输出图像格式选项索引的描述信息
        public dvpSelectionDescr GetTargetFormatSelDescr()
        {
            dvpSelectionDescr refTargetFormatSelDescr = new dvpSelectionDescr();
            try
            {
                dvpStatus status = DVPCamera.dvpGetTargetFormatSelDescr(handle, ref refTargetFormatSelDescr);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                  //  Console.WriteLine($"获取输出图像格式选项索引的描述信息");
                    return refTargetFormatSelDescr;
                }
                else
                {
                   // Console.WriteLine($"获取输出图像格式选项索引的描述信息获取失败: {status}");
                    return refTargetFormatSelDescr;
                }
            }
            catch (Exception ex)
            {
              //  Console.WriteLine($"获取输出图像格式选项索引的描述信息获取异常: {ex.Message}");
                return refTargetFormatSelDescr;
            }
        }
        //获取输出图像格式选项索引的具体信息
        public dvpFormatSelection GetTargetFormatSelDetail()
        {
            uint TargetFormatSel = GetTargetFormatSel();

            dvpFormatSelection refTargetFormatSelDetail = new dvpFormatSelection();
            try
            {
                dvpStatus status = DVPCamera.dvpGetTargetFormatSelDetail(handle, TargetFormatSel, ref refTargetFormatSelDetail);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                   // Console.WriteLine($"获取输出图像格式选项索引的描述信息");
                    return refTargetFormatSelDetail;
                }
                else
                {
                  //  Console.WriteLine($"获取输出图像格式选项索引的描述信息获取失败: {status}");
                    return refTargetFormatSelDetail;
                }
            }
            catch (Exception ex)
            {
               // Console.WriteLine($"获原始图像格式选项索引的描述信息获取异常: {ex.Message}");
                return refTargetFormatSelDetail;
            }
        }

        //获取分辨率模式选项的索引
        public uint GetResolutionModeSel()
        {
            try
            {
                uint refResolutionModeSel = 0;
                dvpStatus status = DVPCamera.dvpGetResolutionModeSel(handle, ref refResolutionModeSel);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                  //  Console.WriteLine($"获取分辨率模式选项的索引");
                    return refResolutionModeSel;
                }
                else
                {
//Console.WriteLine($"分辨率模式选项的索引获取失败: {status}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
              //  Console.WriteLine($"分辨率模式选项的索引获取异常: {ex.Message}");
                return 0;
            }
        }
        //设置当前分辨率模式选项的索引
        //这里为0-6范围，对应normal bin2 sum2 bin3 sum3 bin4 sum4
        public bool SetResolutionModeSel(uint ResolutionModeSel)
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetResolutionModeSel(handle, ResolutionModeSel);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                  //  Console.WriteLine($"设置当前分辨率模式选项的索引");
                    return true;
                }
                else
                {
                  //  Console.WriteLine($"设置当前分辨率模式选项的索引失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
              //  Console.WriteLine($"设置当前分辨率模式选项的索引异常: {ex.Message}");
                return false;
            }
        }
        //获取分辨率模式选项索引的描述信息
        public dvpSelectionDescr GetResolutionModeSelDescr()
        {
            dvpSelectionDescr refResolutionModeSelDescr = new dvpSelectionDescr();
            try
            {
                dvpStatus status = DVPCamera.dvpGetResolutionModeSelDescr(handle, ref refResolutionModeSelDescr);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                  //  Console.WriteLine($"获取输出图像格式选项索引的描述信息");
                    return refResolutionModeSelDescr;
                }
                else
                {
                   // Console.WriteLine($"获取输出图像格式选项索引的描述信息获取失败: {status}");
                    return refResolutionModeSelDescr;
                }
            }
            catch (Exception ex)
            {
             //   Console.WriteLine($"获取输出图像格式选项索引的描述信息获取异常: {ex.Message}");
                return refResolutionModeSelDescr;
            }
        }
        //获取当前分辨率模式选项索引对应的具体信息
        public dvpResolutionMode GetResolutionModeSelDetail()
        {
            uint ResolutionModeSel = GetResolutionModeSel();

            dvpResolutionMode refTargetFormatSelDetail = new dvpResolutionMode();
            try
            {
                dvpStatus status = DVPCamera.dvpGetResolutionModeSelDetail(handle, ResolutionModeSel, ref refTargetFormatSelDetail);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
              //      Console.WriteLine($"获取输出图像格式选项索引的描述信息");
                    return refTargetFormatSelDetail;
                }
                else
                {
                 //   Console.WriteLine($"获取输出图像格式选项索引的描述信息获取失败: {status}");
                    return refTargetFormatSelDetail;
                }
            }
            catch (Exception ex)
            {
             //   Console.WriteLine($"获原始图像格式选项索引的描述信息获取异常: {ex.Message}");
                return refTargetFormatSelDetail;
            }
        }


        //红绿蓝数字增益失能
        public bool CloseRgbGain()
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetRgbGainState(handle, false);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                 //   Console.WriteLine($"rgb数字增益失能");
                    return true;
                }
                else
                {
                  // Console.WriteLine($"rgb数字增益失能失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
              //  Console.WriteLine($"rgb数字增益失能异常: {ex.Message}");
                return false;
            }
        }
        //关闭消除频闪
        public bool CloseAntiFlick()
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetAntiFlick(handle, dvpAntiFlick.ANTIFLICK_DISABLE);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                   // Console.WriteLine($"消除频闪失能");
                    return true;
                }
                else
                {
                 //   Console.WriteLine($"消除频闪失能失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
             //   Console.WriteLine($"消除频闪失能异常: {ex.Message}");
                return false;
            }
        }
        //关闭色温
        public bool CloseColorTemperature()
        {
            try
            {
                dvpStatus status = DVPCamera.dvpSetColorTemperatureState(handle, false);
                if (status == dvpStatus.DVP_STATUS_OK)
                {
                  //  Console.WriteLine($"色温失能");
                    return true;
                }
                else
                {
                 //   Console.WriteLine($"色温失能失败: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
             //   Console.WriteLine($"色温失能异常: {ex.Message}");
                return false;
            }
        }

        //默认配置参数，也可用于自检
        public bool DefaultConfig()
        {
            try
            {
                CloseTriggerMode();
                CloseGammaMode();
                CloseSharp();
                CloseSaturation();
                CloseNoiseReduct2d();
                CloseNoiseReduct3d();
                CloseBlackLevel();
                CloseMono();
                CloseFlat();
                CloseFix();
                CloseAutoFix();
                CloseRotate();
                CloseAe();
                CloseAntiFlick();
                CloseColorTemperature();
                SetROIStatus(false);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //单次曝光（小于10秒，异步）：在后台线程采集一帧并返回
        public Task<ImageMatrix> StartSingleExposureAsync(double exposureTime)
        {
            return Task.Run(() =>
            {
                SetExposureTime(exposureTime * 1000000);

                // 循环等待并读取图像
                while (true)
                {
                    ImageMatrix currentFrame = camera.CaptureSingleImage(handle);

                    // 检查图像是否有效
                    if (currentFrame != null && currentFrame.Data2D != null)
                    {
                        return currentFrame; // 成功读取到有效图像
                    }

                    // 等待一段时间后重试，避免忙等占用CPU
                    System.Threading.Thread.Sleep(50);
                }
            });
        }

        //多重曝光（大于等于10秒，异步）：拆分为多个10s帧累加后返回
        public Task<ImageMatrix> StartMultiExposureAsync(double exposureTime)
        {
            return Task.Run(() =>
            {
                uint exposureCount = (uint)(exposureTime / 10);

                // 累积和矩阵：累加时逐像素饱和限制在 0~65535，用 int 即可（比 long 省一半内存）
                int[,] sumMatrix = null;
                uint validFrameCount = 0;
                const int MAX_USHORT = 65535;

                for (int i = 0; i < exposureCount; i++)
                {
                    SetExposureTime(10000000); // 设置10s曝光
                    // 循环等待并读取图像
                    ImageMatrix currentFrame = null;
                    while (true)
                    {
                        currentFrame = camera.CaptureSingleImage(handle);

                        // 检查图像是否有效
                        if (currentFrame != null && currentFrame.Data2D != null)
                        {
                            break; // 成功读取到有效图像，跳出等待循环
                        }

                        // 等待一段时间后重试
                        System.Threading.Thread.Sleep(50);
                    }

                    // 初始化或更新累加矩阵
                    if (sumMatrix == null)
                    {
                        // 第一次成功采集，初始化累加矩阵
                        sumMatrix = new int[currentFrame.Width, currentFrame.Height];
                        validFrameCount = 1;

                        // 直接复制当前帧到累加矩阵
                        for (int x = 0; x < currentFrame.Width; x++)
                        {
                            for (int y = 0; y < currentFrame.Height; y++)
                            {
                                sumMatrix[x, y] = currentFrame.Data2D[x, y];
                            }
                        }
                    }
                    else
                    {
                        // 累加模式：临时 int 相加（两帧各≤65535，和≤131070不溢出）后立即饱和
                        validFrameCount++;
                        for (int x = 0; x < currentFrame.Width; x++)
                        {
                            for (int y = 0; y < currentFrame.Height; y++)
                            {
                                int temp = sumMatrix[x, y] + currentFrame.Data2D[x, y];
                                sumMatrix[x, y] = Math.Min(temp, MAX_USHORT);
                            }
                        }
                    }
                }

                // 将累加矩阵转换为 ImageMatrix 返回（累加时已逐像素饱和，直接拷贝）
                if (sumMatrix != null && validFrameCount > 0)
                {
                    ImageMatrix resultImage = new ImageMatrix(sumMatrix.GetLength(0), sumMatrix.GetLength(1), 16, 1);
                    for (int x = 0; x < resultImage.Width; x++)
                    {
                        for (int y = 0; y < resultImage.Height; y++)
                        {
                            resultImage.Data2D[x, y] = sumMatrix[x, y];
                        }
                    }
                    return resultImage;
                }
                return null;
            });
        }

        //同步入口：按曝光时长分派到单次或多次曝光
        public ImageMatrix StartMultiExposure(double exposureTime)
        {
            if (exposureTime >= 10)
            {
                return StartMultiExposureAsync(exposureTime).GetAwaiter().GetResult();
            }
            else
            {
                return StartSingleExposureAsync(exposureTime).GetAwaiter().GetResult();
            }
        }


    }
    //图像采集类
    public class CameraCapture
    {
        private byte[] frameBuffer = null;//复用的帧缓冲，避免每帧 new 大数组导致 LOH 碎片化（bin 变化时按需扩容）

        private int GetBitsPerPixel(DVPCameraType.dvpBits bits)
        {
            switch (bits)
            {
                case DVPCameraType.dvpBits.BITS_8: return 8;
                case DVPCameraType.dvpBits.BITS_10: return 10;
                case DVPCameraType.dvpBits.BITS_12: return 12;
                case DVPCameraType.dvpBits.BITS_14: return 14;
                case DVPCameraType.dvpBits.BITS_16: return 16;
                default: return 8;
            }
        }

        public ImageMatrix CaptureSingleImage(uint handle, uint timeout = 100)
        {
            try
            {
                
                // 获取图像缓冲区
                DVPCameraType.dvpFrameBuffer rawBuffer = new DVPCameraType.dvpFrameBuffer();
                DVPCameraType.dvpFrameBuffer outBuffer = new DVPCameraType.dvpFrameBuffer();
                dvpStatus status = DVPCamera.dvpGetFrameBuffer(handle, ref rawBuffer, ref outBuffer, timeout);

                if (status != dvpStatus.DVP_STATUS_OK)
                {
                  //  Console.WriteLine($"获取图像失败: {status}");
                    return null;
                }

                // 获取图像信息
                uint bufferLength = outBuffer.frame.uBytes;
                int width = outBuffer.frame.iWidth;
                int height = outBuffer.frame.iHeight;
                int bitsPerPixel = GetBitsPerPixel(outBuffer.frame.bits);
                IntPtr bufferStart = outBuffer.pData;

                // 计算通道数
                int channels = (int)bufferLength / (width * height);

                // 判断图像格式
                bool isBGR = (channels == 3);
                if (!isBGR)
                {
                    channels = 1;
                }
                // 创建图像矩阵
                ImageMatrix imageMatrix = new ImageMatrix(width, height, bitsPerPixel, channels);

                // 复用帧缓冲，避免每帧 new 大数组导致 LOH 碎片化（bin 变化时按需扩容）
                if (frameBuffer == null || frameBuffer.Length < bufferLength)
                {
                    frameBuffer = new byte[bufferLength];
                }
                Marshal.Copy(bufferStart, frameBuffer, 0, (int)bufferLength);

                if (isBGR)
                {
                    // BGR24格式：三通道，直接填充到 ushort 矩阵（较少用）
                    int bytesPerPixel = bitsPerPixel / 8;
                    int stride = width * channels * bytesPerPixel;

                    for (int h = 0; h < height; h++)
                    {
                        int rowOffset = h * stride;
                        for (int w = 0; w < width; w++)
                        {
                            int pixelOffset = rowOffset + w * channels * bytesPerPixel;

                            // BGR 三个通道
                            for (int c = 0; c < channels; c++)
                            {
                                // 根据位深读取值
                                if (bytesPerPixel == 1)
                                {
                                    // 8位数据
                                    imageMatrix.Data3D[h, w, c] = frameBuffer[pixelOffset + c];
                                }
                                else if (bytesPerPixel == 2)
                                {
                                    // 16位数据（小端序）
                                    int byteOffset = pixelOffset + c * 2;
                                    ushort value = (ushort)(frameBuffer[byteOffset] | (frameBuffer[byteOffset + 1] << 8));
                                    imageMatrix.Data3D[h, w, c] = value;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // 单通道格式（包括 RAW8、RAW10、RAW12、RAW16 等）— 正常整帧读取，复用 frameBuffer
                    int bytesPerPixel = (int)bufferLength / (width * height);

                    for (int h = 0; h < height; h++)
                    {
                        for (int w = 0; w < width; w++)
                        {
                            int pixelOffset = (h * width + w) * bytesPerPixel;
                            ushort value = 0;

                            if (bytesPerPixel == 1)
                            {
                                // 8位数据：直接赋值到低位
                                value = frameBuffer[pixelOffset];
                            }
                            else if (bytesPerPixel == 2)
                            {
                                // 16位数据：合并高低位（小端序）
                                value = (ushort)(frameBuffer[pixelOffset] | (frameBuffer[pixelOffset + 1] << 8));
                            }
                            // 存储到单通道矩阵
                            imageMatrix.Data2D[w, h] = value;
                        }
                    }
                }
                return imageMatrix;
            }
            catch (Exception ex)
            {
                CameraLog.Write($"{DateTime.Now}: CaptureSingleImage ERROR {ex} callback\n");
                return null;
            }
        }
    }

    // 图像矩阵类（使用 ushort 存储）
    public class ImageMatrix
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Channels { get; private set; }
        public int BitsPerPixel { get; private set; }
        public bool IsBGR { get; private set; }

        public int BytesPerPixel => BitsPerPixel / 8;

        // 统一使用 ushort 类型的三维矩阵
        public ushort[,,] Data3D { get; private set; }  // [height, width, channel]rgb
        public int[,] Data2D { get; private set; }  // [height, width]raw


        public ImageMatrix(int width, int height, int bitsPerPixel, int channels)
        {
            Width = width;
            Height = height;
            BitsPerPixel = bitsPerPixel;
            Channels = channels;

            // 驱动是 debayer 前的原始数据，只用到 Data2D；Data3D 仅 BGR 格式才需要（省内存，避免32位进程OOM）
            if (channels > 1)
            {
                Data3D = new ushort[height, width, channels];
            }
            Data2D = new int[width, height];//int ushort都可以
        }
    }
}