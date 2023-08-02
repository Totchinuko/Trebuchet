using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("delete", HelpText = "Delete the selected profile")]
    internal class DeleteCommand : ICommand, ITestLiveOption, IProfileOption
    {
        public bool testlive { get; set; }
        public string? profile { get; set; }

        [Option("force", HelpText = "Avoid the confirmation prompt")]
        public bool force { get; set; }

        public void Execute()
        {
            Profile.Load(testlive, this.profile, out _, out Profile? profile);

            string prev = profile.ProfileName;
            if(!force)
            {
                Console.Write("Are you sure ? (y/n)");
                string answer = Console.ReadLine() ?? "";
                if (answer.ToLower() != "y")
                    return;
            }

            profile.DeleteProfile();
            Tools.WriteColoredLine($"Profile {prev} has been deleted", ConsoleColor.Cyan);
        }
    }
}
