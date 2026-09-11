using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GameMaster.App.Views;

public partial class ProfileView : UserControl
{
    public ProfileView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
