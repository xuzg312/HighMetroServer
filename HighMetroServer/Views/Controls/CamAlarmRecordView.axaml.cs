using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using HighMetroServer.BaseModel;
using HighMetroServer.Message;
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
                    WeakReferenceMessenger.Default.Send(new ClosePlayImageMessage());
                    return;
                }
                await previewWin.ShowDialog(owner);
            }
            else if (fileType.Equals(PublicConst.DoorStateCamera))
            {
                var playVideoView = new PlayVideoView();
                playVideoView.LoadVideo(filePath);
                var owner = this.FindAncestorOfType<Window>();
                if (owner is null)
                {
                    if(DataContext is PlayVideoViewModel vm)
                    {
                        vm.MessageText = "DataContext未找到！";
                    }
                    WeakReferenceMessenger.Default.Send(new ClosePlayVideoViewMessage());
                    return;
                }
                await playVideoView.ShowDialog(owner);
            }
        }
        catch (Exception ex)
        {
            if(DataContext is CamAlarmRecordViewModel vm)
            {
                vm.MessageText = $"查看拍照图片异常：{ex.Message}";
            }
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