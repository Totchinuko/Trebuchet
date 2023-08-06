﻿using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("list", HelpText = "List all the profiles")]
    internal class ListCommand : ITestLiveOption, ICommand
    {
        public bool testlive { get; set; }

        public void Execute()
        {
            Config.Load(out Config config, testlive);

            if (!config.FolderServerProfiles.Exists)
                throw new DirectoryNotFoundException($"{config.FolderServerProfiles.FullName} was not found");

            foreach(string dir in Directory.GetDirectories(config.FolderServerProfiles.FullName))
                Console.WriteLine(Path.GetFileName(dir));
        }
    }
}
