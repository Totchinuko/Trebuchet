using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog
{
    public interface IProfileOption
    {
        [Option('p', "profile", HelpText = "Choose a specific profile (Goog use the current folder first, then the last one used")]
        public string? profile { get; set; }
    }
}
