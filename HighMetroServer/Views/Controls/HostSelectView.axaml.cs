using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace HighMetroServer.Views.Controls;

public partial class HostSelectView : UserControl
{
    public HostSelectView()
    {
        InitializeComponent();
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}