namespace Trebuchet
{
    public class ServerInstanceInformation
    {
        public int Port { get; }

        public int QueryPort { get; }

        public int RconPort { get; }

        public string Title { get; }

        public int Instance { get; }

        public ServerInstanceInformation(int instance, string title, int port, int queryPort, int rconPort)
        {
            Port = port;
            QueryPort = queryPort;
            RconPort = rconPort;
            Title = title;
            Instance = instance;
        }
    }
}