using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetroServer.BaseModel;
using HighMetroServer.HikVision;
using HighMetroServer.Services;

namespace HighMetroServer.ViewModels;

public partial class PlayVideoViewModel : ObservableObject
{
    private string? _filePath;
    private readonly PlayCtrl.DeccbFun _decodeCallBack;
    private readonly PlayCtrl.FileEndCallBack _fileEndCallBack;
    private readonly CamRemoteLinkImpl _camRemoteLinkImpl;
    private bool _isValid;
    private bool _isRunIng;
    private bool _isPause;
    private readonly ConcurrentQueue<byte[]> _bufferPool = new();
    private int _isRendering;
    private volatile bool _isClosed = false;
    private WriteableBitmap? _wbA;
    private WriteableBitmap? _wbB;
    
    [ObservableProperty]
    private string _messageText = string.Empty;

    [ObservableProperty]
    private WriteableBitmap? _previewSource;
    
    public PlayVideoViewModel()
    {
        _decodeCallBack = OnDecodedFrameCallBack;
        _fileEndCallBack = OnFileEndCallBack;
        _camRemoteLinkImpl = new CamRemoteLinkImpl();
    }
    public void LoadVideo(string filePath)
    {
        if (!File.Exists(filePath))
        {
            MessageText = "视频文件不存在！";
            return;
        }
        _filePath = filePath;
        var loadCamResult = _camRemoteLinkImpl.PlayOpenMp4(_filePath,_decodeCallBack,_fileEndCallBack);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = loadCamResult.Message;
            return;
        }
        _isValid = true;
        _filePath = filePath;
        MessageText = string.Empty;
        NotifyCanExecuteChanged();
    }
    [RelayCommand(CanExecute= nameof(CanPlay))]
    private void Play()
    {
        LoadCamResult loadCamResult;
        if (!_isRunIng)
        {
            loadCamResult = _camRemoteLinkImpl.PlayPlayMp4();
        }
        else
        {
            loadCamResult = _camRemoteLinkImpl.PlayPauseMp4(0);
        }
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = loadCamResult.Message;
            return;
        }
        _isRunIng = true;
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
        _isPause = true;
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
        _isPause = false;
        _isRunIng = false;
        NotifyCanExecuteChanged();
    }
    [RelayCommand]
    private void Exit()
    {
        _camRemoteLinkImpl.Close();
        _isClosed = true;
        Interlocked.Exchange(ref _isRendering, 0);
        _wbA?.Dispose();
        _wbB?.Dispose();
        _wbA = null;
        _wbB = null;
        PreviewSource = null;
    }
    private void OnDecodedFrameCallBack(
        int nPort, IntPtr pBuf, int nSize, ref PlayCtrl.FrameInfo frameInfo, IntPtr pUser)
    {
        Console.WriteLine($"OnDecodedFrameCallBack:nPort:{nPort},nSize:{nSize},frameInfo.NWidth:{frameInfo.NWidth},frameInfo.NHeight:{frameInfo.NHeight},frameInfo.NType:{frameInfo.NType}");
        if (_isClosed)
            return;
        var width = frameInfo.NWidth;
        var height = frameInfo.NHeight;
        if (frameInfo.NType != 3 || width <= 0 || height <= 0 || pBuf == IntPtr.Zero || nSize <= 0)
            return;
        if (Interlocked.CompareExchange(ref _isRendering, 1, 0) != 0)
            return;
        try
        {
            int yPlaneSize = width * height;
            int uvPlaneSize = width * height / 4;
            int bgraSize = width * height * 4;
            unsafe
            {
                byte* src = (byte*)pBuf.ToPointer();
                byte* yPtr = src;
                byte* vPtr = src + yPlaneSize;
                byte* uPtr = src + yPlaneSize + uvPlaneSize;
                if (!_bufferPool.TryDequeue(out var tempConvertBuffer) || tempConvertBuffer.Length < bgraSize)
                {
                    tempConvertBuffer = new byte[bgraSize];
                }
                fixed (byte* dst = tempConvertBuffer)
                {
                    LibYuv.I420ToBGRA(
                        (IntPtr)yPtr, width,
                        (IntPtr)vPtr, width / 2,
                        (IntPtr)uPtr, width / 2,
                        (IntPtr)dst, width * 4,
                        width, height);
                }
                byte[] uiFrame = new byte[bgraSize];
                Buffer.BlockCopy(tempConvertBuffer, 0, uiFrame, 0, bgraSize);
                _bufferPool.Enqueue(tempConvertBuffer);
                // 把需要的局部变量提前捕获，不要在lambda内部访问回调栈变量
                var capWidth = width;
                var capHeight = height;
                var capBgraSize = bgraSize;
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        if (_isClosed) return;
                        //尺寸变更，重建双缓冲
                        if (_wbA == null || _wbA.PixelSize.Width != capWidth || _wbA.PixelSize.Height != capHeight)
                        {
                            //释放旧位图，防止显存泄漏
                            _wbA?.Dispose();
                            _wbB?.Dispose();

                            _wbA = new WriteableBitmap(
                                new PixelSize(capWidth, capHeight),
                                new Vector(96, 96),
                                PixelFormat.Bgra8888);

                            _wbB = new WriteableBitmap(
                                new PixelSize(capWidth, capHeight),
                                new Vector(96, 96),
                                PixelFormat.Bgra8888);

                            // ✅关键修复：第一次初始化，给PreviewSource赋初始值
                            PreviewSource = _wbA;
                        }

                        // 选后台未显示的bitmap进行写入
                        WriteableBitmap writeBmp;
                        if (PreviewSource == _wbA)
                        {
                            writeBmp = _wbB!;
                        }
                        else
                        {
                            writeBmp = _wbA!;
                        }
                        using (var fb = writeBmp.Lock())
                        {
                            fixed (byte* pSrc = uiFrame)
                            {
                                long copyLen = Math.Min(capBgraSize, fb.RowBytes * capHeight);
                                Buffer.MemoryCopy(pSrc, fb.Address.ToPointer(), fb.RowBytes * capHeight, copyLen);
                            }
                        }
                        //切换显示引用，触发UI刷新
                        PreviewSource = writeBmp;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"渲染帧异常: {ex.Message}");
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _isRendering, 0);
                    }
                });
            }
        }
        catch
        {
            Interlocked.Exchange(ref _isRendering, 0);
        }
    }
    private void OnFileEndCallBack(int nPort, System.IntPtr pUser)
    {
        _isRunIng = false;
        _isPause = false;
        NotifyCanExecuteChanged();
    }
    private bool CanPlay()
    {
        return _isValid && (!_isRunIng || _isPause); 
    }
    private bool CanStop()
    {
        return _isRunIng && !_isPause; 
    }

    private void NotifyCanExecuteChanged()
    {
        PlayCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }
}