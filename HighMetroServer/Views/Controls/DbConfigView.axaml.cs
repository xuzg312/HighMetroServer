using Avalonia.Controls;
using HighMetroServer.ViewModels;

namespace HighMetroServer.Views.Controls;

public partial class DbConfigView : UserControl
{
    public DbConfigView()
    {
        InitializeComponent();
    }
    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        // 获取ViewModel
        if (DataContext is DbConfigViewModel vm)
        {
            // 清空错误文本
            vm.MessageText = string.Empty;
        }
    }
}