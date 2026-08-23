using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using HighMetroServer.Models;
using HighMetroServer.ViewModels;

namespace HighMetroServer.Views.Controls;

public partial class HostConfigView : UserControl
{
    // 1. 定义依赖属性 (StyledProperty)
    public static readonly StyledProperty<HostOptions> ConfigProperty =
        AvaloniaProperty.Register<HostConfigView, HostOptions>(nameof(Config));

    // 2. 包装属性
    public HostOptions Config
    {
        get => GetValue(ConfigProperty);
        set => SetValue(ConfigProperty, value);
    }

    public HostConfigViewModel HostConfigViewModelVm { get; }
    public HostConfigView()
    {
        HostConfigViewModelVm = new HostConfigViewModel();
        InitializeComponent();
        Loaded += OnLoaded;
    }
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            _ = HostConfigViewModelVm.Start();
        }
        catch (Exception)
        {
            // 捕获初始化异常，弹窗/日志
        }
    }
    static HostConfigView()
    {
        ConfigProperty.Changed.AddClassHandler<HostConfigView, HostOptions>((view, args) =>
        {
            var newVal = args.GetNewValue<HostOptions>();
            view.HostConfigViewModelVm.Config = newVal;
        });
    }
}