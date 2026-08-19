using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HighMetroServer.ViewModels;

namespace HighMetroServer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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