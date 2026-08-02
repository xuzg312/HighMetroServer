using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HighMetro.ViewModels;

namespace HighMetro.Views.Controls;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        // 获取ViewModel
        if (DataContext is LoginViewModel vm)
        {
            // 清空错误文本
            vm.Msg = string.Empty;
        }
    }
}