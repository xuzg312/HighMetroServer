using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using HighMetro.Models;
using HighMetro.ViewModels;

namespace HighMetro.Views.Controls;

public partial class CamAlarmRecordView : UserControl
{
    public CamAlarmRecordView()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.Register<PreviewImageMessage>(this, (_, msg) =>
        {
            // 显式丢弃任务，并且内部try-catch，防止崩溃
            _ = ShowPreviewAsync(msg.FilePath);
        });
    }
    private async Task ShowPreviewAsync(string filePath)
    {
        try
        {
            var previewWin = new ImagePreview();
            previewWin.LoadImage(filePath);
            var owner = this.FindAncestorOfType<Window>();
            await previewWin.ShowDialog(owner);
        }
        catch (Exception ex)
        {
            // 日志兜底，不会炸进程
            System.Diagnostics.Debug.WriteLine($"图片预览异常：{ex.Message}");
        }
    }
}