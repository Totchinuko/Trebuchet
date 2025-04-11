using System.CommandLine;
using System.CommandLine.IO;
using Microsoft.Extensions.DependencyInjection;
using tot_lib;
using TrebuchetLib.Services;

namespace Boulder.Commands;

public class LambClientCommand : ITotCommand, ITotCommandInvoked, ITotCommandOptions
{
    public string Command => "client";
    public string Description => "Start a conan exile game process and exit";
    public string Profile { get; set; } = string.Empty;
    public string Modlist { get; set; } = string.Empty;
    public bool BattleEye { get; set; } = false;
    
    public IEnumerable<Option> GetOptions()
    {
        var modlistOpt = new TotOption<string>("--modlist", "modlist name as seen in trebuchet");
        modlistOpt.AddAlias("-m");
        modlistOpt.AddSetter(x => Modlist = x ?? string.Empty);
        yield return modlistOpt;
        var profileOpt = new TotOption<string>("--save", "client save name as seen in trebuchet");
        profileOpt.AddAlias("-s");
        profileOpt.AddSetter(x => Profile = x ?? string.Empty);
        yield return profileOpt;
        var battleEyeOpt = new TotOption<bool>("--battle-eye", "start with battle eye");
        battleEyeOpt.AddAlias("-b");
        battleEyeOpt.AddSetter(x => BattleEye = x);
        yield return battleEyeOpt;
    }
    
    public async Task<int> InvokeAsync(IServiceProvider provider, CancellationToken token)
    {
        var launcher = provider.GetRequiredService<Launcher>();
        var console = provider.GetRequiredService<IConsole>();

        try
        {
            var process = await launcher.CatapultClientProcess(Profile, Modlist, BattleEye);
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