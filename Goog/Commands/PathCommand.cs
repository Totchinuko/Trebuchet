using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("path", HelpText = "Return a path to use in cd")]
    internal class PathCommand : IProfileOption, ITestLiveOption, ICommand
    {
        public string? profile { get; set; }
        public bool testlive { get; set; }

        public void Execute()
        {
            if(profile !=null)
            {
                Profile.LoadStrict(testlive, this.profile, out _, out Profile? profile);
                Console.Write(profile.ProfileFile.FullName);
            }
        }
    }
}
