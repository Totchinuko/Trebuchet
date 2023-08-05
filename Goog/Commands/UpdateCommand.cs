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

            Tools.WriteColoredLine("Updating server binaries...", ConsoleColor.Cyan);
            Task<int> updateServer = Setup.UpdateServer(config, default, reinstall);
            updateServer.Wait();
            if (updateServer.Result != 7 && updateServer.Result != 0)
                throw new Exception($"SteamCMD failed to update and returned {updateServer.Result}");
        }
    }
}