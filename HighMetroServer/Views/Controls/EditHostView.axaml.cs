using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HighMetroServer.ViewModels;

namespace HighMetroServer.Views.Controls;

public partial class EditHostView : UserControl
{
    public EditHostView()
    {
        InitializeComponent();
    }
    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        // 获取ViewModel
        if (DataContext is EditHostViewModel vm)
        {
            // 清空错误文本
            vm.MessageText = string.Empty;
        }
    }
}