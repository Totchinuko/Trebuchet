using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("conflict", HelpText = "Use Tot to evaluate the conflicts of a profile")]
    internal class ConflictCommand : ICommand, ITestLiveOption, IProfileOption
    {
        public bool testlive { get; set; }
        public string? profile { get; set; }

        public void Execute()
        {
            Profile.Load(testlive, this.profile, out _, out Profile? profile);

            File.WriteAllLines(profile.GeneratedModList.FullName, profile.Modlist.Modlist);

            Process process = new Process();
            process.StartInfo.FileName = "tot";
            process.StartInfo.Arguments = $"conflict {profile.GeneratedModList.FullName}";
            process.Start();
            process.WaitForExit();
        }
    }
}
