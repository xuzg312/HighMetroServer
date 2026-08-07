using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using HighMetro.Models;
using HighMetro.ViewModels;

namespace HighMetro.Views.Controls;

public partial class SerialConfigView : UserControl
{
    // 1. 定义依赖属性 (StyledProperty)
    public static readonly StyledProperty<SerialPortOptions> ConfigProperty =
        AvaloniaProperty.Register<SerialConfigView, SerialPortOptions>(nameof(Config));

    // 2. 包装属性
    public SerialPortOptions Config
    {
        get => GetValue(ConfigProperty);
        set => SetValue(ConfigProperty, value);
    }
    // 2. 注册 IsReadOnly 属性
    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<SerialConfigView, bool>(nameof(IsReadOnly));

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }
    // 2. 定义内部 ViewModel 属性
    // 这样 XAML 就可以通过 ElementName="Root" 访问到它
    public SerialConfigViewModel SerialConfigViewModelVm { get; }
    public SerialConfigView()
    {
        SerialConfigViewModelVm = new SerialConfigViewModel(true);
        InitializeComponent();
    }
    static SerialConfigView()
    {
        ConfigProperty.Changed.AddClassHandler<SerialConfigView, SerialPortOptions>((view, args) =>
        {
            var newVal = args.GetNewValue<SerialPortOptions>();
            view.SerialConfigViewModelVm.Config = newVal;
        });

        IsReadOnlyProperty.Changed.AddClassHandler<SerialConfigView, bool>((view, args) =>
        {
            var newVal = args.GetNewValue<bool>();
            view.SerialConfigViewModelVm.IsReadOnly = newVal;
        });
    }
}