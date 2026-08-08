using System.IO;
using System.Runtime.InteropServices;
using HighMetro.BaseModel;
using HighMetro.ClassLib;
using HighMetro.HikVision;

namespace HighMetro.Services;

public static class CamRemoteLinkImpl
{
    public static LoadCamResult Init()
    {
        var value = HikSdk.NET_DVR_Init();
        var loadCamResult = new LoadCamResult
        {
            Code = value? PublicConst.FlagYes : PublicConst.FlagNo
        };
        return loadCamResult;
    }
    public static LoadCamResult Login(HardInfo hardInfo)
    {
        //登录设备；
        var loginInfo = new CHCNetSDK.NET_DVR_USER_LOGIN_INFO();

        //设备IP地址或者域名
        var byIp = System.Text.Encoding.Default.GetBytes(hardInfo.Ip);
        loginInfo.sDeviceAddress = new byte[129];
        byIp.CopyTo(loginInfo.sDeviceAddress, 0);

        //设备用户名
        var byUserName = System.Text.Encoding.Default.GetBytes(hardInfo.UserName);
        loginInfo.sUserName = new byte[64];
        byUserName.CopyTo(loginInfo.sUserName, 0);

        //设备密码
        var byPassword = System.Text.Encoding.Default.GetBytes(hardInfo.PassWord);
        loginInfo.sPassword = new byte[64];
        byPassword.CopyTo(loginInfo.sPassword, 0);

        loginInfo.wPort = (ushort)hardInfo.Port;//设备服务端口号
        loginInfo.bUseAsynLogin = false; //是否异步登录：0- 否，1- 是 

        var deviceInfo = new CHCNetSDK.NET_DVR_DEVICEINFO_V40();

        //登录设备 Login the device
        var userId = HikSdk.NET_DVR_Login_V40(ref loginInfo, ref deviceInfo);
        if (userId >= 0)
        {
            var loadCamResult = new LoadCamResult
            {
                Code = PublicConst.FlagYes,
                Value = userId
            };
            return loadCamResult;
        }
        else
        {
            return GetLastError();
        }
    }
    public static LoadCamResult Logout(int iUserId)
    {
        var value = HikSdk.NET_DVR_Logout(iUserId);
        var loadCamResult = new LoadCamResult
        {
            Code = value!=0? PublicConst.FlagYes : PublicConst.FlagNo
        };
        return loadCamResult;
    }
    public static LoadCamResult CaptureJpegPicture(int iUserId,CameraBean cameraBean)
    {
        //图片保存路径和文件名 the path and file name to save
        var directory = SystemInfo.PhotoDir;
        directory = Path.Combine(directory,System.DateTime.Now.ToString("yyyy-MM-dd"));
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        directory = Path.Combine(directory,cameraBean.Id.ToString());
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        var now = System.DateTime.Now;
        // 提取时、分、秒
        var hour = now.Hour;       // 小时（0-23）
        var minute = now.Minute;   // 分钟（0-59）
        var second = now.Second;
        var timeStr = $"{hour:D2}-{minute:D2}-{second:D2}";
        var sJpegPicFileName = directory + cameraBean.Serial+"-"+timeStr + ".jpg";
        cameraBean.FilePath = sJpegPicFileName;
        var lChannel = 1; //通道号 Channel number
        var lpJpegPara = new CHCNetSDK.NET_DVR_JPEGPARA
        {
            wPicQuality = 0, 
            wPicSize = 0xff
        };
        var bufferPtr = Marshal.AllocHGlobal(PublicConst.MaxBufferSize);
        try
        {
            uint actualSize = 0;
            var nativeRet = HikSdk.NET_DVR_CaptureJPEGPicture_NEW(
                iUserId,
                lChannel,
                ref lpJpegPara,
                bufferPtr,
                PublicConst.MaxBufferSize,
                ref actualSize);
            // nativeRet !=0 代表SDK调用成功
            if (nativeRet == 0 || actualSize <= 0)
            {
                return GetLastError();
            }
            // 从非托管内存拷贝到托管字节数组
            byte[] jpegBytes = new byte[actualSize];
            Marshal.Copy(bufferPtr, jpegBytes, 0, (int)actualSize);
            // 写入磁盘
            File.WriteAllBytes(sJpegPicFileName, jpegBytes);
            return new LoadCamResult{ Code = PublicConst.FlagYes};
        }
        finally
        {
            Marshal.FreeHGlobal(bufferPtr);
        }
    }
    private static LoadCamResult GetLastError()
    {
        var iLastErr = HikSdk.NET_DVR_GetLastError();
        var loadCamResult = new LoadCamResult
        {
            Code = PublicConst.FlagNo,
            Message = "登录失败，错误代码：" + iLastErr
        };
        return loadCamResult;
    }
}