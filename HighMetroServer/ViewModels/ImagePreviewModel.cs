using System;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HighMetroServer.Message;

namespace HighMetroServer.ViewModels;

public partial class ImagePreviewModel : ObservableObject
{
    [ObservableProperty]
    private string _messageText=string.Empty;
    
    [RelayCommand]
    private void Quit()
    {
        WeakReferenceMessenger.Default.Send(new ClosePlayImageMessage());
    }
    public void ReleaseResource()
    {
        Console.WriteLine("释放ImagePreviewModel->ReleaseResource！");
    }
}