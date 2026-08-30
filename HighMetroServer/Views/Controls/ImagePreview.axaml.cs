using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Messaging;
using HighMetroServer.Message;
using HighMetroServer.ViewModels;

namespace HighMetroServer.Views.Controls;

public partial class ImagePreview : Window,IRecipient<ClosePlayImageMessage>
{
    private readonly ImagePreviewModel _imagePreviewModel;

    public ImagePreview()
    {
        InitializeComponent();
        _imagePreviewModel = new ImagePreviewModel();
        DataContext = _imagePreviewModel;
        WeakReferenceMessenger.Default.Register(this);
    }
    public void LoadImage(string filePath)
    {
        var bitmap = new Bitmap(filePath);
        ImgViewer.Source = bitmap;
        Title = filePath;
    }
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _imagePreviewModel.ReleaseResource();
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
    public void Receive(ClosePlayImageMessage message)
    {
        Close();
    }
}