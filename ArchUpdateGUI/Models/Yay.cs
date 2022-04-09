using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchUpdateGUI.Utils;

namespace ArchUpdateGUI.Models;

public class Yay : IProvider
{
    public string Name => "Yay - AUR Helper";
    public List<Package> Packages { get; }
    public int Installed { get; }
    public int Total { get; }
    
    public Yay()
    {
        Packages = new();
        var result = Command.Run("yay -Sl");
        if (result.ExitCode != 0) throw new CommandException(result.Error);
        string[] list = result.Output.Split('\n');
        foreach (var package in list)
        {
            string[] listPackage = package.Split(' ');
            if (listPackage.Length > 1)
                Packages.Add(new()
                {
                    Provider = "yay",
                    Repository = listPackage[0],
                    Name = listPackage[1],
                    Version = listPackage[2],
                    IsInstalled = listPackage.Length == 4
                });
        }

        Installed = Packages.Count(p => p.IsInstalled);
        Total = Packages.Count;
    }
    public string Search(Package package)
    {
        var result = Command.Run($"yay -Si {package.Name}");
        if (result.ExitCode != 0) throw new CommandException(result.Error);
        return result.Output;
    }

    public void Install(Package package)
    {
        var result = Command.Run($"yay -Ss {package.Name}"); //TODO
        if (result.ExitCode != 0) throw new CommandException(result.Error);
    }

    public void Remove(Package package)
    {
        var result = Command.Run($"yay -Rs {package.Name}"); //TODO
        if (result.ExitCode != 0) throw new CommandException(result.Error);
    }

    public Task<int> Update(Action<string?> output, Action<string?> error) =>
        Command.Run("printf '%s\n' <PASSWORD> | yay -Syu --noconfirm", output, error); //TODO

    public Command Version() => Command.Run("yay --version");
}