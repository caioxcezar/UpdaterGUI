using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Security;
using System.Threading.Tasks;
using ArchUpdateGUI.Models;
using DynamicData;
using MessageBox.Avalonia;
using ReactiveUI;

namespace ArchUpdateGUI.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ObservableCollection<Package> Packages { get; }
    private string _searchParam;
    private IProvider _provider;
    private List<IProvider> _providers;
    private string _info = "Select a provider. ";
    private string _packageInfo;
    private Package _selectedPackage;
    private string _commandText;
    private bool _commandVisibility;
    public MainViewModel(MainWindowViewModel model) : base(model)
    {
        try
        {
            ShowPassword = new();
            Packages = new();
            Providers = new Providers().List;
            this.WhenAnyValue(props => props.Provider).Subscribe(provider => ChangedProvider(provider));
            this.WhenAnyValue(props => props.SelectedPackage).Subscribe(package => ChangedPackage(package));
            this.WhenAnyValue(props => props.SearchParam).Where(param => !string.IsNullOrWhiteSpace(param))
                .Throttle(TimeSpan.FromMilliseconds(400)).ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => Search());

            IObservable<bool> canExecute = this.WhenAnyValue(props => props.CommandText, action => !string.IsNullOrWhiteSpace(action));

            CommandAction = ReactiveCommand.CreateFromTask(async () =>
            {
                var pass = Provider.RootRequired ? await ShowPassword.Handle(new PasswordViewModel()) : null;
                switch (CommandText)
                {
                    case "Install":
                        ShowTerminal(action =>
                        {
                            var exitCode = Provider.Install(pass, SelectedPackage,
                                action.Invoke,
                                action.Invoke).Result;
                            action.Invoke(Command.ExitCodeName(exitCode));
                            Reload();
                        });
                        break;
                    case "Remove":
                        ShowTerminal(action =>
                        {
                            var exitCode = Provider.Remove(pass, SelectedPackage,
                                action.Invoke,
                                action.Invoke).Result;
                            action.Invoke(Command.ExitCodeName(exitCode));
                            Reload();
                        });
                        break;
                    default:
                        await MessageBoxManager.GetMessageBoxStandardWindow("A error has occurred", "Invalid Option. ").Show();
                        break;
                }
            }, canExecute);
        }
        catch (Exception e)
        {
            var msgBox = MessageBoxManager.GetMessageBoxStandardWindow("A error has occurred", e.Message);
            msgBox.Show();
        }
    }

    private void Reload()
    {
        Provider.Load();
        ChangedProvider(Provider);
    }

    private void ShowTerminal(Action<Action<string?>> action) => Navigate(typeof(TerminalViewModel), action);

    private void ChangedProvider(IProvider? provider)
    {
        try
        {
            if (provider == null) return;
            Packages.Clear();
            Packages.AddRange(provider.Packages);
            Info = $"packages {provider.Installed} installed of {provider.Total}";
        }
        catch (Exception e)
        {
            var msgBox = MessageBoxManager.GetMessageBoxStandardWindow("A error has occurred", e.Message);
            msgBox.Show();
        }
    }

    public async void OpenConfig()
    {
        try
        {
            Navigate(typeof(ConfigViewModel));
        }
        catch (Exception e)
        {
            await MessageBoxManager.GetMessageBoxStandardWindow("A error has occurred", e.Message).Show();
        }
    }

    public void Search()
    {
        var filteredList = Provider.Packages.Where(package =>
            package.Name != null && package.Name.ToLower().Contains(SearchParam.ToLower()));
        Packages.Clear();
        Packages.AddRange(filteredList);
    }
    
    public void ChangedPackage(Package? package)
    {
        try
        {
            if (package == null)
            {
                CommandVisibility = false;
                return;
            }
            CommandVisibility = true;
            PackageInfo = Provider.PackageInfo(package);
            CommandText = package.IsInstalled ? "Remove" : "Install";
        }
        catch (Exception e)
        {
            var msgBox = MessageBoxManager
                .GetMessageBoxStandardWindow("A error has occurred",
                    $"Was not possible to archive package information.\n{e.Message}");
            msgBox.Show();
        }
    }

    public async void SystemUpdate()
    {
        try
        {
            var pass = Provider.RootRequired ? await ShowPassword.Handle(new PasswordViewModel()) : null;
            if (pass == null && Provider.RootRequired)
            {
                await MessageBoxManager
                    .GetMessageBoxStandardWindow("Wrong password",
                        $"Please provide the correct password. ")
                    .Show();
                return;
            }
            ShowTerminal(action =>
            {
                var exitCode = Provider.Update(pass, 
                    output => action.Invoke(output), 
                    error => action.Invoke(error)).Result;
               action.Invoke(Command.ExitCodeName(exitCode));
               Reload();
            });
        }
        catch (Exception e)
        {
            await MessageBoxManager
                .GetMessageBoxStandardWindow("A error has occurred",
                    $"Was not possible to make update.\n{e.Message}")
                .Show();
        }
    }

    // public async Task<SecureString?> AskPassword() => Provider.RootRequired ? await ShowPassword.Handle(new PasswordViewModel()) : null;
        
    public IProvider Provider
    {
        get => _provider;
        private set => this.RaiseAndSetIfChanged(ref _provider, value);
    }
    public List<IProvider> Providers
    {
        get => _providers;
        private set => this.RaiseAndSetIfChanged(ref _providers, value);
    }

    public string Info
    {
        get => _info;
        private set => this.RaiseAndSetIfChanged(ref _info, value);
    }

    public string SearchParam
    {
        get => _searchParam;
        private set => this.RaiseAndSetIfChanged(ref _searchParam, value);
    }

    public string PackageInfo
    {
        get => _packageInfo;
        private set => this.RaiseAndSetIfChanged(ref _packageInfo, value);
    }

    public Package SelectedPackage
    {
        get => _selectedPackage;
        private set => this.RaiseAndSetIfChanged(ref _selectedPackage, value);
    }
    
    public string CommandText
    {
        get => _commandText;
        private set => this.RaiseAndSetIfChanged(ref _commandText, value);
    }
    
    public bool CommandVisibility
    {
        get => _commandVisibility;
        private set => this.RaiseAndSetIfChanged(ref _commandVisibility, value);
    }

    public ReactiveCommand<Unit, Unit> CommandAction { get; }
    public Interaction<PasswordViewModel, SecureString?> ShowPassword { get; }
}