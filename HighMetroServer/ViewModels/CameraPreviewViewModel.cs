using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Threading;
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

public partial class CameraPreviewViewModel : ObservableRecipient,IRecipient<AppCleanupMessage>
{
    private readonly ConcurrentQueue<byte[]> _bufferPool = new();
    private int _isRendering;
    
    [ObservableProperty]
    private ulong _frameVersion; 
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
        StatusText = string.Empty;
        OpenCommand.NotifyCanExecuteChanged();
        SnapCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();
    }
    private void OnRealDataReceived(
        int lRealHandle, uint dwDataType, nint pBuffer, uint dwBufSize, nint pUser)
    {
        if (dwBufSize == 0) return;
        if(dwDataType != 1 && dwDataType != 2)
            return;
        var loadCamResult = _camRemoteLinkImpl.PreviewInputData(pBuffer, dwBufSize);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            Dispatcher.UIThread.Post(() => { MessageText = loadCamResult.Message; });
        }
    }

    private void OnDecodedFrameCallback(
        int nPort, IntPtr pBuf, int nSize, ref PlayCtrl.FrameInfo frameInfo, IntPtr pUser)
    {
        var width = frameInfo.NWidth;
        var height = frameInfo.NHeight;
        if (frameInfo.NType != 3 || width <= 0 || height <= 0 || pBuf == IntPtr.Zero || nSize <= 0)
            return;
        if (Interlocked.CompareExchange(ref _isRendering, 1, 0) != 0)
            return;
        var bgraSize = width * height * 4;
        // 【后台线程操作】从内存池获取或创建新缓冲区
        if (!_bufferPool.TryDequeue(out var bgraBuffer) || bgraBuffer.Length < bgraSize)
        {
            bgraBuffer = new byte[bgraSize];
        }
        // 【核心优化】在后台线程完成耗时的 SIMD 转换，彻底解放 UI 线程
        unsafe
        {
            var src = (byte*)pBuf.ToPointer();
            var yPlaneSize = width * height;
            var uvPlaneSize = width * height / 4;
            var yPtr = src;
            var vPtr = src + yPlaneSize;
            var uPtr = src + yPlaneSize + uvPlaneSize;
            ConvertYuvToBgraSimd(yPtr, uPtr, vPtr, bgraBuffer, width, height);
        }
        // 切换到 UI 线程，只做最轻量的内存拷贝
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (PreviewSource == null || PreviewSource.PixelSize.Width != width ||
                    PreviewSource.PixelSize.Height != height)
                {
                    PreviewSource = new WriteableBitmap(
                        new PixelSize(width, height),
                        new Vector(96, 96),
                        PixelFormat.Bgra8888);
                }
                using (var fb = PreviewSource.Lock())
                {
                    unsafe
                    {
                        fixed (byte* src = bgraBuffer)
                        {
                            // UI线程只做极速的内存拷贝
                            Buffer.MemoryCopy(src, fb.Address.ToPointer(), fb.RowBytes * height, bgraSize);
                        }
                    }
                }
                // 触发 Avalonia 重绘
                var temp = PreviewSource;
                PreviewSource = null;
                PreviewSource = temp;
                FrameVersion++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"渲染视频帧异常: {ex.Message}");
            }
            finally
            {
                // 【核心】无论渲染成功还是失败，都必须归还缓冲区并重置标志位
                _bufferPool.Enqueue(bgraBuffer);
                Interlocked.Exchange(ref _isRendering, 0);
            }
        });
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void ConvertYuvToBgraSimd(byte* yPtr, byte* uPtr, byte* vPtr, byte[] bgraBuffer, int width,
        int height)
    {
        fixed (byte* dst = bgraBuffer)
        {
            var dstStride = width * 4;
            // 预定义 SIMD 常量
            var v128 = Vector128.Create(128.0f);
            var v1402 = Vector128.Create(1.402f);
            var v0344 = Vector128.Create(0.344f);
            var v0714 = Vector128.Create(0.714f);
            var v1772 = Vector128.Create(1.772f);
            var v255 = Vector128.Create(255.0f);
            var v0 = Vector128<float>.Zero;

            for (var y = 0; y < height; y++)
            {
                var dstRow = dst + y * dstStride;
                var uvRow = y / 2;
                var yRow = yPtr + y * width;
                var uRow = uPtr + uvRow * (width / 2);
                var vRow = vPtr + uvRow * (width / 2);

                var x = 0;
                for (; x <= width - 4; x += 4)
                {
                    var uvCol = x / 2;
                    var yVec = Vector128.Create(yRow[x], yRow[x + 1], yRow[x + 2],
                        (float)yRow[x + 3]);
                    var uVal = uRow[uvCol] - 128.0f;
                    var vVal = vRow[uvCol] - 128.0f;
                    var uVec = Vector128.Create(uVal);
                    var vVec = Vector128.Create(vVal);

                    var rVec = yVec + v1402 * vVec;
                    var gVec = yVec - v0344 * uVec - v0714 * vVec;
                    var bVec = yVec + v1772 * uVec;

                    rVec = Vector128.Min(Vector128.Max(rVec, v0), v255);
                    gVec = Vector128.Min(Vector128.Max(gVec, v0), v255);
                    bVec = Vector128.Min(Vector128.Max(bVec, v0), v255);

                    dstRow[x * 4 + 0] = (byte)bVec.GetElement(0);
                    dstRow[x * 4 + 1] = (byte)gVec.GetElement(0);
                    dstRow[x * 4 + 2] = (byte)rVec.GetElement(0);
                    dstRow[x * 4 + 3] = 255;
                    dstRow[x * 4 + 4] = (byte)bVec.GetElement(1);
                    dstRow[x * 4 + 5] = (byte)gVec.GetElement(1);
                    dstRow[x * 4 + 6] = (byte)rVec.GetElement(1);
                    dstRow[x * 4 + 7] = 255;
                    dstRow[x * 4 + 8] = (byte)bVec.GetElement(2);
                    dstRow[x * 4 + 9] = (byte)gVec.GetElement(2);
                    dstRow[x * 4 + 10] = (byte)rVec.GetElement(2);
                    dstRow[x * 4 + 11] = 255;
                    dstRow[x * 4 + 12] = (byte)bVec.GetElement(3);
                    dstRow[x * 4 + 13] = (byte)gVec.GetElement(3);
                    dstRow[x * 4 + 14] = (byte)rVec.GetElement(3);
                    dstRow[x * 4 + 15] = 255;
                }
                // 边缘处理
                for (; x < width; x++)
                {
                    var uvCol = x / 2;
                    int c = yRow[x], d = uRow[uvCol] - 128, e = vRow[uvCol] - 128;
                    var rVal = c + (int)(1.402f * e);
                    var gVal = c - (int)(0.344f * d) - (int)(0.714f * e);
                    var bVal = c + (int)(1.772f * d);
                    var idx = x * 4;
                    dstRow[idx] = (byte)Math.Clamp(bVal, 0, 255);
                    dstRow[idx + 1] = (byte)Math.Clamp(gVal, 0, 255);
                    dstRow[idx + 2] = (byte)Math.Clamp(rVal, 0, 255);
                    dstRow[idx + 3] = 255;
                }
            }
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
        StatusText = "等待连接摄像头！";
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
        PreviewSource?.Dispose();
        WeakReferenceMessenger.Default.UnregisterAll(this);
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