using System;
using System.IO;
using Avalonia.Controls;
using HighMetroServer.ViewModels;
using LibVLCSharp.Shared;

namespace HighMetroServer.Views.Controls;

public partial class VideoPreview : Window
{
    private readonly LibVLC _libVcl;
    private readonly MediaPlayer _mediaPlayer;
    private readonly VideoPreviewViewModel _viewModel;
    public VideoPreview()
    {
        InitializeComponent();
        // 初始化 LibVLC
        Core.Initialize();
        Console.WriteLine("--------Core.Initialize()--1----");
        _libVcl = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVcl);
        Console.WriteLine("--------Core.Initialize()---2----");

        // 绑定到 VideoView
        VideoView.MediaPlayer = _mediaPlayer;

        // 创建 ViewModel 并设置为 DataContext
        _viewModel = new VideoPreviewViewModel(_libVcl, _mediaPlayer);
        DataContext = _viewModel;
        Console.WriteLine("--------Core.Initialize()----3---");

    }
    public void LoadVideo(string filePath)
    {
        _viewModel.LoadVideo(filePath);
        Console.WriteLine("--------Core.Initialize()---4----");

        Title = Path.GetFileName(filePath);
        Console.WriteLine("--------Core.Initialize()---5----");

    }
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _mediaPlayer.Stop();
        _mediaPlayer.Dispose();
        _libVcl.Dispose();
    }
}