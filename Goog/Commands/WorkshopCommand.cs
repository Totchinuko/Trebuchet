using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("workshop", HelpText = "All interaction with the workshop are done with this command")]
    internal class WorkshopCommand : ICommand, IProfileOption, ITestLiveOption
    {
        public string? profile { get; set; }
        public bool testlive { get; set; }

        public void Execute()
        {
            Profile.Load(testlive, this.profile, out Config config, out Profile? profile);

            List<string> list = profile.Modlist.GetModIDList();
            if (list.Count == 0)
                Tools.WriteColoredLine("Nothing to update", ConsoleColor.Cyan);

            List<string> updates = new List<string>();
            list.ForEach(x => updates.Add(string.Format(Config.CmdArgWorkshopUpdate, config.ClientAppID, x)));

            Process process = new Process();
            process.StartInfo.FileName = config.SteamCMD.FullName;
            process.StartInfo.Arguments = string.Join(" ",
                    Config.CmdArgLoginAnonymous,
                    string.Join(" ", updates.ToArray()),
                    Config.CmdArgQuit
                );

            process.StartInfo.UseShellExecute = false;
            Tools.WriteColoredLine($"Updating {list.Count} mods in {profile.ProfileName}...", ConsoleColor.Cyan);
            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception($"SteamCMD failed to update and returned {process.ExitCode}");
        }
    }
}
