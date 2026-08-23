using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using HighMetroServer.Models;
using HighMetroServer.ViewModels;

namespace HighMetroServer.Views.Controls;

public partial class SerialConfigView : UserControl
{
    public static readonly StyledProperty<int> SerialNoProperty =
        AvaloniaProperty.Register<SerialConfigView, int>(nameof(SerialNo), defaultValue: 1);
    public int SerialNo
    {
        get => GetValue(SerialNoProperty);
        set => SetValue(SerialNoProperty, value);
    }
    public static readonly StyledProperty<SerialPortOptions> ConfigProperty =
        AvaloniaProperty.Register<SerialConfigView, SerialPortOptions>(nameof(Config));

    public SerialPortOptions Config
    {
        get => GetValue(ConfigProperty);
        set => SetValue(ConfigProperty, value);
    }
    private readonly SerialConfigViewModel? _viewModel;
    public SerialConfigView()
    {
        InitializeComponent();
        _viewModel = new SerialConfigViewModel(0);
        Dispatcher.UIThread.Post(() => { DataContext = _viewModel; });
        Loaded += OnLoaded;
    }
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if(_viewModel==null)
            return;
        try
        {
            _ = _viewModel.Start();
        }
        catch (Exception)
        {
            // 捕获初始化异常，弹窗/日志
        }
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_viewModel == null)
            return;
        if (Equals(change.OldValue, change.NewValue))
            return;
        // 只监听三个外部传入的依赖属性
        var prop = change.Property;
        if (prop != SerialNoProperty
            && prop != ConfigProperty)
        {
            return;
        }
        _viewModel.UpdateParams(SerialNo, Config);
    }
}