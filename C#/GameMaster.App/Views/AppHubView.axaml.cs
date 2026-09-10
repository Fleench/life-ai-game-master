using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GameMaster.App.Views;

public partial class AppHubView : UserControl
{
    public AppHubView()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
