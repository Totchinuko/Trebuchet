using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("resolve", HelpText = "Resolve the path of the mods contained in a profile")]
    internal class ResolveCommand : ICommand, IProfileOption, ITestLiveOption
    {
        public string? profile { get; set; }
        public bool testlive { get; set; }

        public void Execute()
        {
            Profile.Load(testlive, this.profile, out Config config, out Profile? profile);

            int count = config.ResolveModsPath(profile.Modlist.Modlist, out List<string> modlist, out List<string> errors);
            profile.Modlist.Modlist = modlist;

            profile.SaveProfile();

            if(count != modlist.Count)
            {
                Tools.WriteColoredLine("These mods could not be resolved:", ConsoleColor.Cyan);
                foreach(string mod in errors)
                    Tools.WriteColoredLine(mod, ConsoleColor.Yellow);
            }

            Tools.WriteColoredLine($"Resolver {count} mod path over {modlist.Count}", ConsoleColor.Cyan);
        }
    }
}
