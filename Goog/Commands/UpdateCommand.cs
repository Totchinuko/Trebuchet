using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("update", HelpText = "Update the server installation")]
    internal class UpdateCommand : ITestLiveOption, ICommand
    {
        public bool testlive { get; set; }

        [Option("reinstall", HelpText = "Force to reinstall Goog, deleting all install")]
        public bool reinstall { get; set; }

        public void Execute()
        {
            Config.Load(out Config config, testlive);

            if (reinstall)
                Tools.DeleteIfExists(config.ServerFolder, true);

            Tools.CreateDir(config.ServerFolder);

            Tools.WriteColoredLine("Installing server binaries...", ConsoleColor.Cyan);
            Process process = new Process();
            process.StartInfo.FileName = config.SteamCMD.FullName;
            process.StartInfo.Arguments = string.Join(" ",
                    string.Format(Config.CmdArgForceInstallDir, config.ServerFolder.FullName),
                    Config.CmdArgLoginAnonymous,
                    string.Format(Config.CmdArgAppUpdate, config.ServerAppID),
                    Config.CmdArgQuit
                );
            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 7 && process.ExitCode != 0)
                throw new Exception($"SteamCMD failed to update and returned {process.ExitCode}");
        }
    }
}
