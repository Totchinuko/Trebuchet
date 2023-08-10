using CommandLine;
using GoogLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("resolve", HelpText = "Resolve the path of the mods contained in a profile")]
    internal class ResolveCommand : ICommand, ITestLiveOption
    {
        [Option('m', "modlist", Required = true)]
        public string? modlist  { get; set; }
        public bool testlive { get; set; }

        public void Execute()
        {
            if (this.modlist == null)
                throw new ArgumentException("modlist parameter is required.");

            Config config = Config.LoadFile(Config.GetPath(testlive));
            string modlistFile = ModListProfile.GetPath(config, this.modlist);
            if (!File.Exists(modlistFile))
                throw new FileNotFoundException($"{this.modlist} is not found");

            ModListProfile modlistProfile = ModListProfile.LoadFile(modlistFile);

            config.ResolveModsPath(modlistProfile.Modlist, out List<string> modlist, out List<string> errors);
            modlistProfile.Modlist = modlist;

            modlistProfile.SaveFile();

            if (errors.Count > 0)
            {
                Tools.WriteColoredLine("These mods could not be resolved:", ConsoleColor.Cyan);
                foreach(string mod in errors)
                    Tools.WriteColoredLine(mod, ConsoleColor.Yellow);
            }

            Tools.WriteColoredLine($"Resolver {modlist.Count - errors.Count} mod path over {modlist.Count}", ConsoleColor.Cyan);
        }
    }
}
