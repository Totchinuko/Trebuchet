using CommandLine.Text;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog
{
    internal interface ITestLiveOption
    {
        [Option("test-live", HelpText = "Choose the test live version rather than the live version")]
        public bool testlive { get; set; }
    }
}
