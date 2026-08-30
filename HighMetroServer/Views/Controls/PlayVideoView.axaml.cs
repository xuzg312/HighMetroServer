using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using HighMetroServer.Message;
using HighMetroServer.ViewModels;

namespace HighMetroServer.Views.Controls;

public partial class PlayVideoView : Window,IRecipient<ClosePlayVideoViewMessage>
{
    private readonly PlayVideoViewModel _playViewModel;
    public PlayVideoView()
    {
        InitializeComponent();
        _playViewModel = new PlayVideoViewModel();
        DataContext = _playViewModel;
        WeakReferenceMessenger.Default.Register(this);
    }
    public void LoadVideo(string filePath)
    {
        _playViewModel.LoadVideo(filePath);
        Title = filePath;
    }
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _playViewModel.ReleaseResource();
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
    public void Receive(ClosePlayVideoViewMessage message)
    {
        Close();
    }
}