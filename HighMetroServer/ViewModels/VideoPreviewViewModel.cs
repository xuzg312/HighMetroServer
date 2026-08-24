using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
//using LibVLCSharp.Avalonia;
//using LibVLCSharp.Shared;

namespace HighMetroServer.ViewModels;

public partial class VideoPreviewViewModel : ObservableObject
{
    /*private readonly LibVLC _libVcl;
    private readonly MediaPlayer _mediaPlayer;
    private string? _filePath;
    
    [ObservableProperty]
    private string _messageText = string.Empty;

    public VideoPreviewViewModel(LibVLC libVcl, MediaPlayer mediaPlayer)
    {
        Core.Initialize(); 
        _libVcl = libVcl;
        _mediaPlayer = mediaPlayer;
    }
    public void LoadVideo(string filePath)
    {
        if (!File.Exists(filePath))
        {
            MessageText = "视频文件不存在！";
            return;
        }
        Console.WriteLine("--------Core.Initialize()---6----");

        _filePath = filePath;
        MessageText = string.Empty; 
    }
    [RelayCommand]
    private void Play()
    {
        if (string.IsNullOrEmpty(_filePath))
        {
            MessageText = "请先加载视频文件";
            return;
        }
        if (_mediaPlayer.State == VLCState.Paused)
        {
            _mediaPlayer.SetPause(false);
        }
        else
        {
            var media = new Media(_libVcl, _filePath, FromType.FromLocation);
            _mediaPlayer.Play(media);
        }
    }*/
}