using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using HighMetro.Models;
using HighMetro.ViewModels;

namespace HighMetro.Views.Controls;

public partial class EditCamConfigView : UserControl
{
    public EditCamConfigView()
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
        if (DataContext is EditCamConfigViewModel vm)
        {
            // 清空错误文本
            vm.MessageText = string.Empty;
        }
    }
}