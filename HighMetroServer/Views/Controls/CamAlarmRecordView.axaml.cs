using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using HighMetroServer.Models;
using HighMetroServer.ViewModels;

namespace HighMetroServer.Views.Controls;

public partial class CamAlarmRecordView : UserControl
{
    public CamAlarmRecordView()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.Register<PreviewImageMessage>(this, (_, msg) =>
        {
            SafeRunPreview(msg.FilePath);
        });
    }
    private void SafeRunPreview(string filePath)
    {
        _ = ShowPreviewAsync(filePath);
    }
    private async Task ShowPreviewAsync(string filePath)
    {
        try
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
        catch (Exception ex)
        {
            if(DataContext is CamAlarmRecordViewModel vm)
            {
                vm.MessageText = $"查看拍照图片异常：{ex.Message}";
            }
        }
    }
}