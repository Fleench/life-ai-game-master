using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using GameMaster.ReplApp.ViewModels;

namespace GameMaster.ReplApp.Views;

public partial class MainView : UserControl
{
    private ScrollViewer? _scrollViewer;

    public MainView()
    {
        InitializeComponent();
        _scrollViewer = this.FindControl<ScrollViewer>("TerminalScrollViewer");
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void OnTerminalTapped(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        var inputTextBox = this.FindControl<TextBox>("InputTextBox");
        inputTextBox?.Focus();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainViewModel vm)
        {
            vm.Messages.CollectionChanged += (s, args) => 
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    Dispatcher.UIThread.Post(() => _scrollViewer?.ScrollToEnd());
                }
            };
        }
    }
}
