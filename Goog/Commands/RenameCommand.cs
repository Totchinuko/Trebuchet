using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("rename", HelpText = "Rename the selected profile")]
    internal class RenameCommand : ICommand, ITestLiveOption, IProfileOption
    {
        public bool testlive { get; set; }
        public string? profile { get; set; }

        [Option("name", Required = true, HelpText = "New name for your profile")]
        public string? name { get; set; }

        public void Execute()
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Missing argument name");

            Profile.Load(testlive, this.profile, out Config config, out Profile? profile);

            string prev = profile.ProfileName;
            profile.MoveTo(Path.Combine(config.ProfilesFolder.FullName, name, Config.profileConfigName));
            config.SetLastProfile(profile);

            Tools.WriteColoredLine($"Profile {prev} has been renamed to {profile.ProfileName}", ConsoleColor.Cyan);
        }
    }
}
