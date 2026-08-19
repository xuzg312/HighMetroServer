using System;
using System.IO;
using System.Runtime.InteropServices;
using HighMetroServer.ClassLib;
using HighMetroServer.BaseModel;
using HighMetroServer.HikVision;

namespace HighMetroServer.Services;

public class CamRemoteLinkImpl
{
    private readonly object _dirLockObj = new();
    public LoadCamResult Init()
    {
        var value = HikSdk.NET_DVR_Init();
        var loadCamResult = new LoadCamResult
        {
            Code = value>=0? PublicConst.FlagYes : PublicConst.FlagNo
        };
        return loadCamResult;
    }
    public LoadCamResult Login(HardInfo hardInfo)
    {
        //登录设备；
        var loginInfo = new ChcNetSdk.NetDvrUserLoginInfo();

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

        var deviceInfo = new ChcNetSdk.NetDvrDeviceinfoV40();

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
            Code = value>=0? PublicConst.FlagYes : PublicConst.FlagNo
        };
        return loadCamResult;
    }
    public LoadCamResult CaptureJpegPicture(int iUserId,CameraBean cameraBean,string baseDirectory)
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
            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var camIdFolder = cameraBean.Id.ToString();
            var dayDir = Path.Combine(baseDirectory, dateFolder);
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
            var lpJpegPara = new ChcNetSdk.NetDvrJpegpara
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
            if (nativeRet < 0 || actualSize <= 0)
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
    public LoadCamResult DebugCaptureJpegPicture(int iUserId)
    {
        try
        {
            var lChannel = 1;
            var lpJpegPara = new ChcNetSdk.NetDvrJpegpara
            {
                wPicQuality = 0,
                wPicSize = 0xff
            };
            var bufferPtr = Marshal.AllocHGlobal(PublicConst.MaxBufferSize);
            uint actualSize = 0;
            var nativeRet = HikSdk.NET_DVR_CaptureJPEGPicture_NEW(
                iUserId,
                lChannel,
                ref lpJpegPara,
                bufferPtr,
                PublicConst.MaxBufferSize,
                ref actualSize);

            // SDK调用失败校验
            if (nativeRet < 0 || actualSize <= 0)
            {
                return GetLastError();
            }

            // 拷贝非托管内存
            var jpegBytes = new byte[actualSize];
            Marshal.Copy(bufferPtr, jpegBytes, 0, (int)actualSize);
            return new LoadCamResult
            {
                Code = PublicConst.FlagYes,
                ImageData = jpegBytes,
            };
        }
        catch (Exception ex)
        {
            return new LoadCamResult
            {
                Code = PublicConst.FlagNo,
                Message = $"抓拍未知异常：{ex.Message}"
            };
        }
    }
    public LoadCamResult StartPreview(
        int iUserId,RealDataCallBack realDataCallBack,PlayCtrl.DeccbFun decodeCallback)
    {
        var playPort=0;
        //获取播放句柄 Get the port to play
        if (PlayCtrl.PlayM4_GetPort(ref playPort)<=0)
            return GetLastError();
        //设置流播放模式 Set the stream mode: real-time stream mode
        if (PlayCtrl.PlayM4_SetStreamOpenMode(playPort, 0)<=0)
            return GetLastError();
        //打开码流，送入头数据 Open stream
        if (PlayCtrl.PlayM4_OpenStream(playPort, IntPtr.Zero, 0, CamConst.BufPoolSize)<=0)
            return GetLastError();
        //设置显示缓冲区个数 Set the display buffer number
        if (PlayCtrl.PlayM4_SetDisplayBuf(playPort,CamConst.DisplayBufNumber)<=0)
            return GetLastError(); 
        //设置显示模式 Set the display mode
        if (PlayCtrl.PlayM4_SetOverlayMode(playPort,0,0)<=0)
            return GetLastError(); 
        //设置解码回调函数，获取解码后音视频原始数据 Set callback function of decoded data
        var value = PlayCtrl.PlayM4_SetDecCallBackEx(playPort, decodeCallback, IntPtr.Zero,0);
        if (value <= 0)
            return GetLastError();
        var playInfo = new ChcNetSdk.NetDvrPreviewInfo
        {
            hPlayWnd = IntPtr.Zero,
            lChannel = 1,          // 通道号，IPC一般1
            dwStreamType = 1,      // 1-主码流，2-子码流
            dwLinkMode = 0,        //0：TCP方式,1：UDP方式,2：多播方式,3 - RTP方式，4-RTP/RTSP,5-RSTP/HTTP
            bBlocked = true,      //0-非阻塞取流, 1-阻塞取流, 如果阻塞SDK内部connect失败将会有5s的超时才能够返回,不适合于轮询取流操作
            dwDisplayBufNum = 1,   //播放库播放缓冲区最大缓冲帧数，范围1-50，置0时默认为1 
            byProtoType = 0,       //应用层取流协议，0-私有协议，1-RTSP协议
            byPreviewMode = 0,     //预览模式，0-正常预览，1-延迟预览
        };
        // 开启预览，传入码流回调
        var playHandle = HikSdk.NET_DVR_RealPlay_V40(iUserId, ref playInfo, realDataCallBack, nint.Zero);
        if (playHandle < 0)
            return GetLastError();
        if (PlayCtrl.PlayM4_Play(playPort, nint.Zero)<=0) //传 IntPtr.Zero 表示软解码，触发回调
            return GetLastError();
        
        return new LoadCamResult
        {
            Code = PublicConst.FlagYes,
            Value = playHandle,
            Tag = playPort,
        };
    }
    public LoadCamResult PreviewInputData(int playPort, nint pBuffer, uint dwBufSize)
    {
        if (PlayCtrl.PlayM4_InputData(playPort, pBuffer, dwBufSize)<=0)
            return GetLastError(); ;
        return new LoadCamResult
        {
            Code = PublicConst.FlagYes,
        };
    }
    public LoadCamResult StopPreview(int iUserId, int iPort, int iRealHandle)
    {
        if (iPort >= 0)
        {
            PlayCtrl.PlayM4_Stop(iPort);
            PlayCtrl.PlayM4_CloseStream(iPort);
            PlayCtrl.PlayM4_FreePort(iPort);
        }
        if (iRealHandle >= 0)
        {
            HikSdk.NET_DVR_StopRealPlay(iRealHandle);
        }
        if (iUserId >= 0)
        {
            HikSdk.NET_DVR_Logout(iUserId);
        }
        return new LoadCamResult
        {
            Code = PublicConst.FlagNo,
        };
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
            Code = value>=0? PublicConst.FlagYes : PublicConst.FlagNo
        };
        return loadCamResult;
    }
    public bool CheckOnLine(int iUserId)
    {
        if (iUserId < 0)
        {
            return false;
        }
        // 检测在线
        var value= HikSdk.NET_DVR_RemoteControl(iUserId,20005,IntPtr.Zero,0);
        return value >= 0;
    }
    private LoadCamResult GetLastError()
    {
        var iLastErr = HikSdk.NET_DVR_GetLastError();
        var loadCamResult = new LoadCamResult
        {
            Code = PublicConst.FlagNo,
            Message = "登录失败，错误代码：" + iLastErr,
            Value = iLastErr,
        };
        return loadCamResult;
    }
}