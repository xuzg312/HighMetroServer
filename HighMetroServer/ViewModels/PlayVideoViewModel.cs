using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HighMetroServer.BaseModel;
using HighMetroServer.HikVision;
using HighMetroServer.Message;
using HighMetroServer.Services;

namespace HighMetroServer.ViewModels;

public partial class PlayVideoViewModel : ObservableObject
{
    private string? _filePath;
    private PlayCtrl.DeccbFun? _decodeCallBack;
    private PlayCtrl.FileEndCallBack? _fileEndCallBack;
    private readonly CamRemoteLinkImpl _camRemoteLinkImpl;
    private bool _isValid;
    private PlayBackState _playState;
    private volatile bool _isClosed;
    private WriteableBitmap? _wbA;
    private WriteableBitmap? _wbB;
    private readonly ConcurrentQueue<FrameData> _frameQueue = new();
    private readonly SemaphoreSlim _frameSignal;
    private readonly MemoryPool<byte> _memoryPool = MemoryPool<byte>.Shared;
    private readonly CancellationTokenSource _cts;
    private Task? _showUiTask;
    private int _frameCounter; 
    
    [ObservableProperty]
    private string _messageText = string.Empty;

    [ObservableProperty]
    private WriteableBitmap? _previewSource;
    
    public PlayVideoViewModel()
    {
        _decodeCallBack = OnDecodedFrameCallBack;
        _fileEndCallBack = OnFileEndCallBack;
        _camRemoteLinkImpl = new CamRemoteLinkImpl();
        _frameSignal = new SemaphoreSlim(0);
        _cts = new CancellationTokenSource();
        _showUiTask = Task.Run(() => SafeHandleLoop(_cts.Token), _cts.Token);
    }
    public void LoadVideo(string filePath)
    {
        if (!File.Exists(filePath))
        {
            MessageText = "视频文件不存在！";
            return;
        }
        _filePath = filePath;
        var loadCamResult = _camRemoteLinkImpl.PlayOpenMp4(_filePath,_decodeCallBack!,_fileEndCallBack!);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = loadCamResult.Message;
            return;
        }
        _isValid = true;
        MessageText = string.Empty;
        NotifyCanExecuteChanged();
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
    private void OnDecodedFrameCallBack(
        int nPort, IntPtr pBuf, int nSize, ref PlayCtrl.FrameInfo frameInfo, IntPtr pUser)
    {
        if (_isClosed) 
            return;
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
                    Dispatcher.UIThread.InvokeAsync(() => { MessageText = $"内存池分片单块不够大！";});
                    return;
                }
            }
            frame = new FrameData(memOwner)
            {
                DataSize = nSize,
                Width = width,
                Height = height,
                YSize = ySize,
                UvSize = uvSize,
                FrameNumber = Interlocked.Increment(ref _frameCounter) 
            };
            _frameQueue.Enqueue(frame);
            _frameSignal.Release();
        }
        catch (Exception ex)
        {
            frame?.Dispose();
            memOwner?.Dispose();
            Dispatcher.UIThread.InvokeAsync(() => { MessageText = $"触发解码回调，处理异常：{ex.Message}";});
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
                    await Dispatcher.UIThread.InvokeAsync(() =>
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
                await Dispatcher.UIThread.InvokeAsync(() =>
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
                    (IntPtr)fb.Address, fb.RowBytes,
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
    private void OnFileEndCallBack(int nPort, IntPtr pUser)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _playState = PlayBackState.Ended;
            NotifyCanExecuteChanged();
        });
    }
    [RelayCommand(CanExecute= nameof(CanPlay))]
    private void Play()
    {
        if (_playState==PlayBackState.Ended)
        {
            var loadCamResult00 = _camRemoteLinkImpl.StopPlayMp4();
            if (!loadCamResult00.Code.Equals(PublicConst.FlagYes))
            {
                MessageText = loadCamResult00.Message;
                return;
            }
            _playState = PlayBackState.Idle;
        }
        var loadCamResult = _playState == PlayBackState.Paused? _camRemoteLinkImpl.PlayPauseMp4(0) : _camRemoteLinkImpl.PlayPlayMp4();
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = loadCamResult.Message;
            return;
        }
        _playState = PlayBackState.Playing;
        NotifyCanExecuteChanged();
    }
    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Pause()
    {
        var loadCamResult = _camRemoteLinkImpl.PlayPauseMp4(1);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = loadCamResult.Message;
            return;
        }
        _playState=PlayBackState.Paused;
        NotifyCanExecuteChanged();
    }
    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        var loadCamResult = _camRemoteLinkImpl.StopPlayMp4();
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = loadCamResult.Message;
            return;
        }
        _playState=PlayBackState.Idle;
        NotifyCanExecuteChanged();
    }
    [RelayCommand]
    private void Exit()
    {
        WeakReferenceMessenger.Default.Send(new ClosePlayVideoViewMessage());
    }
    private bool CanPlay()
    {
        return _isValid && _playState is not PlayBackState.Playing;
    }
    private bool CanStop()
    {
        return _isValid && _playState is PlayBackState.Playing; 
    }
    private void NotifyCanExecuteChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            PlayCommand.NotifyCanExecuteChanged();
            PauseCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
        });
    }
    public void ReleaseResource()
    {
        Console.WriteLine("释放PlayVideoViewModel->ReleaseResource！");
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
            _frameSignal.Release();
        }
        catch
        {
            //忽略；
        }
        _camRemoteLinkImpl.Close();
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
        _decodeCallBack = null;
        _fileEndCallBack = null;
        _frameSignal.Dispose();
    }
}