using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace HighMetroServer.Views.Controls;

public partial class LoadParaView : UserControl
{
    public LoadParaView()
    {
        InitializeComponent();
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}