using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trebuchet
{
    public struct ServerState
    {
        public int MaxPlayers;
        public string Name;
        public bool Online;
        public int Players;

        public ServerState(bool online, string name, int players, int maxPlayers)
        {
            MaxPlayers = maxPlayers;
            Name = name;
            Online = online;
            Players = players;
        }
    }
}