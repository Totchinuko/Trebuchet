using System.Net.Http.Headers;
using System.Net.Sockets;

/// GNU GENERAL PUBLIC LICENSE // Version 2, June 1991
/// Copyright (C) 2023 SteamRE https://opensteamworks.org
/// Full license text: LICENSE.txt at the project root
/// Modificatied by Totchinuko under the same license

namespace Trebuchet
{
    // This is based on the dotnet issue #44686 and its workaround at https://github.com/dotnet/runtime/issues/44686#issuecomment-733797994
    // We don't know if the IPv6 stack is functional.
    internal class HttpClientFactory
    {
        public static HttpClient CreateHttpClient()
        {
            var client = new HttpClient(new SocketsHttpHandler
            {
                ConnectCallback = IPv4ConnectAsync
            });

            var assemblyVersion = typeof(HttpClientFactory).Assembly.GetName().Version?.ToString(fieldCount: 3);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Trebuchet", assemblyVersion));

            return client;
        }

        private static async ValueTask<Stream> IPv4ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
        {
            // By default, we create dual-mode sockets:
            // Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.NoDelay = true;

            try
            {
                await socket.ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    }
}