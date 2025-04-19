using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
using tot_lib;

namespace TrebuchetLib
{
    public class Rcon : IRcon, IDisposable
    {
        public Rcon(IPEndPoint endpoint, string password)
        {
            EndPoint = endpoint;
            _password = password;
        }
        
        private readonly Queue<RconPacket> _rconQueue = [];
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly string _password;
        private TcpClient? _client;
        private CancellationTokenSource? _timeoutCts;
        private int _id;
        
        public IPEndPoint EndPoint { get; }
        
        /// <summary>
        /// Timeout duration for each RCon requests, in seconds
        /// </summary>
        public int Timeout { get; init; } = 15;

        /// <summary>
        /// Keep alive duration for the active RCon duration, in seconds
        /// </summary>
        public int KeepAlive { get; init; } = 300;

        public event AsyncEventHandler<RconEventArgs>? RconResponded;

        public void Dispose()
        {
            _client?.Close();
            _client?.Dispose();
            _client = null;
        }

        public async Task<int> Send(string data, CancellationToken token)
        {
            if (data.Length > 4086)
                throw new ArgumentOutOfRangeException(nameof(data), "Data must be less than 4086 characters.");

            var packet = new RconPacket
            {
                Id = _id++,
                Type = RconPacketType.SERVERDATA_EXECCOMMAND,
                Body = data
            };

            await _semaphore.WaitAsync(token);
            await Connect(token);
            await RConSend(_client, packet, token);
            _semaphore.Release();
            AutoDisconnect();
            return packet.Id;
        }

        public int QueueData(string data)
        {
            if (data.Length > 4086)
                throw new ArgumentOutOfRangeException(nameof(data), "Data must be less than 4086 characters.");

            var packet = new RconPacket
            {
                Id = _id++,
                Type = RconPacketType.SERVERDATA_EXECCOMMAND,
                Body = data
            };
            _rconQueue.Enqueue(packet);
            return packet.Id;
        }

        public async Task FlushQueue(CancellationToken token)
        {
            if (_rconQueue.Count == 0) return;
            
            await _semaphore.WaitAsync(token);
            await Connect(token);
            while (_rconQueue.Count > 0)
            {
                var packet = _rconQueue.Dequeue();
                await RConSend(_client, packet, token);
            }
            _semaphore.Release();
            AutoDisconnect();
        }

        public async Task<List<int>> Send(IEnumerable<string> data, CancellationToken ct)
        {
            List<int> ids = [];
            foreach (var d in data)
                ids.Add(QueueData(d));
            await FlushQueue(ct);
            return ids;
        }

        
        [MemberNotNull("_client")]
        public async Task Connect(CancellationToken token)
        {
            if (_client is not null && _client.Connected) return;
            if (_client is not null)
                await Disconnect();
            _client = new();
            _client.ReceiveTimeout = Timeout * 1000;
            _client.SendTimeout = Timeout * 1000;
            await _client.ConnectAsync(EndPoint, token);
            await RConLogin(_password, _client.GetStream(), token);
        }

        public async Task Disconnect()
        {
            await _semaphore.WaitAsync();
            _client?.Close(); 
            _client?.Dispose();
            _client = null;
            _semaphore.Release();
        }

        protected async Task OnRconResponded(RconEventArgs e)
        {
            if(RconResponded is not null)
                await RconResponded.Invoke(this, e);
        }

        protected async Task OnRconResponded(Exception ex)
        {
            if (ex is not OperationCanceledException)
                if(RconResponded is not null)
                    await RconResponded.Invoke(this, new RconEventArgs(string.Empty, ex));
        }

        private void AutoDisconnect()
        {
            Task.Run(AutoDisconnectTask);
        }
 
        private async Task AutoDisconnectTask()
        {
            try
            {
                await _semaphore.WaitAsync();
                if (_timeoutCts is not null)
                    await _timeoutCts.CancelAsync();
                _timeoutCts = new();
                var ct = _timeoutCts.Token;
                _semaphore.Release();

                await Task.Delay(KeepAlive * 1000, ct);
                await Disconnect();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private RconPacket BuildAuthPacket(string password)
        {
            var packet = new RconPacket
            {
                Id = ++_id,
                Type = RconPacketType.SERVERDATA_AUTH,
                Body = password
            };

            return packet;
        }

        private async Task<RconEventArgs> EvaluateRconResponse(BinaryReader br)
        {
            return await Task.Run(() =>
            {
                try
                {
                    int size = br.ReadInt32();
                    if (size < 10 ||
                        size > 4096) // rcon message (called packet) can never be larger than 4096 bytes and never smaller than 10, this is by design in the protocol definition
                        throw new Exception($"Invalid packet received (size=10<{size}<4096)");

                    var id = br.ReadInt32();
                    _ = (RconPacketType)br.ReadInt32();
                    var response = br.ReadNullTerminatedString();
                    var code = br.ReadByte();
                    if (code != 0)
                        throw new Exception($"Invalid packet received ({code})");

                    if (id == -1)
                        throw new Exception("Authentication failed.");

                    return new RconEventArgs(response ?? string.Empty);
                }
                catch(Exception ex)
                {
                    return new RconEventArgs(ex.Message, ex);
                }
            });
        }

        private async Task RConLogin(string password, NetworkStream stream, CancellationToken ct)
        {
            var auth = BuildAuthPacket(password);
            using var bw = new BinaryWriter(stream, Encoding.ASCII, true);
            var result = await auth.WriteBinary(bw);
            if (!string.IsNullOrEmpty(result.Response))
                await OnRconResponded(result);
            ct.ThrowIfCancellationRequested();
            await RConReceive(stream, ct);
        }

        private async Task RConReceive(NetworkStream stream, CancellationToken ct)
        {
            using var br = new BinaryReader(stream, Encoding.ASCII, true);
            ct.ThrowIfCancellationRequested();
            var response = await EvaluateRconResponse(br);
            if(!string.IsNullOrEmpty(response.Response))
                await OnRconResponded(response);
        }

        private async Task RConSend(TcpClient client, RconPacket packet, CancellationToken ct)
        {
            using var bw = new BinaryWriter(client.GetStream(), Encoding.ASCII, true);
            var result = await packet.WriteBinary(bw);
            if (!string.IsNullOrEmpty(result.Response))
                await OnRconResponded(result);
            ct.ThrowIfCancellationRequested();
            await RConReceive(client.GetStream(), ct);
        }

        // Message contains int32 length, int32 request id, int32 type, string body null terminated, string null terminator
        // It is important to note that the length is not including itself, so the minimum length is 10 bytes
        // The maximum length is 4096 bytes, this is by design in the protocol definition
        // https://developer.valvesoftware.com/wiki/Source_RCON_Protocol
        private class RconPacket
        {
            public string Body { get; init; } = string.Empty;
            public int Id { get; init; }
            public RconPacketType Type { get; init; }

            public int Length => Body.Length + 10;

            public async Task<RconEventArgs> WriteBinary(BinaryWriter bw)
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        bw.Write(Length);
                        bw.Write(Id);
                        bw.Write((int)Type);
                        bw.WriteNullTerminatedString(Body);
                        bw.Write((byte)0);
                        return new RconEventArgs(string.Empty);
                    }
                    catch(Exception ex)
                    {
                        return new RconEventArgs(ex.Message, ex);
                    }
                });
            }
        }
    }

    public class RconEventArgs
    {
        public RconEventArgs(string response, Exception? exception = null)
        {
            Response = response;
            Exception = exception;
        }

        public Exception? Exception { get; }

        public string Response { get; }
    }
}