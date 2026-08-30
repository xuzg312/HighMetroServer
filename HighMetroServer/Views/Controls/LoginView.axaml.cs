using Avalonia.Controls;
using HighMetroServer.ViewModels;

namespace HighMetroServer.Views.Controls;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
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