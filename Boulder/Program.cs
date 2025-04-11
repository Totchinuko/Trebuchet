using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Boulder.Commands;
using Microsoft.Extensions.DependencyInjection;
using tot_lib;
using TrebuchetLib;
using TrebuchetLib.Services;
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
        collection.AddSingleton<AppFiles>();
        collection.AddSingleton<Launcher>();
        collection.AddSingleton<IConsole>(new DotnetConsole());
    }
}