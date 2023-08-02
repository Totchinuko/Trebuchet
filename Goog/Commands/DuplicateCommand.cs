using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("duplicate", HelpText = "Duplicate the selected profile")]
    internal class DuplicateCommand : ICommand, ITestLiveOption, IProfileOption
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

            profile.CopyTo(Path.Combine(config.ProfilesFolder.FullName, name, Config.profileConfigName));

            Tools.WriteColoredLine($"Profile {profile.ProfileName} has been duplicated to {name}", ConsoleColor.Cyan);
        }
    }
}
