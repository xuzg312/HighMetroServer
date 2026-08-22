using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using HighMetroServer.ViewModels;

namespace HighMetroServer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnWindowOpened;
    }
    private void OnWindowOpened(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            WindowState = WindowState.Maximized;
        }, DispatcherPriority.Loaded);
    }
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (DataContext is MainViewModel vm)
        {
            vm.CleanResources();
        }
    }
}