using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog.Commands
{
    [Verb("kill", HelpText = "Terminate the processes")]
    internal class KillCommand : ICommand
    {
        [Option('c', "client", HelpText = "Start the client")]
        public bool client { get; set; }
        [Option('s', "server", HelpText = "Start the server")]
        public bool server { get; set; }

        public void Execute()
        {
            if(client)
            {
                Process[] clients = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Config.FileClientBin));
                foreach (Process client in clients)
                {
                    var pid = client.Id;
                    client.Kill();
                    Tools.WriteColoredLine($"Killed client process with PID={pid}", ConsoleColor.Cyan);
                }
            }
            
            if(server)
            {
                Process[] servers = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Config.FileServerBin));
                foreach (Process server in servers)
                {
                    var pid = server.Id;
                    server.Kill();
                    Tools.WriteColoredLine($"Killed server process with PID={pid}", ConsoleColor.Cyan);
                }
            }            
        }
    }
}
