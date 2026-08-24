using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HighMetroServer.ViewModels;

namespace HighMetroServer.Views.Controls;

public partial class PlayVideoView : Window
{
    private readonly PlayVideoViewModel _playViewModel;
    public PlayVideoView()
    {
        InitializeComponent();
        _playViewModel = new PlayVideoViewModel();
        DataContext = _playViewModel;
    }
    public void LoadVideo(string filePath)
    {
        _playViewModel.LoadVideo(filePath);
        Console.WriteLine("--------Core.Initialize()---4----");

        Title = Path.GetFileName(filePath);
        Console.WriteLine("--------Core.Initialize()---5----");
    }
}