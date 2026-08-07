// Implements:	ASCOM Camera interface version: 1.0.0
// Author:		yinqizhu <18757389069@163.com>
//
// Edit Log:
//
// Date		Who	     Vers	Description
// --------	-------- -----	------------------------------------------------
// 2026.4.2 yinqizhu 1.0.0	Initial edit, created from ASCOM driver template
// -------------------------------------------------------------------------
//


// This is used to define code in the template that is specific to one class implementation
// unused code can be deleted and this definition removed.
#define Cameraclass

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;


namespace ASCOM.MyCamera
{
    //
    // Your driver's DeviceID is ASCOM.TEMPLATEDEVICENAME.CAMERACLASS
    //
    // The Guid attribute sets the CLSID for ASCOM.TEMPLATEDEVICENAME.CAMERACLASS
    // The ClassInterface/None attribute prevents an empty interface called
    // _TEMPLATEDEVICENAME from being created and used as the [default] interface
    //
    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM CAMERACLASS Driver for TEMPLATEDEVICENAME.
    /// </summary>
    [Guid("5C34EAB1-D646-EC51-FBD0-188D211B9FDA")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    [ProgId("ASCOM.DVPCamera.Camera")]
    public class Driver: Cameraclass
    {
        /// <summary>
        /// 注册驱动到 ASCOM 平台
        /// </summary>
        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            using (var profile = new ASCOM.Utilities.Profile())
            {
                profile.DeviceType = "Camera";
                profile.Register("ASCOM.DVPCamera.Camera", "DVPCamrea");
            }
        }

        /// <summary>
        /// 从 ASCOM 平台卸载驱动
        /// </summary>
        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            using (var profile = new ASCOM.Utilities.Profile())
            {
                profile.DeviceType = "Camera";
                profile.Unregister("ASCOM.DVPCamera.Camera");
            }
        }

        /// <summary>
        /// 工厂方法：创建相机实例
        /// </summary>
        //public DeviceCamera Create()
        //{
            //System.IO.File.AppendAllText(@"C:\temp\camera_debug.txt", $"{DateTime.Now}: Create DeviceCamera callback\n");
            //return new DeviceCamera();
        //}
    }
    public class Cameraclass : DeviceCamera
    {
        public Cameraclass() : base()
        {
        }
    }
}
