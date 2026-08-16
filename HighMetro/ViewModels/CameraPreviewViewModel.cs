using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HighMetro.BaseModel;
using HighMetro.HikVision;
using HighMetro.Message;
using HighMetro.Models;
using HighMetro.Services;

namespace HighMetro.ViewModels;

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
    private WriteableBitmap? _snapshotSource;
    
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
    private bool _decoderInited;
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
        _decoderInited = false;
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
        if (dwDataType == 1U)
        {
            //1：码流头信息，初始化PlayCtrl解码器
            if(_decoderInited)
                return;
            var loadCamResult= _camRemoteLinkImpl.SetPreviewPara(_playPort, pBuffer, dwBufSize);
            if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
            {
                Dispatcher.UIThread.Post(() => { MessageText = loadCamResult.Message; });
                return;
            }
            _decoderInited = true;
        }
        else
        {
            // 送入H264/H265码流解码
            //送入其他数据 Input the other data
            for (var i = 0; i < 999; i++)
            {
                var loadCamResult = _camRemoteLinkImpl.PreviewInputData(_playPort, pBuffer, dwBufSize);
                if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
                {
                    Dispatcher.UIThread.Post(() => { MessageText = loadCamResult.Message; });
                    continue;
                }
                break;
            }
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
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                WriteableBitmap wb;
                var pixelSize = new PixelSize(w, h);
                var dpiVec = new Vector(96, 96);
                switch (pixType)
                {
                    case 1:
                    {
                        // YV12 → BGRA（原有逻辑不变，托管数组中转，安全）
                        var yv12 = new byte[nSize];
                        Marshal.Copy(pBuf, yv12, 0, nSize);
                        var bgra = Yv12ToBgra(yv12, w, h);
                        wb = new WriteableBitmap(pixelSize, dpiVec, PixelFormats.Bgra8888, AlphaFormat.Opaque);
                        using var fb = wb.Lock();
                        Marshal.Copy(bgra, 0, fb.Address, bgra.Length);
                        break;
                    }
                    case 2:
                    {
                        // RGB24：非托管pBuf → WriteableBitmap内存
                        wb = new WriteableBitmap(pixelSize, dpiVec, PixelFormats.Rgb24, AlphaFormat.Opaque);
                        using var fb = wb.Lock();
                        // 方案A：中转byte数组（最稳妥，不需要unsafe）
                        var rgb24 = new byte[nSize];
                        Marshal.Copy(pBuf, rgb24, 0, nSize);
                        Marshal.Copy(rgb24, 0, fb.Address, nSize);
                        break;
                    }
                    case 3:
                    {
                        // BGRA8888原生帧
                        wb = new WriteableBitmap(pixelSize, dpiVec, PixelFormats.Bgra8888, AlphaFormat.Opaque);
                        using var fb = wb.Lock();
                        var bgraRaw = new byte[nSize];
                        Marshal.Copy(pBuf, bgraRaw, 0, nSize);
                        Marshal.Copy(bgraRaw, 0, fb.Address, nSize);
                        break;
                    }
                    default:
                    {
                        Dispatcher.UIThread.Post(() => { MessageText = $"pixType：【{pixType}】类型无效！"; });
                        return;
                    }
                }
                PreviewSource = wb;
                IsNoVideo = false;
                StatusText = string.Empty;
            }
            catch(Exception ex)
            {
                // 异常静默丢弃坏帧，不中断预览
                Dispatcher.UIThread.Post(() => { MessageText = $"视频解码异常：【{ex.Message}】"; });
            }
        });
    }
    private byte[] Yv12ToBgra(byte[] yv12, int width, int height)
    {
        var bgraLen = width * height * 4;
        var bgra = new byte[bgraLen];

        var ySize = width * height;
        var uvSize = ySize / 4;
        var yPos = 0;
        var vPos = ySize;
        var uPos = ySize + uvSize;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var yVal = yv12[yPos++];
                var uvIndex = (y / 2) * (width / 2) + (x / 2);
                var vVal = yv12[vPos + uvIndex];
                var uVal = yv12[uPos + uvIndex];

                var r = yVal + (int)(1.402 * (vVal - 128));
                var g = yVal - (int)(0.34414 * (uVal - 128)) - (int)(0.71414 * (vVal - 128));
                var b = yVal + (int)(1.772 * (uVal - 128));

                r = Math.Clamp(r, 0, 255);
                g = Math.Clamp(g, 0, 255);
                b = Math.Clamp(b, 0, 255);

                var idx = (y * width + x) * 4;
                bgra[idx] = (byte)b;
                bgra[idx + 1] = (byte)g;
                bgra[idx + 2] = (byte)r;
                bgra[idx + 3] = 0xFF;
            }
        }
        return bgra;
    }
    [RelayCommand(CanExecute = nameof(CanClose))]
    private void Snap()
    {
        
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