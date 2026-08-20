using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using HighMetroServer.ViewModels;

namespace HighMetroServer.Views.Controls;

public partial class MainPageView : UserControl
{
    public MainPageView()
    {
        InitializeComponent();
        Loaded += MainPageView_Loaded;
    }
    private void MainPageView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainPageViewModel vm)
        {
            vm.InitSerialPorts();
        }
    }
}