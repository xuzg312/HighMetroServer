using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HighMetro.Models;
using HighMetro.ViewModels;

namespace HighMetro.Views.Controls;

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
    // 2. 注册 IsReadOnly 属性
    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<CamConfigView, bool>(nameof(IsReadOnly));

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }
    // 2. 定义内部 ViewModel 属性
    // 这样 XAML 就可以通过 ElementName="Root" 访问到它
    public CamConfigViewModel CamConfigViewModelVm { get; }
    public CamConfigView()
    {
        CamConfigViewModelVm = new CamConfigViewModel(false);
        InitializeComponent();
    }
    static CamConfigView()
    {
        ConfigProperty.Changed.AddClassHandler<CamConfigView, CamOptions>((view, args) =>
        {
            // GetNewValue<T>() 自动拆包BindingValue，拿到真实对象
            var newVal = args.GetNewValue<CamOptions>();
            view.CamConfigViewModelVm.Config = newVal;
        });

        IsReadOnlyProperty.Changed.AddClassHandler<CamConfigView, bool>((view, args) =>
        {
            var newVal = args.GetNewValue<bool>();
            view.CamConfigViewModelVm.IsReadOnly = newVal;
        });
    }

}