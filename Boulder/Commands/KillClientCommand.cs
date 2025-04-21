using System.CommandLine;
using Microsoft.Extensions.Logging;
using tot_lib;
using tot_lib.CommandLine;
using TrebuchetLib.Services;

namespace Boulder.Commands;

public class KillClientCommand(Launcher launcher, ILogger<KillServerCommand> logger) : IInvokableCommand<KillClientCommand>
{
    public static readonly Command Command = CommandBuilder
        .CreateInvokable<KillClientCommand>("client", "Kill a client process")
        .SetServiceConfiguration(Program.ConfigureServices)
        .BuildCommand();
    
    public int Instance { get; private set; }
    public async Task<int> InvokeAsync(CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Searching for process...");
            var process = await launcher.FindClientProcess();
            if (process is null)
            {
                logger.LogInformation("Did not find Conan Exiles proces");
                return 0;
            }
            
            logger.LogInformation("Found process: {pid} ({name}.exe)", 
                process.Process.Id,
                process.Process.ProcessName);
                    
            process.Process.Kill();
            await process.Process.WaitForExitAsync(ct);
            logger.LogInformation("Killed");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to kill process");
            return ex.GetErrorCode();
        }
    }
}