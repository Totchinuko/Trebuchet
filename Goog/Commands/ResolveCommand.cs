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

            Config config = Tools.LoadFile<Config>(Config.GetConfigPath(testlive));
            string modlistFile = ModListProfile.GetModlistPath(config, this.modlist);
            if (!File.Exists(modlistFile))
                throw new FileNotFoundException($"{this.modlist} is not found");

            ModListProfile modlistProfile = Tools.LoadFile<ModListProfile>(modlistFile);

            int count = config.ResolveModsPath(modlistProfile.Modlist, out List<string> modlist, out List<string> errors);
            modlistProfile.Modlist = modlist;

            modlistProfile.SaveFile();

            if (count != modlist.Count)
            {
                Tools.WriteColoredLine("These mods could not be resolved:", ConsoleColor.Cyan);
                foreach(string mod in errors)
                    Tools.WriteColoredLine(mod, ConsoleColor.Yellow);
            }

            Tools.WriteColoredLine($"Resolver {count} mod path over {modlist.Count}", ConsoleColor.Cyan);
        }
    }
}
