using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("run", HelpText = "Run an executable on a given profile")]
    internal class RunCommand : IProfileOption, ITestLiveOption, ICommand, IMapOption
    {
        public string? profile { get; set; }
        public bool testlive { get; set; }
        public string? map { get; set; }

        [Option('c',"client", HelpText = "Start the client")]
        public bool client { get; set; }
        [Option('s',"server", HelpText = "Start the server")]
        public bool server { get; set; }
        [Option("auto-resolve", HelpText = "If mod files are not found, try to auto resolve")]
        public bool autoresolve { get; set; }
        [Option("battle-eye", HelpText = "Start the client with battle eye enabled")]
        public bool battleEye { get; set; }

        public void Execute()
        {
            if (!server && !client)
                throw new ArgumentException("Missing argument server or client");

            Profile.Load(testlive, this.profile, out Config config, out Profile? profile);

            if (!profile.Modlist.IsValidModList())
            {
                if (!autoresolve)
                    throw new FileNotFoundException("Some mod files are not found, use resolve");
                
                WorkshopCommand workshop = new WorkshopCommand() { profile = this.profile, testlive = testlive };
                workshop.Execute();

                ResolveCommand resolver = new ResolveCommand() { profile = this.profile, testlive = testlive };
                resolver.Execute();

                Profile.Load(profile.ProfileFile.FullName, out profile);
                if (!profile.Modlist.IsValidModList())
                    throw new FileNotFoundException("Some mod files are not found");
            }

            File.WriteAllLines(profile.GeneratedModList.FullName, profile.Modlist.Modlist);

            KillCommand kill = new KillCommand();
            kill.Execute();

            Process? serverProcess = null;
            Process? clientProcess = null;
            if (server)
                ServerExecute(config, profile, out serverProcess);
            if (client)
                ClientExecute(config, profile, out clientProcess);

            if (serverProcess != null)
            {
                serverProcess.Start();
                Tools.WriteColoredLine("Server started", ConsoleColor.Cyan);
            }

            if(clientProcess != null)
            {
                clientProcess.Start();
                Tools.WriteColoredLine("Client started", ConsoleColor.Cyan);
            }
        }

        private void ServerExecute(Config config, Profile profile, out Process? server)
        {
            server = null;

            if (!config.ManageServers)
                throw new Exception("Goog is not configured to manage servers");

            if (profile == null || profile.ProfileFile.Directory == null)
                throw new Exception("Invalid profile");

            Tools.SetupSymboliclink(config.ServerSaveFolder.FullName, profile.ProfileFile.Directory.FullName);

            string targetMap = map ?? "";
            config.ResolveMap(ref targetMap, profile);

            server = new Process();
            if (!config.ServerBin.Exists)
                throw new FileNotFoundException("Server is not installed properly, binaries not found");

            List<string> args = new List<string>() { targetMap };
            if (profile.Server.Log) args.Add(Config.GameArgsLog);
            if (profile.Server.UseAllCores) args.Add(Config.GameArgsUseAllCore);
            args.Add(string.Format(Config.ServerArgsMaxPlayers, 10));
            args.Add(string.Format(Config.GameArgsModList, profile.GeneratedModList.FullName));

            server.StartInfo.FileName = config.ServerBin.FullName;
            server.StartInfo.Arguments = string.Join(" ", args);
            server.StartInfo.WorkingDirectory = config.ServerBinaryFolder.FullName;
        }

        private void ClientExecute(Config config, Profile profile, out Process? process)
        {
            process = new Process();
            if (!config.ClientBin.Exists)
                throw new Exception($"Client not found ({(string.IsNullOrEmpty(config.ClientPath) ? "Missing" : config.ClientPath)})");

            if (profile == null)
                throw new Exception("Invalid profile");


            List<string> args = new List<string>();

            if (profile.Client.Log) args.Add(Config.GameArgsLog);
            if (profile.Client.UseAllCores) args.Add(Config.GameArgsUseAllCore);
            args.Add(string.Format(Config.GameArgsModList, profile.GeneratedModList.FullName));

            process.StartInfo.FileName = battleEye ? config.ClientBEBin.FullName : config.ClientBin.FullName;
            process.StartInfo.Arguments = string.Join(" ", args);
            process.StartInfo.WorkingDirectory = config.ClientBinaries.FullName;
        }
    }
}
