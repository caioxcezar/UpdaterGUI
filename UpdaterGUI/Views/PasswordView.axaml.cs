using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace UpdaterGUI.Views;

public partial class PasswordView : UserControl
{
    public PasswordView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}