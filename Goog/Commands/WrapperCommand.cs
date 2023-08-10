using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("wrapper", Hidden = true)]
    public class WrapperCommand : ICommand
    {
        [Option('a', "arguments")]
        public string? steamArgs { get; set; }

        public void Execute()
        {
            Config config = Config.LoadFile(Config.GetPath(false));

            string steamCMD = Path.Combine(config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin);
            Process process = new Process();
            process.StartInfo.FileName = steamCMD;
            process.StartInfo.Arguments = steamArgs;
            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0 && process.ExitCode != 7)
                throw new Exception($"Steam CMD terminated with error code {process.ExitCode}");
        }
    }
}
