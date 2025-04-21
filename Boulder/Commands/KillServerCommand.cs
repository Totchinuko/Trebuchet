using System.CommandLine;
using Microsoft.Extensions.Logging;
using tot_lib;
using tot_lib.CommandLine;
using TrebuchetLib.Services;

namespace Boulder.Commands;

public class KillServerCommand(Launcher launcher, ILogger<KillServerCommand> logger) : IInvokableCommand<KillServerCommand>
{
    public static readonly Command Command = CommandBuilder
        .CreateInvokable<KillServerCommand>("server", "Kill a server instance")
        .SetServiceConfiguration(Program.ConfigureServices)
        .Arguments.Create<int>("instance")
        .SetSetter((c,v) => c.Instance = v).SetDefault(0)
        .BuildArgument()
        .BuildCommand();
    
    public int Instance { get; private set; }
    public async Task<int> InvokeAsync(CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Searching for process...");
            await foreach (var process in launcher.FindServerProcesses())
            {
                if (process.Instance != Instance) continue;
                
                logger.LogInformation("Found process: {pid} ({name}.exe)", 
                    process.Process.Id,
                    process.Process.ProcessName);
                    
                process.Process.Kill();
                await process.Process.WaitForExitAsync(ct);
                logger.LogInformation("Killed");
                return 0;
            }
            logger.LogInformation("Did not find process for instance {intance}", Instance);
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to kill process");
            return ex.GetErrorCode();
        }
    }
}