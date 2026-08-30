using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HighMetroServer.BaseModel;
using HighMetroServer.HikVision;

namespace HighMetroServer.Services;

public class CamRemoteLinkImpl
{
    private int _userId = -1;
    private int _playHandle = -1;
    private int _iPort = -1;
    private int _playPort = -1;
    private static bool _initSign;
    private RealDataCallBack? _realDataCallback;
    private PlayCtrl.DeccbFun? _decodeCallback;
    private PlayCtrl.DeccbFun? _playDecodeCallBack;
    private readonly object _dirLockObj = new();
    private static readonly SemaphoreSlim AsyncLock = new SemaphoreSlim(1, 1);
    public int GetUserId() => _userId;
    public LoadCamResult Init()
    {
        var value = 1;
        if (!_initSign)
        {
            value = HikSdk.NET_DVR_Init();
            if (value >= 0)
                _initSign = true;
        }
        var loadCamResult = new LoadCamResult
        {
            Code = value>=0? PublicConst.FlagYes : PublicConst.FlagNo
        };
        return loadCamResult;
    }
    public LoadCamResult Login(HardInfo hardInfo)
    {
        if (!_initSign)
        {
            return new LoadCamResult
            {
                Code = PublicConst.FlagNo,
                Message = "摄像头初始化失败！",
            };
        }
        if (_userId>=0 || _playHandle>=0)
        {
            return new LoadCamResult
            {
                Code = PublicConst.FlagNo,
                Message = "已处于登录状态，拒绝重复登录！",
            };
        }
        //登录设备；
        var loginInfo = new ChcNetSdk.NetDvrUserLoginInfo();

        //设备IP地址或者域名
        var byIp = Encoding.Default.GetBytes(hardInfo.Ip);
        loginInfo.sDeviceAddress = new byte[129];
        byIp.CopyTo(loginInfo.sDeviceAddress, 0);

        //设备用户名
        var byUserName = Encoding.Default.GetBytes(hardInfo.UserName);
        loginInfo.sUserName = new byte[64];
        byUserName.CopyTo(loginInfo.sUserName, 0);

        //设备密码
        var byPassword = Encoding.Default.GetBytes(hardInfo.PassWord);
        loginInfo.sPassword = new byte[64];
        byPassword.CopyTo(loginInfo.sPassword, 0);

        loginInfo.wPort = (ushort)hardInfo.Port;//设备服务端口号
        loginInfo.bUseAsynLogin = false; //是否异步登录：0- 否，1- 是 

        var deviceInfo = new ChcNetSdk.NetDvrDeviceinfoV40();

        //登录设备 Login the device
        _userId = HikSdk.NET_DVR_Login_V40(ref loginInfo, ref deviceInfo);
        if (_userId >= 0)
        {
            var loadCamResult = new LoadCamResult
            {
                Code = PublicConst.FlagYes,
            };
            return loadCamResult;
        }
        return HikSdkGetLastError();
    }
    public async Task<LoadCamResult> CaptureJpegPicture(CameraBean cameraBean,string baseDirectory)
    {
        if (_userId < 0)
        {
            return new LoadCamResult()
            {
                Code = PublicConst.FlagNo,
                Message = "UserId无效！"
            };
        }
        var bufferPtr = IntPtr.Zero;
        await AsyncLock.WaitAsync();
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
            var fullSavePath = Path.Combine(dayDir, camIdFolder, fileName);
            cameraBean.FilePath = fullSavePath;
            #endregion
            #region 4. SDK抓拍逻辑
            var lpJpegPara = new ChcNetSdk.NetDvrJpegpara
            {
                wPicQuality = 0,
                wPicSize = 0xff
            };
            bufferPtr = Marshal.AllocHGlobal(PublicConst.MaxBufferSize);
            uint actualSize = 0;
            var nativeRet = HikSdk.NET_DVR_CaptureJPEGPicture_NEW(
                _userId,
                1,
                ref lpJpegPara,
                bufferPtr,
                PublicConst.MaxBufferSize,
                ref actualSize);
            // SDK调用失败校验
            if (nativeRet < 0 || actualSize <= 0)
            {
                return HikSdkGetLastError();
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
            AsyncLock.Release();
        }
    }
    public async Task<LoadCamResult> PlayCam(CameraBean cameraBean,string baseDirectory)
    {
        if (_userId < 0)
        {
            return new LoadCamResult()
            {
                Code = PublicConst.FlagNo,
                Message = "UserId无效！"
            };
        }
        if (_playHandle >= 0)
        {
            return new LoadCamResult()
            {
                Code = PublicConst.FlagNo,
                Message = "PlayHandle>=0，此次操作被拒绝！"
            };
        }
        // 静态锁防止多线程并发创建目录冲突
        await AsyncLock.WaitAsync();
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
            var fileName = $"{cameraBean.Serial}-{timeStr}.mp4";
            var fullSavePath = Path.Combine(dayDir, camIdFolder, fileName);
            cameraBean.FilePath = fullSavePath;
            #endregion
            var playInfo = new ChcNetSdk.NetDvrPreviewInfo
            {
                hPlayWnd = IntPtr.Zero,
                lChannel = 1,         
                dwStreamType = 1,     
                dwLinkMode = 0,        
                bBlocked = false,      
                dwDisplayBufNum = 1,  
                byProtoType = 0,       
                byPreviewMode = 0,     
            };
            // 开启预览
            _playHandle = -1;
            try
            {
                _playHandle = HikSdk.NET_DVR_RealPlay_V40(_userId, ref playInfo, null!, nint.Zero);
                if (_playHandle < 0)
                    return HikSdkGetLastError();
                var startRet = HikSdk.NET_DVR_SaveRealData(_playHandle, fullSavePath);
                if (startRet < 0)
                    return HikSdkGetLastError();
                // 等待10秒
                await Task.Delay(TimeSpan.FromSeconds(CamConst.PlayCamTime));
                return new LoadCamResult
                {
                    Code = PublicConst.FlagYes,
                };
            }
            finally
            {
                if (_playHandle >= 0)
                {
                    HikSdk.NET_DVR_StopSaveRealData(_playHandle);
                    // 额外等待一小段时间，确保文件句柄完全释放
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    HikSdk.NET_DVR_StopRealPlay(_playHandle);
                    _playHandle = -1;
                }
            }
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
            if (_playHandle>=0)
            {
                HikSdk.NET_DVR_StopSaveRealData(_playHandle);
                HikSdk.NET_DVR_StopRealPlay(_playHandle);
                _playHandle = -1;
            }
            AsyncLock.Release();
        }
    }
    public LoadCamResult DebugCaptureJpegPicture()
    {
        if (_userId < 0)
        {
            return new LoadCamResult()
            {
                Code = PublicConst.FlagNo,
                Message = "UserId无效！"
            };
        }
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
                _userId,
                lChannel,
                ref lpJpegPara,
                bufferPtr,
                PublicConst.MaxBufferSize,
                ref actualSize);
            // SDK调用失败校验
            if (nativeRet < 0 || actualSize <= 0)
            {
                return HikSdkGetLastError();
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
        RealDataCallBack realDataCallBack,PlayCtrl.DeccbFun decodeCallback)
    {
        _realDataCallback = realDataCallBack;
        _decodeCallback = decodeCallback;
        if (_userId < 0)
        {
            return new LoadCamResult()
            {
                Code = PublicConst.FlagNo,
                Message = "UserId无效！"
            };
        }
        if (_playHandle >= 0)
        {
            return new LoadCamResult()
            {
                Code = PublicConst.FlagNo,
                Message = "PlayHandle>=0，此次操作被拒绝！"
            };
        }
        if (_iPort >= 0)
        {
            return new LoadCamResult()
            {
                Code = PublicConst.FlagNo,
                Message = "IPort>=0，此次操作被拒绝！"
            };
        }
        //获取播放句柄 Get the port to play
        var value = PlayCtrl.PlayM4_GetPort(ref _iPort);
        if(value<0)
            return PlayM4GetLastError();
        //设置流播放模式 Set the stream mode: real-time stream mode
        value = PlayCtrl.PlayM4_SetStreamOpenMode(_iPort, 0);
        if(value<0)
            return PlayM4GetLastError();
        //打开码流，送入头数据 Open stream
        value = PlayCtrl.PlayM4_OpenStream(_iPort, IntPtr.Zero, 0, CamConst.BufPoolSize);
        if(value<0)
            return PlayM4GetLastError();
        //设置显示缓冲区个数 Set the display buffer number
        value = PlayCtrl.PlayM4_SetDisplayBuf(_iPort, CamConst.DisplayBufNumber);
        if(value<0)
            return PlayM4GetLastError(); 
        //设置解码回调函数，获取解码后音视频原始数据 Set callback function of decoded data
        value = PlayCtrl.PlayM4_SetDecCallBackExMend(_iPort, _decodeCallback, IntPtr.Zero, 0,IntPtr.Zero);
        if(value<0)
            return PlayM4GetLastError();
        value = PlayCtrl.PlayM4_SetDecodeEngine(_iPort, 0);
        if(value<0)
            return PlayM4GetLastError();
        var playInfo = new ChcNetSdk.NetDvrPreviewInfo
        {
            hPlayWnd = IntPtr.Zero,
            lChannel = 1,          
            dwStreamType = 1,      
            dwLinkMode = 0,       
            bBlocked = false,     
            dwDisplayBufNum = 1,   
            byProtoType = 0,       
            byPreviewMode = 0,     
        };
        // 开启预览，传入码流回调
        _playHandle = HikSdk.NET_DVR_RealPlay_V40(_userId, ref playInfo, _realDataCallback, nint.Zero);
        if (_playHandle < 0)
            return HikSdkGetLastError();
        value = PlayCtrl.PlayM4_Play(_iPort, nint.Zero); //传 IntPtr.Zero 表示软解码，触发回调
        if(value<0)
            return PlayM4GetLastError();
        
        return new LoadCamResult
        {
            Code = PublicConst.FlagYes,
        };
    }
    public LoadCamResult PreviewInputData(nint pBuffer, uint dwBufSize)
    {
        var value = PlayCtrl.PlayM4_InputData(_iPort, pBuffer, dwBufSize);
        if(value<0)
            return PlayM4GetLastError();
        return new LoadCamResult
        {
            Code = PublicConst.FlagYes
        };
    }
    public LoadCamResult PlayOpenMp4(string fileName,
        PlayCtrl.DeccbFun decodeCallback,PlayCtrl.FileEndCallBack fileEndCallBack)
    {
        _playDecodeCallBack = decodeCallback;
        try
        {
            if (_playPort < 0)
            {
                var value00 = PlayCtrl.PlayM4_GetPort(ref _playPort);
                if (value00 < 0)
                {
                    _playPort = -1;
                    return PlayM4GetLastError();
                }
            }
            var value = PlayCtrl.PlayM4_SetDecCallBackExMend(_playPort, _playDecodeCallBack, IntPtr.Zero, 0,
                IntPtr.Zero);
            if (value < 0)
                return PlayM4GetLastError();
            value = PlayCtrl.PlayM4_SetFileEndCallback(_playPort, fileEndCallBack,IntPtr.Zero);
            if (value < 0)
                return PlayM4GetLastError();
            var ret = PlayCtrl.PlayM4_OpenFile(_playPort, fileName);
            if (!ret)
                return PlayM4GetLastError();
            return new LoadCamResult
            {
                Code = PublicConst.FlagYes,
            };
        }
        catch (Exception ex)
        {
            return new LoadCamResult
            {
                Code = PublicConst.FlagNo,
                Message = ex.Message,
            };
        }
    }
    public LoadCamResult PlayPlayMp4()
    {
        if (_playPort < 0)
        {
            return new LoadCamResult
            {
                Code = PublicConst.FlagNo,
                Message = "PlayPort无效！",
            };
        }
        var value = PlayCtrl.PlayM4_Play(_playPort, nint.Zero); //传 IntPtr.Zero 表示软解码，触发回调
        if (value < 0)
            return PlayM4GetLastError();
        return new LoadCamResult
        {
            Code = PublicConst.FlagYes,
        };
    }
    public LoadCamResult PlayPauseMp4(uint nPause)
    {
        if (_playPort < 0)
        {
            return new LoadCamResult
            {
                Code = PublicConst.FlagNo,
                Message = "PlayPort无效！",
            };
        }
        var value = PlayCtrl.PlayM4_Pause(_playPort, nPause); 
        if (value < 0)
            return PlayM4GetLastError();
        return new LoadCamResult
        {
            Code = PublicConst.FlagYes,
        };
    }
    public LoadCamResult StopPlayMp4()
    {
        if (_playPort < 0)
        {
            return new LoadCamResult
            {
                Code = PublicConst.FlagNo,
                Message = "PlayPort无效！",
            };
        }
        var value = PlayCtrl.PlayM4_Stop(_playPort);
        if (value < 0)
            return PlayM4GetLastError();
        return new LoadCamResult
        {
            Code = PublicConst.FlagYes,
        };
    }
    public LoadCamResult SetPlayPos(uint position)
    {
        if (_playPort < 0)
        {
            return new LoadCamResult
            {
                Code = PublicConst.FlagNo,
                Message = "PlayPort无效！",
            };
        }
        var value = PlayCtrl.PlayM4_SetPlayPos(_playPort,position);
        if (value < 0)
            return PlayM4GetLastError();
        return new LoadCamResult
        {
            Code = PublicConst.FlagYes,
        };
    }
    private void SafeWriteFile(string filePath, byte[] data)
    {
        // FileOptions.None 常规写入；可加 FileOptions.WriteThrough 强制落盘
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        fs.Write(data, 0, data.Length);
        fs.Flush(true);
    }
    public LoadCamResult Logout()
    {
        if (!_initSign || _userId < 0)
        {
            return new LoadCamResult
            {
                Code = PublicConst.FlagNo,
                Message = $"逻辑错误，初始化标志：{_initSign}，登录UserId{_userId}",
            };
        }
        return Close();
    }
    public LoadCamResult Close()
    {
        if (_iPort >= 0)
        {
            PlayCtrl.PlayM4_Stop(_iPort);
            PlayCtrl.PlayM4_CloseStream(_iPort);
            PlayCtrl.PlayM4_FreePort(_iPort);
            _iPort = -1;
        }
        if (_playPort >= 0)
        {
            PlayCtrl.PlayM4_Stop(_playPort);
            PlayCtrl.PlayM4_CloseFile(_playPort);
            PlayCtrl.PlayM4_FreePort(_playPort);
            _playPort = -1;
        }
        if (_playHandle >= 0)
        {
            HikSdk.NET_DVR_StopRealPlay(_playHandle);
            _playHandle = -1;
        }
        if (_userId >= 0)
        {
            HikSdk.NET_DVR_Logout(_userId);
            _userId = -1;
        }
        _realDataCallback = null;
        _playDecodeCallBack=null;
        _decodeCallback = null;
        return new LoadCamResult
        {
            Code = PublicConst.FlagYes,
        };
    }
    public LoadCamResult Clear()
    {
        Close();
        var value = 1;
        if(_initSign)
            value = HikSdk.NET_DVR_Cleanup();
        var loadCamResult = new LoadCamResult
        {
            Code = value>=0? PublicConst.FlagYes : PublicConst.FlagNo
        };
        return loadCamResult;
    }
    public bool CheckOnLine()
    {
        if (_userId < 0)
        {
            return false;
        }
        // 检测在线
        var value= HikSdk.NET_DVR_RemoteControl(_userId,20005,IntPtr.Zero,0);
        return value >= 0;
    }
    private LoadCamResult HikSdkGetLastError()
    {
        var iLastErr = HikSdk.NET_DVR_GetLastError();
        var loadCamResult = new LoadCamResult
        {
            Code = PublicConst.FlagNo,
            Message = "登录失败，错误代码：" + iLastErr,
        };
        return loadCamResult;
    }
    private LoadCamResult PlayM4GetLastError()
    {
        var iLastErr = PlayCtrl.PlayM4_GetLastError(_iPort);
        var loadCamResult = new LoadCamResult
        {
            Code = PublicConst.FlagNo,
            Message = "登录失败，错误代码：" + iLastErr,
        };
        return loadCamResult;
    }
}