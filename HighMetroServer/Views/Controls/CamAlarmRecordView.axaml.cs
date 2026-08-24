using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using HighMetroServer.BaseModel;
using HighMetroServer.HikVision;
using HighMetroServer.Models;
using HighMetroServer.ViewModels;

namespace HighMetroServer.Views.Controls;

public partial class CamAlarmRecordView : UserControl
{
    private bool _unregistered;
    private readonly CancellationTokenSource _cts = new();
    public CamAlarmRecordView()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.Register<PreviewImageMessage>(this, (_, msg) =>
        {
            SafeRunPreview(msg.FilePath,msg.FileType);
        });
        Unloaded += OnControlUnloaded;
    }
    private void SafeRunPreview(string filePath,string fileType)
    {
        _ = ShowPreviewAsync(filePath,fileType,_cts.Token);
    }
    private async Task ShowPreviewAsync(string filePath, string fileType,CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            if (fileType.Equals(PublicConst.DoorStateCapture))
            {
                var previewWin = new ImagePreview();
                previewWin.LoadImage(filePath);
                var owner = this.FindAncestorOfType<Window>();
                if (owner is null)
                {
                    if(DataContext is CamAlarmRecordViewModel vm)
                    {
                        vm.MessageText = "DataContext未找到！";
                    }
                    return;
                }
                await previewWin.ShowDialog(owner);
            }
            else if (fileType.Equals(PublicConst.DoorStateCamera))
            {
                var playVideoView = new PlayVideoView();
                Console.WriteLine("playVideoView.LoadVideo(filePath)");
                playVideoView.LoadVideo(filePath);
                var owner = this.FindAncestorOfType<Window>();
                if (owner is null)
                {
                    if(DataContext is PlayVideoViewModel vm)
                    {
                        vm.MessageText = "DataContext未找到！";
                    }
                    return;
                }
                await playVideoView.ShowDialog(owner);
                
                /*var previewWin = new VideoPreview();
                Console.WriteLine("previewWin.LoadVideo(filePath)");

                previewWin.LoadVideo(filePath);
                var owner = this.FindAncestorOfType<Window>();
                if (owner is null)
                {
                    if(DataContext is CamAlarmRecordViewModel vm)
                    {
                        vm.MessageText = "DataContext未找到！";
                    }
                    return;
                }
                await previewWin.ShowDialog(owner);
                */
                /*var resultInfo = OpenVideoBySystemPlayer(filePath);
                if (!resultInfo.Code.Equals(PublicConst.FlagYes))
                {
                    if(DataContext is CamAlarmRecordViewModel vm)
                    {
                        vm.MessageText = $"查看录像文件异常：{resultInfo.Message}";
                    }                    
                }*/
            }
        }
        catch (Exception ex)
        {
            if(DataContext is CamAlarmRecordViewModel vm)
            {
                vm.MessageText = $"查看拍照图片异常：{ex.Message}";
            }
            Console.WriteLine(ex.Message);
        }
    }
    private ResultInfo OpenVideoBySystemPlayer(string videoPath)
    {
        try
        {
            if (HikPlatform.IsWindows)
            {
                // Windows：调用系统Shell，唤起默认播放器
                Process.Start(new ProcessStartInfo
                {
                    FileName = videoPath,
                    UseShellExecute = true
                });
            }
            else if (HikPlatform.IsLinux)
            {
                // 银河麒麟、统信UOS
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{videoPath}\"",
                    UseShellExecute = false
                });
            }
            else if (HikPlatform.IsMac)
            {
                // MacOS（可选，如果你需要）
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"\"{videoPath}\"",
                    UseShellExecute = false
                });
            }
            return new ResultInfo
            {
                Code = PublicConst.FlagYes,
            };
        }
        catch (Exception ex)
        {
            return new ResultInfo
            {
                Code = PublicConst.FlagYes,
                Message = ex.Message,
            };
        }
    }
    private void OnControlUnloaded(object? sender, EventArgs e)
    {
        Console.WriteLine("释放CamAlarmRecordView！");
        if (_unregistered) 
            return;
        try
        {
            _cts.Cancel();
        }
        catch (Exception)
        {
            //忽略；
        }
        try
        {
            _cts.Dispose();
        }
        catch (Exception)
        {
            //忽略；
        }
        WeakReferenceMessenger.Default.UnregisterAll(this);
        Unloaded -= OnControlUnloaded;
        _unregistered = true;
    }
}