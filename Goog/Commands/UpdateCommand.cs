using CommandLine;

namespace Goog.Commands
{
    [Verb("update", HelpText = "Update the server installation")]
    internal class UpdateCommand : ITestLiveOption, ICommand
    {
        [Option("reinstall", HelpText = "Force to reinstall Goog, deleting all install")]
        public bool reinstall { get; set; }

        public bool testlive { get; set; }

        public void Execute()
        {
            Config config = Config.LoadFile(Config.GetConfigPath(testlive));
            if (config.ServerInstanceCount == 0)
                return;

            for (int i = 0; i < config.ServerInstanceCount; i++)
            {
                Tools.WriteColoredLine($"Updating server {i} binaries...", ConsoleColor.Cyan);
                Task<int> updateServer = Setup.UpdateServer(config, i, default, reinstall);
                updateServer.Wait();
                if (updateServer.Result != 0)
                    throw new Exception($"SteamCMD failed to update");
            }
        }
    }
}