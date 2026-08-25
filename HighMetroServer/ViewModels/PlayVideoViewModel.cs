using System;
using System.IO;
using System.Threading;
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
    private readonly PlayCtrl.DeccbFun _decodeCallBack;
    private readonly PlayCtrl.FileEndCallBack _fileEndCallBack;
    private readonly CamRemoteLinkImpl _camRemoteLinkImpl;
    private bool _isValid;
    private bool _isRunIng;
    private bool _isPause;
    private int _isRendering;
    private volatile bool _isClosed;
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
        var loadCamResult = !_isRunIng ? _camRemoteLinkImpl.PlayPlayMp4() : _camRemoteLinkImpl.PlayPauseMp4(0);
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
        WeakReferenceMessenger.Default.Send(new ClosePlayVideoViewMessage());
    }
    private void OnDecodedFrameCallBack(
        int nPort, IntPtr pBuf, int nSize, ref PlayCtrl.FrameInfo frameInfo, IntPtr pUser)
    {
        Console.WriteLine(
            $"OnDecodedFrameCallBack:nPort:{nPort},nSize:{nSize},frameInfo.NWidth:{frameInfo.NWidth},frameInfo.NHeight:{frameInfo.NHeight},frameInfo.NType:{frameInfo.NType}");
        if (_isClosed) return;

        var width = frameInfo.NWidth;
        var height = frameInfo.NHeight;

        // 仅处理视频帧（海康 SDK 中 3 代表 YV12 视频数据）
        if (frameInfo.NType != 3 || width <= 0 || height <= 0 || pBuf == IntPtr.Zero || nSize <= 0)
            return;

        // 丢帧保护：如果上一帧还没渲染完，直接丢弃当前帧
        if (Interlocked.CompareExchange(ref _isRendering, 1, 0) != 0)
            return;

        try
        {
            int yPlaneSize = width * height;
            int uvPlaneSize = (width / 2) * (height / 2);

            unsafe
            {
                byte* src = (byte*)pBuf.ToPointer();

                // 严格按照海康 YV12 的紧凑内存布局提取指针 (Y + V + U)
                byte* yPtr = src;
                byte* vPtr = src + yPlaneSize; // V 平面紧跟 Y 平面
                byte* uPtr = vPtr + uvPlaneSize; // U 平面紧跟 V 平面

                // 捕获局部变量，避免在 lambda 中访问栈变量
                var capWidth = width;
                var capHeight = height;

                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        if (_isClosed) return;

                        // 尺寸变更，重建双缓冲
                        if (_wbA == null || _wbA.PixelSize.Width != capWidth
                                         || _wbA.PixelSize.Height != capHeight)
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
                        // 选择后台未显示的 bitmap 进行写入
                        var writeBmp = PreviewSource == _wbA ? _wbB! : _wbA!;
                        using (var fb = writeBmp.Lock())
                        {
                            int actualStride = fb.RowBytes;
                            byte* dst = (byte*)fb.Address.ToPointer();
                            var value = LibYuv.I420ToBGRA(
                                (IntPtr)yPtr, capWidth, // Y 数据, Y 步长
                                (IntPtr)vPtr, capWidth / 2, // V 数据, V 步长
                                (IntPtr)uPtr, capWidth / 2, // U 数据, U 步长
                                (IntPtr)dst, actualStride, // 【关键】使用实际的 RowBytes 作为目标步长
                                capWidth, capHeight);
                            if (value < 0)
                            {
                                MessageText = $"解码失败，返回值：{value}";
                                return;
                            }
                        }
                        PreviewSource = writeBmp;
                    }
                    catch (Exception ex)
                    {
                        MessageText = $"渲染帧异常: {ex.Message}";
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Interlocked.Exchange(ref _isRendering, 0);
            Dispatcher.UIThread.Post(() => { MessageText = $"解码帧异常: {ex.Message}";});
        }
        finally
        {
            Interlocked.Exchange(ref _isRendering, 0);
        }
    }
    private void OnFileEndCallBack(int nPort, IntPtr pUser)
    {
        var loadCamResult = _camRemoteLinkImpl.SetPlayPos(0);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = loadCamResult.Message;
            return;
        }
        _isRunIng = false;
        _isPause = false;
        NotifyCanExecuteChanged();
    }
    private bool CanPlay()
    {
        return _isValid && ((_isRunIng && _isPause) || (!_isRunIng)); 
    }
    private bool CanStop()
    {
        return _isValid && _isRunIng && !_isPause; 
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
        _camRemoteLinkImpl.Close();
        _isClosed = true;
        _wbA?.Dispose();
        _wbB?.Dispose();
        _wbA = null;
        _wbB = null;
        PreviewSource = null;
    }
}