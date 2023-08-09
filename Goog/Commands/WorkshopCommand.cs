using CommandLine;

namespace Goog.Commands
{
    [Verb("workshop", HelpText = "All interaction with the workshop are done with this command")]
    internal class WorkshopCommand : ICommand, ITestLiveOption
    {
        [Option('m', "modlist", Required = true)]
        public string? modlist { get; set; }

        public bool testlive { get; set; }

        public void Execute()
        {
            if (modlist == null)
                throw new ArgumentException("modlist is required.");

            Config config = Config.LoadFile(Config.GetConfigPath(testlive));
            Task<int> updateServer = Setup.UpdateMods(config, modlist, default);
            updateServer.Wait();
            if (updateServer.Result != 0)
                throw new Exception($"SteamCMD failed to update");
        }
    }
}