using System;
using System.Threading.Tasks;
using ReactiveUI;

namespace UpdaterGUI.ViewModels;

public class TerminalViewModel : ViewModelBase
{
    private string _terminalText = "";
    private string _title = "";
    private bool _backEnabled;

    public TerminalViewModel(MainWindowViewModel main, string title, Action<Action<string?>> action) : base(main)
    {
        Title = title;
        Task.Run(() => action.Invoke(str => TerminalText = TerminalText.Insert(0, str))).ContinueWith(_ => BackEnabled = true);
    }

    public string TerminalText
    {
        get => _terminalText;
        set => this.RaiseAndSetIfChanged(ref _terminalText, value);
    }
    public bool BackEnabled
    {
        get => _backEnabled;
        set => this.RaiseAndSetIfChanged(ref _backEnabled, value);
    }
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }
    public void Back() => GoBack();
}