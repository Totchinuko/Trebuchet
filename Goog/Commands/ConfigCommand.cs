using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("config", HelpText = "Set a Configuration variable")]
    internal class ConfigCommand : ICommand, ITestLiveOption
    {
        [Option('i', "install-path", HelpText = "Path to your desired location for goog own files")]
        public string? InstallPath { get; set; }

        [Option('s', "manage-server", HelpText = "Choose if goog should handle server executables")]
        public bool? ManageServer { get; set; }

        [Option('g', "game-path", HelpText = "Path to your Conan Exile Game installation")]
        public string? ClientPath { get; set; }

        public bool testlive { get; set; }

        public void Execute()
        {
            Config.Load(out Config config, testlive);
            
            if(!string.IsNullOrEmpty(InstallPath))
            {
                DirectoryInfo installDir = new DirectoryInfo(InstallPath);
                config.InstallPath = installDir.FullName;
                Tools.WriteColoredLine("install-path Configured", ConsoleColor.Cyan);
            }

            if (!string.IsNullOrEmpty(ClientPath))
            {
                DirectoryInfo gameDir = new DirectoryInfo(ClientPath);
                if (!gameDir.Exists)
                    throw new DirectoryNotFoundException($"{gameDir.FullName} was not found");
                config.ClientPath = gameDir.FullName;
                Tools.WriteColoredLine("game-path Configured", ConsoleColor.Cyan);
            }

            if (ManageServer != null)
            {
                config.ManageServer = ManageServer ?? false;
                Tools.WriteColoredLine("manage-server Configured", ConsoleColor.Cyan);
            }

            config.SaveConfig();
        }
    }
}
