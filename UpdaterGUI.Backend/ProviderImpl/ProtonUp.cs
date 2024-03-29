using System.Security;
using DynamicData.Kernel;

namespace UpdaterGUI.Backend.ProviderImpl;

internal class ProtonUp : IProvider
{
    public string Name => "ProtonUp";
    public bool RootRequired => false;
    public List<Package> Packages { get; }
    public int Installed { get; private set; }
    public int Total { get; private set; }

    public ProtonUp()
    {
        Packages = new();
    }
    public void Load(bool cached)
    {
        Packages.Clear();
        var result = Command.Run("protonup --releases");
        if (result.ExitCode != 0) throw new CommandException(result.Error);
        Packages.AddRange(result.Output.Split('\n')
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(version => new Package
            {
                Provider = Name,
                Version = version,
                Name = "Proton GE",
                QualifiedName = $"Proton GE {version}"
            }));
        result = Command.Run("protonup -l");
        if (result.ExitCode != 0) throw new CommandException(result.Error);
        var installed = result.Output.Split("\n").ToList();
        foreach(var package in Packages)
        {
            package.IsInstalled = installed.FirstOrOptional(i => i.Split(" - ")[0] == package.Version).HasValue;
        }
        Total = Packages.Count;
        Installed = Packages.Count(p => p.IsInstalled);
    }
    public string PackageInfo(Package package) => Packages.First(p => p.Version == package.Version).Version ?? "";

    public Task<int> Install(SecureString? pass, IList<Package> packages, Action<string?> output, Action<string?> error)
    {
        foreach (var package in packages)
        {
            var exitcode = Command.Run($"protonup -t {package.Version} -y", output, error);
            if (exitcode.Result != 0) return Task.FromResult(exitcode.Result);
        }

        return Task.FromResult(0);
    }


    public Task<int> Remove(SecureString? pass, IList<Package> packages, Action<string?> output, Action<string?> error)
    {
        foreach (var package in packages)
        {
            var exitcode = Command.Run($"protonup -r {package.Version} -y", output, error);
            if (exitcode.Result != 0) return exitcode;
        }
        return Task.FromResult(0);
    }

    public Task<int> Update(SecureString? pass, Action<string?> output, Action<string?> error) =>
        Command.Run("protonup -y", output, error);
    
    public Command Version() => Command.Run("protonup -h");
}