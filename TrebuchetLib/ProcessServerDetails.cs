using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yuu.Ini;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TrebuchetLib
{
    public class ProcessServerDetails : ProcessDetails
    {
        public ProcessServerDetails(int instance, ServerProfile profile, ModListProfile modlist) : base(profile.ProfileName, modlist.ProfileName)
        {
            Instance = instance;
            Port = profile.GameClientPort;
            QueryPort = profile.SourceQueryPort;
            RconPort = profile.RConPort;
            Title = profile.ServerName;
            RconPassword = profile.RConPassword;
        }

        public ProcessServerDetails(ProcessServerDetails details, ProcessState state) : base(details, state)
        {
            Instance = details.Instance;
            Port = details.Port;
            QueryPort = details.QueryPort;
            RconPort = details.RconPort;
            Title = details.Title;
            RconPassword = details.RconPassword;
        }

        public ProcessServerDetails(ProcessServerDetails details, int players, int maxPlayers, ProcessState state) : base(details, state)
        {
            Instance = details.Instance;
            Port = details.Port;
            QueryPort = details.QueryPort;
            RconPort = details.RconPort;
            Title = details.Title;
            RconPassword = details.RconPassword;
            Players = players;
            MaxPlayers = maxPlayers;
        }

        public ProcessServerDetails(ProcessServerDetails details, ProcessData data, ProcessState state) : base(details, data, state)
        {
            Instance = details.Instance;
            Port = details.Port;
            QueryPort = details.QueryPort;
            RconPort = details.RconPort;
            Title = details.Title;
            RconPassword = details.RconPassword;
        }

        public ProcessServerDetails(ProcessServerDetails details, string instancePath, ProcessData data, ProcessState state) : base(details, data, state)
        {
            Instance = details.Instance;

            string initPath = Path.Combine(instancePath, string.Format(Config.FileIniServer, "Engine"));
            IniDocument document = IniParser.Parse(Tools.GetFileContent(initPath));

            IniSection section = document.GetSection("OnlineSubsystem");
            Title = section.GetValue("ServerName", "Conan Exile Dedicated Server");

            section = document.GetSection("URL");
            Port = section.GetValue("Port", 7777);

            section = document.GetSection("OnlineSubsystemSteam");
            QueryPort = section.GetValue("GameServerQueryPort", 27015);

            document = IniParser.Parse(Tools.GetFileContent(Path.Combine(instancePath, string.Format(Config.FileIniServer, "Game"))));
            section = document.GetSection("RconPlugin");
            RconPassword = section.GetValue("RconPassword", string.Empty);
            RconPort = section.GetValue("RconEnabled", false) ? section.GetValue("RconPort", 25575) : 0;
        }

        public int Instance { get; }

        public int MaxPlayers { get; set; }

        public int Players { get; set; }

        public int Port { get; }

        public int QueryPort { get; }

        public string RconPassword { get; }

        public int RconPort { get; }

        public string Title { get; }
    }

    public class ProcessServerDetailsEventArgs
    {
        public ProcessServerDetailsEventArgs(ProcessServerDetails oldDetails, ProcessServerDetails newDetails)
        {
            OldDetails = oldDetails;
            NewDetails = newDetails;
        }

        public ProcessServerDetails NewDetails { get; }

        public ProcessServerDetails OldDetails { get; }
    }
}