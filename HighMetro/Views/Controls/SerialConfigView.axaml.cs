using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using HighMetro.Models;
using HighMetro.ViewModels;
namespace HighMetro.Views.Controls;

public partial class SerialConfigView : UserControl
{
    public static readonly StyledProperty<int> SerialNoProperty =
        AvaloniaProperty.Register<SerialConfigView, int>(nameof(SerialNo), defaultValue: 1);
    public int SerialNo
    {
        get => GetValue(SerialNoProperty);
        set => SetValue(SerialNoProperty, value);
    }
    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<SerialConfigView, bool>(nameof(IsReadOnly), defaultValue: false);
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
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
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Console.WriteLine("⚠️ 严重警告：SerialConfigView 正在后台线程被创建！");
        }

        InitializeComponent();
        _viewModel = new SerialConfigViewModel(false,0);
        Console.WriteLine("SerialConfigView:------"+SerialNo+"-----"+IsReadOnly);
        Dispatcher.UIThread.Post(() => { DataContext = _viewModel; });
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
        if (prop != IsReadOnlyProperty
            && prop != SerialNoProperty
            && prop != ConfigProperty)
        {
            return;
        }
        _viewModel.UpdateParams(IsReadOnly, SerialNo, Config);
        Console.WriteLine($"SerialConfigView 更新：No={SerialNo} ReadOnly={IsReadOnly}");
        if (prop == ConfigProperty)
        {
            var newCfg = change.NewValue as SerialPortOptions;
            Console.WriteLine($"【外部传入Config更新】端口：{newCfg?.PortName} 波特率：{newCfg?.BaudRate}");        }
    }
}