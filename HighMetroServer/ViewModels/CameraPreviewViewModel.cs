using System;
using System.IO;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HighMetroServer.BaseModel;
using HighMetroServer.HikVision;
using HighMetroServer.Message;
using HighMetroServer.Models;
using HighMetroServer.Services;

namespace HighMetroServer.ViewModels;

public partial class CameraPreviewViewModel : ObservableObject,IRecipient<AppCleanupMessage>
{
    public event Action? OnClose;

    [ObservableProperty] 
    private string _camState;
    
    [ObservableProperty]
    private string _ip = string.Empty;

    [ObservableProperty]
    private int _port = 3000;
    
    [ObservableProperty]
    private string _userName = string.Empty;
    
    [ObservableProperty]
    private string _messageText = string.Empty;

    // 实时预览图
    [ObservableProperty]
    private WriteableBitmap? _previewSource;

    // 抓拍静态图
    [ObservableProperty]
    private IImage? _snapshotSource;
    
    [ObservableProperty]
    private string _statusText = "等待连接摄像头";

    [ObservableProperty]
    private bool _isNoVideo = true;

    [ObservableProperty]
    private string _snapshotTip = "暂无抓拍图";

    [ObservableProperty]
    private bool _isNoSnapshot = true;

    // 画面拉伸模式，默认Uniform保持比例（推荐）
    [ObservableProperty]
    private Stretch _stretchMode = Stretch.Uniform;
    
    private readonly HardInfo _hardInfo;
    private bool _start;
    private readonly CamRemoteLinkImpl _camRemoteLinkImpl;
    private int _lUserId = -1;
    private int _lRealPlayHandle = -1;
    private int _playPort = -1; // PlayCtrl解码端口
    private readonly RealDataCallBack _realDataCallback;
    private readonly PlayCtrl.DeccbFun _decodeCallback;
    public CameraPreviewViewModel(HardInfo hardInfo,ResultInfo resultInfo)
    {
        CamState = "【 摄像头连接状态：❌ 】";
        _hardInfo= hardInfo;
        if (!resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = resultInfo.Message;
        }
        Ip = hardInfo.Ip;
        Port = hardInfo.Port;
        UserName = hardInfo.UserName;
        _camRemoteLinkImpl = new CamRemoteLinkImpl();
        _start = false;
        _realDataCallback = OnRealDataReceived;
        _decodeCallback = OnDecodedFrameCallback;
        WeakReferenceMessenger.Default.RegisterAll(this);
    }
    [RelayCommand(CanExecute = nameof(CanOpen))]
    private void Open()
    {
        if(!CheckValid())
            return;
        if (HikPlatform.IsMac)
        {
            MessageText = "MAC环境，不支持此操作，请切换到：Windows/Linux环境测试！";
            return;        
        }
        //尝试连接摄像机；
        if (!InitDrive.InitSign)
        {
            //初始化；
            var loadCamResult00 = _camRemoteLinkImpl.Init();
            if (!loadCamResult00.Code.Equals(PublicConst.FlagYes))
            {
                MessageText = "摄像头初始化失败！";
                return;
            }
            InitDrive.InitSign = true;
        }
        //尝试登录;
        var loadCamResult = _camRemoteLinkImpl.Login(_hardInfo);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = loadCamResult.Message;
            return;
        }
        _lUserId = loadCamResult.Value;
        //打开实时预览；
        loadCamResult = _camRemoteLinkImpl.StartPreview(_lUserId, _realDataCallback,_decodeCallback);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = loadCamResult.Message;
            return;
        }
        _lRealPlayHandle = loadCamResult.Value;
        _playPort= loadCamResult.Tag;
        _start = true;
        CamState = "【 摄像头连接状态：✅ 】";
        OpenCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();
    }
    private void OnRealDataReceived(
        int lRealHandle, uint dwDataType, nint pBuffer, uint dwBufSize, nint pUser)
    {
        if (_playPort < 0 || dwBufSize == 0) return;
        if(dwDataType != CamConst.NetDvrStreamData)
            return;
        // 送入H264/H265码流解码
        //送入其他数据 Input the other data
        var pushResult = false;
        LoadCamResult? loadCamResult=null;
        for (var i = 0; i < 20; i++)
        {
            loadCamResult = _camRemoteLinkImpl.PreviewInputData(_playPort, pBuffer, dwBufSize);
            if (loadCamResult.Code.Equals(PublicConst.FlagYes))
            {
                pushResult = true;
                break;
            }
            if (loadCamResult.Value != 11)
            {
                break;
            }
        }
        if (!pushResult)
        {
            Dispatcher.UIThread.Post(() => { MessageText = loadCamResult!.Message; });
        }
    }
    private void OnDecodedFrameCallback(
        int nPort, IntPtr pBuf, int nSize, ref PlayCtrl.FrameInfo pFrameInfo, 
        int nReserved1, int nReserved2)
    {
        var pixType = pFrameInfo.NType;
        var w = pFrameInfo.NWidth;
        var h = pFrameInfo.NHeight;
        if (w <= 0 || h <= 0 || pBuf == IntPtr.Zero || nSize <= 0)
            return;
        //核心判断：nType == 3 代表 YUV420P 格式,
        //海康播放库默认输出 YUV420P 格式，而 Avalonia 的 WriteableBitmap 需要 RGB 格式
        if(pixType != 3)
            return;
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var pixelSize = new PixelSize(w, h);
                var dpiVec = new Vector(96, 96);
                // BGRA8888原生帧
                var wb = new WriteableBitmap(pixelSize, dpiVec, PixelFormats.Bgra8888, AlphaFormat.Opaque);
                using var fb = wb.Lock();
                var bgraRaw = new byte[nSize];
                Marshal.Copy(pBuf, bgraRaw, 0, nSize);
                Marshal.Copy(bgraRaw, 0, fb.Address, nSize);
                PreviewSource = wb;
                IsNoVideo = false;
                StatusText = string.Empty;
            }
            catch(Exception ex)
            {
                // 异常静默丢弃坏帧，不中断预览
                MessageText = $"视频解码异常：【{ex.Message}】";
            }
        });
    }
    [RelayCommand(CanExecute = nameof(CanClose))]
    private void Snap()
    {
        var loadCamResult = _camRemoteLinkImpl.DebugCaptureJpegPicture(_lUserId); 
        (SnapshotSource as Bitmap)?.Dispose();
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            SnapshotSource = null;
            IsNoSnapshot = true;
            SnapshotTip = $"拍照失败！{loadCamResult.Message}";
            return;
        }    
        using var ms = new MemoryStream(loadCamResult.ImageData);
        SnapshotSource = new Bitmap(ms);
        IsNoSnapshot = false;
    }
    [RelayCommand(CanExecute= nameof(CanClose))]
    private void Close()
    {
        var loadCamResult = _camRemoteLinkImpl.StopPreview(_lUserId,_playPort,_lRealPlayHandle);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = "退出登录失败！";
        }
        _lUserId = -1;
        _playPort = -1;
        _lRealPlayHandle = -1;
        _start = false;
        CamState = "【 摄像头连接状态：❌ 】";
        OpenCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();
    }
    [RelayCommand]
    private void Exit()
    {
        if (_start)
        {
            var loadCamResult = _camRemoteLinkImpl.StopPreview(_lUserId,_playPort,_lRealPlayHandle);
            if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
            {
                MessageText = "退出登录失败！";
            }
            _lUserId = -1;
            _playPort = -1;
            _lRealPlayHandle = -1;
            _start = false;            
        }
        OnClose?.Invoke();
    }
    private bool CanOpen()
    {
        return !_start; 
    }
    private bool CanClose()
    {
        return _start; 
    }
    private bool CheckValid()
    {
        if (string.IsNullOrWhiteSpace(_hardInfo.Ip) ||
            _hardInfo.Port <= 0 ||
            string.IsNullOrWhiteSpace(_hardInfo.UserName) ||
            string.IsNullOrWhiteSpace(_hardInfo.PassWord))
        {
            MessageText = "摄像机参数配置不完整，请先维护像机参数！";
            return false;
        }
        return true;
    }
    private void ClearResource()
    {
        if (_start)
        {
            _camRemoteLinkImpl.StopPreview(_lUserId, _playPort, _lRealPlayHandle);
        }
    }
    public void Receive(AppCleanupMessage message)
    {
        ClearResource();
    }
    public void Unsubscribe()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}