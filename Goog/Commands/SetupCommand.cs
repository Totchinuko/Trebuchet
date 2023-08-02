using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("setup", HelpText = "Setup a few information for the command proper function")]
    internal class SetupCommand : ICommand, ITestLiveOption
    {
        public const string steamCMDZipFile = "SteamCMD.zip";

        [Option('u', "url", Hidden = true, Default = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip", HelpText = "Set the url to download steamCMD in case the original url changed")]
        public string? SteamCMDURL { get; set; }

        [Option("reinstall", HelpText = "Force to reinstall Goog, deleting all install")]
        public bool reinstall { get; set; }
        public bool testlive { get; set; }

        public void Execute()
        {
            Config.Load(out Config config, testlive);

            if (string.IsNullOrEmpty(config.InstallPath))
                throw new Exception("install-path is not configured properly");

            if(string.IsNullOrEmpty(SteamCMDURL))
                throw new Exception("url is invalid");

            if (config.SteamCMD.Exists && !reinstall)
                throw new Exception("Install already exists, use --reinstall to force reinstall");

            var installPath = new DirectoryInfo(config.InstallPath);
            Tools.CreateDir(installPath);
            Tools.DeleteIfExists(config.SteamFolder, true);
            Tools.CreateDir(config.SteamFolder);
            Tools.RemoveSymboliclink(config.ServerSaveFolder.FullName);
            Tools.DeleteIfExists(config.ServerFolder, true);

            FileInfo steamCmdZip = new FileInfo(Path.Join(installPath.FullName, steamCMDZipFile));
            Tools.DeleteIfExists(steamCmdZip);
            
            Task<bool> task = Tools.DownloadSteamCMD(SteamCMDURL, steamCmdZip, null);
            task.Wait();
            if (!task.Result)
                throw new Exception("Could not download steam CMD");

            steamCmdZip.Refresh();
            if (!steamCmdZip.Exists)
                throw new FileNotFoundException("Steam CMD downloaded file not found");

            Tools.UnzipFile(steamCmdZip, config.SteamFolder);

            FileInfo steamExe = new FileInfo(Path.Join(config.SteamFolder.FullName, Config.steamCMDBin));
            if (!steamExe.Exists)
                throw new FileNotFoundException($"{steamExe.FullName} was not found after extraction");

            Tools.DeleteIfExists(steamCmdZip);

            if(config.ManageServer)
            {
                UpdateCommand updateCommand = new UpdateCommand()
                {
                    reinstall = reinstall,
                    testlive = testlive
                };

                updateCommand.Execute();
            }
        }
    }
}
