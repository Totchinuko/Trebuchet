namespace Trebuchet
{
    public class ServerInstanceInformation
    {
        public ServerInstanceInformation(int instance, string title, int port, int queryPort, int rconPort, string rconPassword)
        {
            Port = port;
            QueryPort = queryPort;
            RconPort = rconPort;
            Title = title;
            Instance = instance;
            RconPassword = rconPassword;
        }

        public int Instance { get; }

        public int Port { get; }

        public int QueryPort { get; }

        public string RconPassword { get; }

        public int RconPort { get; }

        public string Title { get; }
    }
}