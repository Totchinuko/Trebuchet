using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("create", HelpText = "Create a new profile")]
    internal class CreateCommand : ICommand, ITestLiveOption
    {
        public bool testlive { get; set; }
        [Option("name", Required = true, HelpText = "Name of your profile")]
        public string? name { get; set; }

        public void Execute()
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Missing argument name");

            Config.Load(out Config config, testlive);

            Profile.Create(Path.Combine(config.ProfilesFolder.FullName, name, Config.profileConfigName), out Profile? profile);
            Tools.WriteColoredLine("Profile created", ConsoleColor.Cyan);
        }
    }
}
