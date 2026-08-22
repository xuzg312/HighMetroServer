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
    private readonly object _frameLock = new();
    private byte[]? _latestBgraFrame;
    private WriteableBitmap? _previewBitmap;
    private bool _useA = true;
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
        //初始化；
        var loadCamResult00 = _camRemoteLinkImpl.Init();
        if (!loadCamResult00.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = "摄像头初始化失败！";
            return;
        }
        //尝试登录;
        var loadCamResult = _camRemoteLinkImpl.Login(_hardInfo);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = loadCamResult.Message;
            return;
        }
        //打开实时预览；
        loadCamResult = _camRemoteLinkImpl.StartPreview(_realDataCallback,_decodeCallback);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = loadCamResult.Message;
            return;
        }
        _start = true;
        CamState = "【 摄像头连接状态：✅ 】";
        OpenCommand.NotifyCanExecuteChanged();
        SnapCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();
    }
    private void OnRealDataReceived(
        int lRealHandle, uint dwDataType, nint pBuffer, uint dwBufSize, nint pUser)
    {
        Console.WriteLine($"OnRealDataReceived:dwDataType:{dwDataType},dwBufSize:{dwBufSize}");
        if (dwBufSize == 0) return;
        if(dwDataType != 2)
            return;
        var loadCamResult = _camRemoteLinkImpl.PreviewInputData(pBuffer, dwBufSize);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            Dispatcher.UIThread.Post(() => { MessageText = loadCamResult.Message; });
        }
    }
    private void OnDecodedFrameCallback(
        int nPort, IntPtr pBuf, int nSize, PlayCtrl.FrameInfo frameInfo, IntPtr pUser)
    {
        Console.WriteLine($"OnDecodedFrameCallback: Port={nPort}, Size={nSize}, W={frameInfo.NWidth}, H={frameInfo.NHeight}, Type={frameInfo.NType}");

        int w = frameInfo.NWidth;
        int h = frameInfo.NHeight;
        if (w <= 0 || h <= 0 || pBuf == IntPtr.Zero || nSize <= 0)
            return;
        try
        {
            byte[] bgra = I420ToBgraRemovePadding(pBuf, w, h);
            lock (_frameLock)
            {
                _latestBgraFrame = bgra;
            }

            // 调度到UI线程渲染
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                RenderFrameToBitmap(w, h);
            }, Avalonia.Threading.DispatcherPriority.Normal);
        }
        catch (Exception ex)
        {
            MessageText = $"解码转换异常：{ex.Message}";
        }
    }

    private byte[] I420ToBgraRemovePadding(IntPtr srcPtr, int w, int h)
    {
        // PlayM4 默认 16字节对齐 stride
        int strideY = ((w + 15) / 16) * 16;
        int strideUV = (((w / 2) + 15) / 16) * 16;

        int ySize = w * h;
        int uvPlaneSize = (w / 2) * (h / 2);
        byte[] cleanI420 = new byte[ySize + uvPlaneSize * 2];

        // Y平面拷贝，跳过padding（修复原版指针运算BUG）
        IntPtr ySrc = srcPtr;
        int dstY = 0;
        for (int line = 0; line < h; line++)
        {
            Marshal.Copy(ySrc, cleanI420, dstY, w);
            ySrc += strideY;
            dstY += w;
        }

        // U平面
        IntPtr uSrc = srcPtr + strideY * h;
        int dstU = ySize;
        for (int line = 0; line < h / 2; line++)
        {
            Marshal.Copy(uSrc, cleanI420, dstU, w / 2);
            uSrc += strideUV;
            dstU += w / 2;
        }

        // V平面
        IntPtr vSrc = uSrc;
        int dstV = ySize + uvPlaneSize;
        for (int line = 0; line < h / 2; line++)
        {
            Marshal.Copy(vSrc, cleanI420, dstV, w / 2);
            vSrc += strideUV;
            dstV += w / 2;
        }

        // I420 -> BGRA8888
        byte[] bgraResult = new byte[w * h * 4];
        int yBase = 0;
        int uBase = ySize;
        int vBase = ySize + uvPlaneSize;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                byte yVal = cleanI420[yBase + y * w + x];
                int uvRow = y / 2;
                int uvCol = x / 2;

                byte uVal = cleanI420[uBase + uvRow * (w / 2) + uvCol];
                byte vVal = cleanI420[vBase + uvRow * (w / 2) + uvCol];

                int c = yVal - 16;
                int d = uVal - 128;
                int e = vVal - 128;

                int r = (298 * c + 409 * e + 128) >> 8;
                int g = (298 * c - 100 * d - 208 * e + 128) >> 8;
                int b = (298 * c + 516 * d + 128) >> 8;

                r = Math.Clamp(r, 0, 255);
                g = Math.Clamp(g, 0, 255);
                b = Math.Clamp(b, 0, 255);

                int idx = (y * w + x) * 4;
                bgraResult[idx + 0] = (byte)b;
                bgraResult[idx + 1] = (byte)g;
                bgraResult[idx + 2] = (byte)r;
                bgraResult[idx + 3] = 0xFF;
            }
        }
        return bgraResult;
    }

    private void RenderFrameToBitmap(int w, int h)
    {
        byte[]? frame;
        lock (_frameLock)
        {
            frame = _latestBgraFrame;
            _latestBgraFrame = null;
        }

        if (frame == null) return;

        try
        {
            // 尺寸变化重建位图
            if (_previewBitmap == null ||
                _previewBitmap.PixelSize.Width != w ||
                _previewBitmap.PixelSize.Height != h)
            {
                _previewBitmap = new WriteableBitmap(
                    new PixelSize(w, h),
                    new Vector(96, 96),
                    PixelFormats.Bgra8888,
                    AlphaFormat.Opaque);
                PreviewSource = _previewBitmap;
            }

            // 锁内存写入，Avalonia自动刷新画布
            using var buf = _previewBitmap.Lock();
            Marshal.Copy(frame, 0, buf.Address, frame.Length);

            IsNoVideo = false;
            StatusText = string.Empty;
        }
        catch (Exception ex)
        {
            MessageText = $"UI渲染异常：{ex.Message}";
        }
    }
    [RelayCommand(CanExecute = nameof(CanSnap))]
    private void Snap()
    {
        var loadCamResult = _camRemoteLinkImpl.DebugCaptureJpegPicture(); 
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
        var loadCamResult = _camRemoteLinkImpl.Close();
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = "退出登录失败！";
        }
        _start = false;
        StopPreview();
        CamState = "【 摄像头连接状态：❌ 】";
        OpenCommand.NotifyCanExecuteChanged();
        SnapCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();
    }
    [RelayCommand]
    private void Exit()
    {
        if (_start)
        {
            var loadCamResult = _camRemoteLinkImpl.Close();
            if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
            {
                MessageText = "退出登录失败！";
            }
            _start = false;            
        }
        OnClose?.Invoke();
    }
    private bool CanOpen()
    {
        return !_start; 
    }
    private bool CanSnap()
    {
        return _start; 
    }
    private bool CanClose()
    {
        return _start; 
    }
    private void StopPreview()
    {
        _previewBitmap = null;
        PreviewSource = null;
        SnapshotSource = null;
        IsNoVideo = true;
        StatusText = string.Empty;
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
            _camRemoteLinkImpl.Close();
        }
    }
    public void Receive(AppCleanupMessage message)
    {
        Console.WriteLine("释放---摄像头调试----CameraPreviewViewModel！");
        WeakReferenceMessenger.Default.UnregisterAll(this);
        ClearResource();
    }
}