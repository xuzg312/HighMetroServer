using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
    private RealDataCallBack? _realDataCallback;
    private PlayCtrl.DeccbFun? _decodeCallback;
    
    private WriteableBitmap? _wbA;
    private WriteableBitmap? _wbB;
    private readonly ConcurrentQueue<FrameData> _frameQueue = new();
    private readonly SemaphoreSlim _frameSignal;
    private readonly MemoryPool<byte> _memoryPool = MemoryPool<byte>.Shared;
    private readonly CancellationTokenSource _cts;
    private Task? _showUiTask;
    private volatile bool _isClosed;

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
        _frameSignal = new SemaphoreSlim(0);
        _cts = new CancellationTokenSource();
        _showUiTask = Task.Run(() => SafeHandleLoop(_cts.Token), _cts.Token);
        WeakReferenceMessenger.Default.Register(this);
    }
    private async Task SafeHandleLoop(CancellationToken token)
    {
        try
        {
            await ProcessFrameQueueAsync(token);
        }
        catch (OperationCanceledException)
        {
            Dispatcher.UIThread.Post(() => { MessageText = $"正常关闭";});
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => { MessageText = $"接收循环顶层异常：{ex.Message}";});
        }
    }
    private async Task ProcessFrameQueueAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _frameSignal.WaitAsync(_cts.Token);
                if (!_frameQueue.TryDequeue(out var frame))
                    continue;
                try
                {
                    await RenderFrameAsync(frame);
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (!_isClosed)
                            MessageText = $"渲染异常: {ex.Message}";
                    });
                }
                finally
                {
                    frame.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (!_isClosed)
                        MessageText = $"循环任务异常: {ex.Message}";
                });
            }
        }
    }
    private async Task RenderFrameAsync(FrameData frame)
    {
        var w = frame.Width;
        var h = frame.Height;
        // 在 unsafe 块外获取指针数据
        IntPtr yPtr, uPtr, vPtr;
        unsafe
        {
            var yPtrRaw = frame.GetYPtr();
            var uPtrRaw = frame.GetUPtr(); // U分量
            var vPtrRaw = frame.GetVPtr(); // V分量
            if (yPtrRaw == null || uPtrRaw == null || vPtrRaw == null)
                return;
            yPtr = (IntPtr)yPtrRaw;
            uPtr = (IntPtr)uPtrRaw;
            vPtr = (IntPtr)vPtrRaw;
        }
        // 在UI线程上更新WriteableBitmap
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_isClosed)
                return;
            // 初始化或重建Bitmap
            var useA = ReferenceEquals(PreviewSource, _wbA);
            var useSource = useA ? _wbB : _wbA;
            if (useSource == null || useSource.PixelSize.Width != w || useSource.PixelSize.Height != h)
            {
                useSource?.Dispose();
                useSource = new WriteableBitmap(new PixelSize(w, h), new Vector(96, 96), PixelFormat.Bgra8888);
                if (useA)
                    _wbB = useSource;
                else
                    _wbA = useSource;
            }
            using (var fb = useSource.Lock())
            {
                var ret = LibYuv.I420ToARGB(
                    yPtr, w,
                    uPtr, w / 2,
                    vPtr, w / 2, 
                    fb.Address, fb.RowBytes,
                    w, h);
                if (ret < 0)
                {
                    MessageText = $"转换失败：{ret}";
                    return;
                }
            }
            PreviewSource = useSource;
        }, DispatcherPriority.Background);
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
        loadCamResult = _camRemoteLinkImpl.StartPreview(_realDataCallback!,_decodeCallback!);
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
        var ySize = width * height; 
        var uvSize = (width / 2) * (height / 2); 
        if (nSize < ySize + uvSize * 2) return;
        // 1. 从内存池租借一块内存
        IMemoryOwner<byte>? memOwner = null;
        FrameData? frame = null;
        try
        {
            memOwner = _memoryPool.Rent(nSize);
            var destSpan = memOwner.Memory.Span;
            unsafe
            {
                if (nSize <= destSpan.Length)
                {
                    var source = new Span<byte>((void*)pBuf, nSize);
                    source.CopyTo(destSpan.Slice(0, nSize));
                }
                else
                {
                    // 内存池分片导致单块不够大时，直接丢弃或降级处理
                    memOwner.Dispose();
                    Dispatcher.UIThread.Post(() => { MessageText = $"内存池分片单块不够大！";});
                    return;
                }
            }
            frame = new FrameData(memOwner)
            {
                Width = width,
                Height = height,
                YSize = ySize,
                UvSize = uvSize,
            };
            _frameQueue.Enqueue(frame);
            _frameSignal.Release();
        }
        catch (Exception ex)
        {
            frame?.Dispose();
            memOwner?.Dispose();
            Dispatcher.UIThread.Post(() => { MessageText = $"触发解码回调，处理异常：{ex.Message}";});
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
        ClearResource();
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
        Console.WriteLine("释放---摄像头调试----CameraPreviewViewModel！");
        if (_start)
        {
            _camRemoteLinkImpl.Close();
        }
        if (_isClosed) 
            return;
        _isClosed = true;
        try
        {
            _cts.Cancel();
        }
        catch
        {
            //忽略；
        }
        try
        {
            _showUiTask?.Wait(500);
        }
        catch (Exception)
        {
            //忽略;
        }
        while (_frameQueue.TryDequeue(out var frame))
        {
            try
            {
                frame.Dispose();
            }
            catch
            {
                //忽略；
            }
        }
        try
        {
            _cts.Dispose();
        }
        catch
        {
            //忽略；
        }
        try
        {
            _frameSignal.Dispose();
        }
        catch
        {
            //忽略；
        }
        _showUiTask = null;
        _wbA?.Dispose();
        _wbB?.Dispose();
        _wbA = null;
        _wbB = null;
        PreviewSource = null;
        _realDataCallback = null;
        _decodeCallback = null;
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
    public void Receive(AppCleanupMessage message)
    {
        ClearResource();
    }
}