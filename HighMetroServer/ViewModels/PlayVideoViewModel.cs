using System;
using System.IO;
using System.Runtime.InteropServices;
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
    private PlayBackState _playState;
    private int _isRendering;
    private volatile bool _isClosed;
    private WriteableBitmap? _wbA;
    private WriteableBitmap? _wbB;
    private byte[]? _yuvBuffer;
    
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
    private void OnDecodedFrameCallBack(
        int nPort, IntPtr pBuf, int nSize, ref PlayCtrl.FrameInfo frameInfo, IntPtr pUser)
    {
        if (_isClosed) return;
        var width = frameInfo.NWidth;
        var height = frameInfo.NHeight;
        if (frameInfo.NType != 3 || width <= 0 || height <= 0 || pBuf == IntPtr.Zero || nSize <= 0)
            return;
        int ySize = width * height; // 2073600
        int uvSize = (width / 2) * (height / 2); // 518400
        if (nSize < ySize + uvSize * 2) return;

        if (_yuvBuffer == null || _yuvBuffer.Length < nSize)
            _yuvBuffer = new byte[nSize];
        Marshal.Copy(pBuf, _yuvBuffer, 0, nSize);

        var yuv = _yuvBuffer;
        var w = width;
        var h = height;

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (_isClosed) return;
                // 丢帧保护
                if (Interlocked.CompareExchange(ref _isRendering, 1, 0) != 0)
                    return;
                if (_wbA == null || _wbA.PixelSize.Width != w || _wbA.PixelSize.Height != h)
                {
                    _wbA?.Dispose();
                    _wbB?.Dispose();
                    _wbA = new WriteableBitmap(new PixelSize(w, h), new Vector(96, 96), PixelFormat.Bgra8888);
                    _wbB = new WriteableBitmap(new PixelSize(w, h), new Vector(96, 96), PixelFormat.Bgra8888);
                    PreviewSource = _wbA;
                }

                var writeBmp = ReferenceEquals(PreviewSource, _wbA) ? _wbB! : _wbA!;

                unsafe
                {
                    fixed (byte* src = yuv)
                    {
                        // 海康 YV12：平面2=V(@ySize)，平面3=U(@ySize+uvSize)
                        byte* yPtr = src;
                        byte* vPtr = src + ySize;
                        byte* uPtr = src + ySize + uvSize;

                        using (var fb = writeBmp.Lock())
                        {
                            // 【修复2】改用 I420ToARGB：输出内存序 B,G,R,A = Bgra8888
                            // 【修复3】U 平面进 U 槽、V 平面进 V 槽
                            int ret = LibYuv.I420ToARGB(
                                (IntPtr)yPtr, w,
                                (IntPtr)uPtr, w / 2,
                                (IntPtr)vPtr, w / 2,
                                (IntPtr)fb.Address, fb.RowBytes,
                                w, h);
                            if (ret < 0)
                            {
                                MessageText = $"转换失败：{ret}";
                                return;
                            }
                        }
                    }
                }

                PreviewSource = writeBmp;
            }
            catch (Exception ex)
            {
                MessageText = $"渲染帧异常: {ex.Message}";
            }
            finally
            {
                Interlocked.Exchange(ref _isRendering, 0);
            }
        });
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
        _camRemoteLinkImpl.Close();
        _isClosed = true;
        _wbA?.Dispose();
        _wbB?.Dispose();
        _wbA = null;
        _wbB = null;
        PreviewSource = null;
    }
}