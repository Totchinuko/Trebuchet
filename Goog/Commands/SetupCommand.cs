using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("setup", HelpText = "Setup a few information for the command proper function")]
    internal class SetupCommand : ICommand, ITestLiveOption
    {
        [Option("reinstall", HelpText = "Force to reinstall Goog, deleting all install")]
        public bool reinstall { get; set; }

        [Option('u', "url", Hidden = true, Default = Setup.SteamCMDURL, HelpText = "Set the url to download steamCMD in case the original url changed")]
        public string? SteamCMDURL { get; set; }

        public bool testlive { get; set; }

        public void Execute()
        {
            Config.Load(out Config config, testlive);

            Tools.WriteColoredLine("Installing SteamCMD binaries...", ConsoleColor.Cyan);
            Task install = Setup.SetupApp(config, default, reinstall);
            install.Wait();

            if (config.ManageServers)
            {
                Tools.WriteColoredLine("Installing server binaries...", ConsoleColor.Cyan);
                Task<int> updateServer = Setup.UpdateServer(config, default, reinstall);
                updateServer.Wait();
                if (updateServer.Result != 7 && updateServer.Result != 0)
                    throw new Exception($"SteamCMD failed to update and returned {updateServer.Result}");
            }
        }
    }
}