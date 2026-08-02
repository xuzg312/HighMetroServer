using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HighMetro.ViewModels;

namespace HighMetro.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        // 获取ViewModel
        if (DataContext is MainViewModel vm)
        {
            vm.CleanResources();
        }
    }
}