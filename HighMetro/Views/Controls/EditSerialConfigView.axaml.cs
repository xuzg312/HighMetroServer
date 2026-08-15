using Avalonia.Controls;
using HighMetro.ViewModels;

namespace HighMetro.Views.Controls;

public partial class EditSerialConfigView : UserControl
{
    public EditSerialConfigView()
    {
        InitializeComponent();
    }
    private void SelectionChanged1(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is EditSerialConfigViewModel vm)
        {
            // 清空错误文本
            vm.MessageText1 = string.Empty;
        }
    }
    private void SelectionChanged2(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is EditSerialConfigViewModel vm)
        {
            // 清空错误文本
            vm.MessageText2 = string.Empty;
        }
    }
    private void OnTextChanged1(object? sender, TextChangedEventArgs e)
    {
        // 获取ViewModel
        if (DataContext is EditSerialConfigViewModel vm)
        {
            // 清空错误文本
            vm.MessageText1 = string.Empty;
        }
    }
    private void OnTextChanged2(object? sender, TextChangedEventArgs e)
    {
        // 获取ViewModel
        if (DataContext is EditSerialConfigViewModel vm)
        {
            // 清空错误文本
            vm.MessageText2 = string.Empty;
        }
    }
}