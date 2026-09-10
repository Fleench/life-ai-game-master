using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using GameMaster.App.ViewModels;

namespace GameMaster.App.Views;

public partial class ReplView : UserControl
{
    private ScrollViewer? _scrollViewer;

    public ReplView()
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
        if (DataContext is ReplViewModel vm)
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
