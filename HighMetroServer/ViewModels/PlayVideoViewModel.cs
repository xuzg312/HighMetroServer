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
        Console.WriteLine(
            $"OnDecodedFrameCallBack:nPort:{nPort},nSize:{nSize},frameInfo.NWidth:{frameInfo.NWidth},frameInfo.NHeight:{frameInfo.NHeight},frameInfo.NType:{frameInfo.NType}");
        if (_isClosed) return;

        var width = frameInfo.NWidth;
        var height = frameInfo.NHeight;

        // 1. 基础校验
        if (frameInfo.NType != 3 || width <= 0 || height <= 0 || pBuf == IntPtr.Zero || nSize <= 0)
            return;

        // 2. 丢帧保护：如果上一帧还没渲染完，直接丢弃当前帧
        if (Interlocked.CompareExchange(ref _isRendering, 1, 0) != 0)
            return;

        try
        {
            int yPlaneSize = width * height;
            int uvPlaneSize = (width / 2) * (height / 2);
            int bgraSize = width * height * 4;

            unsafe
            {
                byte* src = (byte*)pBuf.ToPointer();

                // 3. 严格按照海康 YV12 的紧凑内存布局提取指针 (Y + V + U)
                byte* yPtr = src;
                byte* vPtr = src + yPlaneSize; // V 平面紧跟 Y 平面
                byte* uPtr = vPtr + uvPlaneSize; // U 平面紧跟 V 平面

                // 4. 从内存池获取缓冲区
                if (!_bufferPool.TryDequeue(out var tempConvertBuffer) || tempConvertBuffer.Length < bgraSize)
                {
                    tempConvertBuffer = new byte[bgraSize];
                }

                fixed (byte* dst = tempConvertBuffer)
                {
                    // 5. 调用 LibYuv 的 I420ToARGB
                    // 注意：参数顺序必须是 Y, U, V (I420顺序)，所以这里传 uPtr 在前，vPtr 在后
                    LibYuv.I420ToARGB(
                        (IntPtr)yPtr, width, // Y 数据, Y 步长
                        (IntPtr)uPtr, width / 2, // U 数据, U 步长
                        (IntPtr)vPtr, width / 2, // V 数据, V 步长
                        (IntPtr)dst, width * 4, // ARGB 目标数据, 目标步长
                        width, height);
                }

                // 6. 捕获局部变量，避免在 lambda 中访问栈变量
                var capWidth = width;
                var capHeight = height;
                var capBgraSize = bgraSize;
                var convertBuffer = tempConvertBuffer;

                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        if (_isClosed) return;

                        // 7. 尺寸变更，重建双缓冲
                        if (_wbA == null || _wbA.PixelSize.Width != capWidth || _wbA.PixelSize.Height != capHeight)
                        {
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

                            PreviewSource = _wbA;
                        }

                        // 8. 选择后台未显示的 bitmap 进行写入
                        WriteableBitmap writeBmp = PreviewSource == _wbA ? _wbB! : _wbA!;

                        using (var fb = writeBmp.Lock())
                        {
                            fixed (byte* pSrc = convertBuffer)
                            {
                                long copyLen = Math.Min(capBgraSize, fb.RowBytes * capHeight);
                                Buffer.MemoryCopy(pSrc, fb.Address.ToPointer(), fb.RowBytes * capHeight, copyLen);
                            }

                            // 9. 【关键】ARGB 转 BGRA：交换 R 和 B 通道
                            // I420ToARGB 输出的是 A-R-G-B，而 Avalonia Bgra8888 期望 B-G-R-A
                            byte* pixels = (byte*)fb.Address.ToPointer();
                            int pixelCount = capWidth * capHeight;
                            for (int i = 0; i < pixelCount; i++)
                            {
                                byte* pixel = pixels + i * 4;
                                (pixel[0], pixel[2]) = (pixel[2], pixel[0]); // 交换 B 和 R
                            }
                        }

                        // 10. 切换显示引用，触发 UI 刷新
                        PreviewSource = writeBmp;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"渲染帧异常: {ex.Message}");
                    }
                    finally
                    {
                        // 11. 归还缓冲区并重置标志位
                        _bufferPool.Enqueue(convertBuffer);
                        Interlocked.Exchange(ref _isRendering, 0);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解码帧异常: {ex.Message}");
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