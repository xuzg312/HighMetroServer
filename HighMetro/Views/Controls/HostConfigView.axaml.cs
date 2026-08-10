using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using HighMetro.Models;
using HighMetro.ViewModels;

namespace HighMetro.Views.Controls;

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
    // 2. 注册 IsReadOnly 属性
    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<HostConfigView, bool>(nameof(IsReadOnly));

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }
    // 2. 定义内部 ViewModel 属性
    // 这样 XAML 就可以通过 ElementName="Root" 访问到它
    public HostConfigViewModel HostConfigViewModelVm { get; }
    public HostConfigView()
    {
        HostConfigViewModelVm = new HostConfigViewModel();
        InitializeComponent();
        Unloaded += OnViewUnloaded;
    }
    private void OnViewUnloaded(object? sender, RoutedEventArgs e)
    {
        if(DataContext is HostConfigViewModel vm)
        {
            vm.Unsubscribe();
        }
        Unloaded -= OnViewUnloaded;
    }
    static HostConfigView()
    {
        ConfigProperty.Changed.AddClassHandler<HostConfigView, HostOptions>((view, args) =>
        {
            var newVal = args.GetNewValue<HostOptions>();
            view.HostConfigViewModelVm.Config = newVal;
        });

        IsReadOnlyProperty.Changed.AddClassHandler<HostConfigView, bool>((view, args) =>
        {
            var newVal = args.GetNewValue<bool>();
            view.HostConfigViewModelVm.IsReadOnly = newVal;
        });
    }
}