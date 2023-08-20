using SteamKit2.CDN;
using System.Collections.Concurrent;

namespace Goog
{
    /// <summary>
    /// CDNClientPool provides a pool of connections to CDN endpoints, requesting CDN tokens as needed
    /// </summary>
    internal class CDNClientPool
    {
        private const int ServerEndpointMinimumSize = 8;

        private readonly ConcurrentStack<Server> activeConnectionPool;
        private readonly uint appId;
        private readonly BlockingCollection<Server> availableServerEndpoints;
        private readonly Task monitorTask;
        private readonly AutoResetEvent populatePoolEvent;
        private readonly CancellationTokenSource shutdownToken;
        private readonly SteamSession steamSession;

        public CDNClientPool(SteamSession steamSession, uint appId)
        {
            this.steamSession = steamSession;
            this.appId = appId;
            CDNClient = new Client(steamSession.Client);

            activeConnectionPool = new ConcurrentStack<Server>();
            availableServerEndpoints = new BlockingCollection<Server>();

            populatePoolEvent = new AutoResetEvent(true);
            shutdownToken = new CancellationTokenSource();

            monitorTask = Task.Factory.StartNew(ConnectionPoolMonitorAsync).Unwrap();
        }

        public Client CDNClient { get; }

        public CancellationTokenSource? ExhaustedToken { get; set; }

        public Server? ProxyServer { get; private set; }

        public Server GetConnection(CancellationToken token)
        {
            if (!activeConnectionPool.TryPop(out var connection))
            {
                connection = BuildConnection(token);
            }

            return connection;
        }

        public void ReturnBrokenConnection(Server? server)
        {
            if (server == null) return;

            // Broken connections are not returned to the pool
        }

        public void ReturnConnection(Server? server)
        {
            if (server == null) return;

            activeConnectionPool.Push(server);
        }

        public void Shutdown()
        {
            shutdownToken.Cancel();
            monitorTask.Wait();
        }

        private Server BuildConnection(CancellationToken token)
        {
            if (availableServerEndpoints.Count < ServerEndpointMinimumSize)
            {
                populatePoolEvent.Set();
            }

            return availableServerEndpoints.Take(token);
        }

        private async Task ConnectionPoolMonitorAsync()
        {
            var didPopulate = false;

            while (!shutdownToken.IsCancellationRequested)
            {
                populatePoolEvent.WaitOne(TimeSpan.FromSeconds(1));

                // We want the Steam session so we can take the CellID from the session and pass it through to the ContentServer Directory Service
                if (availableServerEndpoints.Count < ServerEndpointMinimumSize && steamSession.Client.IsConnected)
                {
                    var servers = await FetchBootstrapServerListAsync().ConfigureAwait(false);

                    if (servers == null || servers.Count == 0)
                    {
                        ExhaustedToken?.Cancel();
                        return;
                    }

                    ProxyServer = servers.Where(x => x.UseAsProxy).FirstOrDefault();

                    var weightedCdnServers = servers
                        .Where(server =>
                        {
                            var isEligibleForApp = server.AllowedAppIds.Length == 0 || server.AllowedAppIds.Contains(appId);
                            return isEligibleForApp && (server.Type == "SteamCache" || server.Type == "CDN");
                        })
                        .OrderBy(server => server.WeightedLoad);

                    foreach (var server in weightedCdnServers)
                    {
                        for (var i = 0; i < server.NumEntries; i++)
                        {
                            availableServerEndpoints.Add(server);
                        }
                    }

                    didPopulate = true;
                }
                else if (availableServerEndpoints.Count == 0 && !steamSession.Client.IsConnected && didPopulate)
                {
                    ExhaustedToken?.Cancel();
                    return;
                }
            }
        }

        private async Task<IReadOnlyCollection<Server>> FetchBootstrapServerListAsync()
        {
            try
            {
                var cdnServers = await this.steamSession.Content.GetServersForSteamPipe();
                if (cdnServers != null)
                    return cdnServers;

                throw new Exception("Server list is empty.");
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to retrieve content server list.", ex);
            }
        }
    }
}