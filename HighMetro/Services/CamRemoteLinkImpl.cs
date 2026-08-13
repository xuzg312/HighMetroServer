using System;
using System.IO;
using System.Runtime.InteropServices;
using HighMetro.BaseModel;
using HighMetro.ClassLib;
using HighMetro.HikVision;

namespace HighMetro.Services;

public class CamRemoteLinkImpl
{
    private readonly object _dirLockObj = new();
    public LoadCamResult Init()
    {
        var value = HikSdk.NET_DVR_Init();
        var loadCamResult = new LoadCamResult
        {
            Code = value!=0? PublicConst.FlagYes : PublicConst.FlagNo
        };
        return loadCamResult;
    }
    public LoadCamResult Login(HardInfo hardInfo)
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
        return GetLastError();
    }
    public LoadCamResult Logout(int iUserId)
    {
        var value = HikSdk.NET_DVR_Logout(iUserId);
        var loadCamResult = new LoadCamResult
        {
            Code = value!=0? PublicConst.FlagYes : PublicConst.FlagNo
        };
        return loadCamResult;
    }
    public LoadCamResult CaptureJpegPicture(int iUserId,CameraBean cameraBean)
    {
        if (iUserId < 0)
        {
            return new LoadCamResult()
            {
                Code = PublicConst.FlagNo,
                Message = "UserId无效！"
            };
        }
        var bufferPtr = IntPtr.Zero;
        var fullSavePath = string.Empty;
        // 静态锁防止多线程并发创建目录冲突
        var dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        try
        {
            var baseDir = SystemInfo.PhotoDir;
            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var camIdFolder = cameraBean.Id.ToString();
            var dayDir = Path.Combine(baseDir, dateFolder);
            lock (_dirLockObj)
            {
                if (!Directory.Exists(dayDir))
                    Directory.CreateDirectory(dayDir);

                var targetDir = Path.Combine(dayDir, camIdFolder);
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);
            }
            #region 3. 生成带毫秒唯一文件名，避免同秒覆盖
            var now = DateTime.Now;
            var timeStr = $"{now.Hour:D2}-{now.Minute:D2}-{now.Second:D2}-{now.Millisecond:D3}";
            var fileName = $"{cameraBean.Serial}-{timeStr}.jpg";
            fullSavePath = Path.Combine(dayDir, camIdFolder, fileName);
            cameraBean.FilePath = fullSavePath;
            #endregion

            #region 4. SDK抓拍逻辑
            var lChannel = 1;
            var lpJpegPara = new CHCNetSDK.NET_DVR_JPEGPARA
            {
                wPicQuality = 0,
                wPicSize = 0xff
            };
            bufferPtr = Marshal.AllocHGlobal(PublicConst.MaxBufferSize);
            uint actualSize = 0;
            var nativeRet = HikSdk.NET_DVR_CaptureJPEGPicture_NEW(
                iUserId,
                lChannel,
                ref lpJpegPara,
                bufferPtr,
                PublicConst.MaxBufferSize,
                ref actualSize);

            // SDK调用失败校验
            if (nativeRet == 0 || actualSize <= 0)
            {
                return GetLastError();
            }
            // 拷贝非托管内存
            var jpegBytes = new byte[actualSize];
            Marshal.Copy(bufferPtr, jpegBytes, 0, (int)actualSize);

            // 写入文件，单独捕获IO异常
            SafeWriteFile(fullSavePath, jpegBytes);
            
            return new LoadCamResult
            {
                Code = PublicConst.FlagYes,
            };
            #endregion
        }
        catch (IOException ioEx)
        {
            return new LoadCamResult
            {
                Code = PublicConst.FlagNo,
                Message = $"文件IO异常：{ioEx.Message}，路径：{fullSavePath}【{dateTime}】"
            };
        }
        catch (UnauthorizedAccessException authEx)
        {
            return new LoadCamResult
            {
                Code = PublicConst.FlagNo,
                Message = $"目录无读写权限：{authEx.Message}，路径：{fullSavePath}【{dateTime}】"
            };
        }
        catch (OutOfMemoryException memEx)
        {
            return new LoadCamResult
            {
                Code = PublicConst.FlagNo,
                Message = $"内存不足无法抓拍：{memEx.Message}【{dateTime}】"
            };
        }
        catch (ArgumentException argEx)
        {
            return new LoadCamResult
            {
                Code = PublicConst.FlagNo,
                Message = $"路径参数非法：{argEx.Message}【{dateTime}】"
            };
        }
        catch (Exception ex)
        {
            // 兜底所有未知异常
            return new LoadCamResult
            {
                Code = PublicConst.FlagNo,
                Message = $"抓拍未知异常：{ex.Message}【{dateTime}】"
            };
        }
        finally
        {
            // 强制释放非托管堆内存，防止内存泄漏
            if (bufferPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }
    }
    private void SafeWriteFile(string filePath, byte[] data)
    {
        // FileOptions.None 常规写入；可加 FileOptions.WriteThrough 强制落盘
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        fs.Write(data, 0, data.Length);
        fs.Flush(true);
    }
    public LoadCamResult Clear()
    {
        var value = HikSdk.NET_DVR_Cleanup();
        var loadCamResult = new LoadCamResult
        {
            Code = value!=0? PublicConst.FlagYes : PublicConst.FlagNo
        };
        return loadCamResult;
    }
    public bool CheckOnLine(int iUserId)
    {
        if (iUserId >= 0)
        {
            // 检测在线
            var value= HikSdk.NET_DVR_RemoteControl(iUserId,20005,IntPtr.Zero,0);
            return value != 0;
        }
        return false;
    }
    private LoadCamResult GetLastError()
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