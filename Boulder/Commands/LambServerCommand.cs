using System.CommandLine;
using System.CommandLine.IO;
using Microsoft.Extensions.DependencyInjection;
using tot_lib;
using TrebuchetLib.Services;

namespace Boulder.Commands;

public class LambServerCommand : ITotCommand, ITotCommandInvoked, ITotCommandOptions
{
    public string Command => "server";
    public string Description => "Start a conan exile server process and exit";
    public string Profile { get; set; } = string.Empty;
    public string Modlist { get; set; } = string.Empty;
    public int Instance { get; set; } = 0;
    
    public IEnumerable<Option> GetOptions()
    {
        var modlistOpt = new TotOption<string>("--modlist", "modlist name as seen in trebuchet");
        modlistOpt.AddAlias("-m");
        modlistOpt.AddSetter(x => Modlist = x ?? string.Empty);
        yield return modlistOpt;
        var profileOpt = new TotOption<string>("--save", "server save name as seen in trebuchet");
        profileOpt.AddAlias("-s");
        profileOpt.AddSetter(x => Profile = x ?? string.Empty);
        yield return profileOpt;
        var instance = new TotOption<int>("--instance", "instance number of your trebuchet install");
        instance.AddAlias("-i");
        instance.AddSetter(x => Instance = x);
        instance.SetDefaultValue(0);
        yield return instance;
    }
    
    public async Task<int> InvokeAsync(IServiceProvider provider, CancellationToken token)
    {
        var launcher = provider.GetRequiredService<Launcher>();
        var console = provider.GetRequiredService<IConsole>();
        try
        {
            var process = await launcher.CatapultServerProcess(Profile, Modlist, Instance);
            console.Write(process.Id.ToString());
            return 0;
        }
        catch (Exception ex)
        {
            console.Error.WriteLine(ex.Message);
            return 1;
        }
    }


}