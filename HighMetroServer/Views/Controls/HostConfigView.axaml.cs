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
    }
}