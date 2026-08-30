using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using HighMetroServer.Models;
using HighMetroServer.ViewModels;

namespace HighMetroServer.Views.Controls;

public partial class CamConfigView : UserControl
{
    // 1. 定义依赖属性 (StyledProperty)
    public static readonly StyledProperty<CamOptions> ConfigProperty =
        AvaloniaProperty.Register<CamConfigView, CamOptions>(nameof(Config));

    // 2. 包装属性
    public CamOptions Config
    {
        get => GetValue(ConfigProperty);
        set => SetValue(ConfigProperty, value);
    }

    // 2. 定义内部 ViewModel 属性
    // 这样 XAML 就可以通过 ElementName="Root" 访问到它
    public CamConfigViewModel CamConfigViewModelVm { get; }
    public CamConfigView()
    {
        CamConfigViewModelVm = new CamConfigViewModel();
        InitializeComponent();
        Loaded += OnLoaded;
    }
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            _ = CamConfigViewModelVm.Start();
        }
        catch (Exception)
        {
            // 捕获初始化异常，弹窗/日志
        }
    }
    static CamConfigView()
    {
        ConfigProperty.Changed.AddClassHandler<CamConfigView, CamOptions>((view, args) =>
        {
            var newVal = args.GetNewValue<CamOptions>();
            view.CamConfigViewModelVm.Config = newVal;
        });
    }
}