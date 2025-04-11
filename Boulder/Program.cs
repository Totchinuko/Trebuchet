using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Boulder.Commands;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using tot_lib;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetLib.YuuIni;
using IConsole = System.CommandLine.IConsole;

namespace Boulder;

class Program
{
    private static bool _testlive = false;
    private static bool _experiment = false;
    
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Boulder - Trebuchet's CLI");
        rootCommand.CreateTot<LambCommand>(ConfigureServices);
        var parser = new CommandLineBuilder(rootCommand).UseDefaults().Build();

        var result = parser.Parse(args);
        _testlive = result.UnmatchedTokens.Contains(Constants.argTestLive);
        _experiment = result.UnmatchedTokens.Contains(Constants.argExperiment);
        return await result.InvokeAsync();
    }

    static void ConfigureServices(IServiceCollection collection)
    {
        collection.AddSingleton<AppSetup>(
            new AppSetup(Config.LoadConfig(Constants.GetConfigPath(_testlive)), _testlive, false, _experiment));
        collection.AddLogging(builder => builder.AddSerilog(GetLogger(), true));
        collection.AddSingleton<AppClientFiles>();
        collection.AddSingleton<AppServerFiles>();
        collection.AddSingleton<AppModlistFiles>();
        collection.AddSingleton<AppFiles>();
        collection.AddSingleton<Launcher>();
        collection.AddSingleton<IIniGenerator, YuuIniGenerator>();
        collection.AddSingleton<IConsole>(new DotnetConsole());
    }

    static ILogger GetLogger()
    {
        return new LoggerConfiguration()
#if !DEBUG
            .MinimumLevel.Information()
#endif
            .WriteTo.File(
                Path.Combine(Constants.GetLoggingDirectory().FullName, "boulder.log"),
                retainedFileTimeLimit: TimeSpan.FromDays(7),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}