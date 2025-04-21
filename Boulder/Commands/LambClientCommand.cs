using System.CommandLine;
using System.CommandLine.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using tot_lib;
using tot_lib.CommandLine;
using TrebuchetLib.Services;

namespace Boulder.Commands;

public class LambClientCommand(Launcher launcher, ILogger<LambClientCommand> logger) : IInvokableCommand<LambClientCommand>
{
    public static readonly Command Command = CommandBuilder
        .CreateInvokable<LambClientCommand>("client", "Start a conan exile game process and exit")
        .SetServiceConfiguration(Program.ConfigureServices)
        
        .Options.Create<string>("--modlist", "modlist name as seen in trebuchet").AddAlias("-m")
        .SetSetter((c,v) => c.Modlist = v ?? string.Empty).BuildOption()
        
        .Options.Create<string>("--save", "client save name as seen in trebuchet").AddAlias("-s")
        .SetSetter((c,v) => c.Profile = v ?? string.Empty).BuildOption()
        
        .Options.Create<bool>("--battle-eye", "start with battle eye").AddAlias("-b")
        .SetSetter((c,v) => c.BattleEye = v).SetDefault(false).BuildOption()
        
        .Options.Create<bool>("--auto-connect", "Try to auto connect to the modlist server after launch")
        .SetDefault(false).AddAlias("-a").SetSetter((c,v) => c.AutoConnect = v).BuildOption()
        .BuildCommand();
    public string Profile { get; set; } = string.Empty;
    public string Modlist { get; set; } = string.Empty;
    public bool BattleEye { get; set; } = false;
    public bool AutoConnect { get; set; } = false;
    
    public async Task<int> InvokeAsync(CancellationToken token)
    {
        try
        {
            var data = new Dictionary<string, object>
            {
                { "profile", Profile },
                { "modlist", Modlist },
                { "battle-eye", BattleEye }
            };
            using(logger.BeginScope(data))
                logger.LogInformation("Starting process");
            var process = await launcher.CatapultClientProcess(Profile, Modlist, BattleEye, AutoConnect);
            logger.LogInformation("Process Started: {pid} ({name})", process.Id, process.ProcessName);
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to start process");
            return ex.GetErrorCode();
        }
    }
}