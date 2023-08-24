using SteamKit2.GC.Artifact.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TrebuchetLib
{
    public class Rcon : IRcon
    {
        private CancellationTokenSource? _cts;
        private IPEndPoint _endpoint;
        private int _id;
        private int _keepAlive;
        private object _lock = new object();
        private string _password;
        private BlockingCollection<RconPacket> _queue = new BlockingCollection<RconPacket>(new ConcurrentQueue<RconPacket>());
        private int _timeout;

        public Rcon(IPEndPoint endpoint, string password, int timeout = 5000, int keepAlive = 0)
        {
            _endpoint = endpoint;
            _timeout = timeout;
            _password = password;
            _keepAlive = keepAlive;
        }

        public event EventHandler<RconEventArgs>? RconResponded;

        public int Send(string data)
        {
            if (data.Length > 4086)
                throw new ArgumentOutOfRangeException("data", "Data must be less than 4086 characters.");

            var packet = new RconPacket
            {
                id = _id++,
                type = RconPacketType.SERVERDATA_EXECCOMMAND,
                body = Encoding.ASCII.GetBytes(data)
            };

            _queue.Add(packet);
            StartRconJob();

            return packet.id;
        }

        public IEnumerable<int> Send(IEnumerable<string> data)
        {
            foreach (string s in data)
                yield return Send(s);
        }

        protected void OnRconResponded(RconEventArgs e)
        {
            RconResponded?.Invoke(this, e);
        }

        protected void OnRconResponded(Exception ex)
        {
            if (ex is not OperationCanceledException)
                RconResponded?.Invoke(this, new RconEventArgs(-1, string.Empty, ex));
        }

        private RconPacket BuildAuthPacket()
        {
            var packet = new RconPacket
            {
                id = _id++,
                type = RconPacketType.SERVERDATA_AUTH,
                body = Encoding.ASCII.GetBytes(_password)
            };

            return packet;
        }

        private void EvaluateRconResponse(byte[] data)
        {
            if (data.Length < 10 || !data[^2..].SequenceEqual(new byte[] { 0, 0 }))
                throw new Exception("Invalid packet received.");

            int id = RconPacket.GetAutoInt32(data[..4]);
            RconPacketType type = (RconPacketType)RconPacket.GetAutoInt32(data[4..8]);
            string response = Encoding.ASCII.GetString(data[8..^2]);

            if (type == RconPacketType.SERVERDATA_AUTH_RESPONSE)
                if (id == -1)
                    throw new Exception("Authentication failed.");
                else
                    OnRconResponded(new RconEventArgs(id, "Authentication successful"));
            else if (type == RconPacketType.SERVERDATA_RESPONSE_VALUE)
                OnRconResponded(new RconEventArgs(id, response));
        }

        private async Task RconThread(CancellationToken ct)
        {
            using var client = new TcpClient();
            client.SendTimeout = _timeout;
            client.ReceiveTimeout = _timeout;
            try
            {
                await client.ConnectAsync(_endpoint, ct);
                if (!client.Connected)
                    throw new Exception("Failed to connect to server.");

                await RconThreadLogin(client.GetStream(), ct);
                ct.ThrowIfCancellationRequested();

                await RconThreadLoop(ct, client);
            }
            catch (Exception ex)
            {
                OnRconResponded(ex);
            }
            finally
            {
                StopRconJob();
            }
        }

        private async Task RconThreadLogin(NetworkStream stream, CancellationToken ct)
        {
            var auth = BuildAuthPacket();
            await stream.WriteAsync(auth.GetRconRequest(), 0, auth.Length, ct);
            ct.ThrowIfCancellationRequested();
            await RconThreadReceive(stream, ct);
        }

        private async Task RconThreadLoop(CancellationToken ct, TcpClient client)
        {
            DateTime lastKeepAlive = DateTime.Now;
            while (!ct.IsCancellationRequested)
            {
                if (_queue.Count != 0)
                {
                    lastKeepAlive = DateTime.Now;
                    await RconThreadSend(client, ct);
                }

                if (_keepAlive == 0 || (DateTime.Now - lastKeepAlive).TotalMilliseconds > _keepAlive)
                    return;
                await Task.Delay(100);
            }
        }

        private async Task RconThreadReceive(NetworkStream stream, CancellationToken ct)
        {
            byte[] length = new byte[4];
            await stream.ReadAsync(length, 0, 4, ct);
            ct.ThrowIfCancellationRequested();
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(length);
            int size = BitConverter.ToInt32(length, 0);
            if (size < 10 || size > 4096) // rcon message (called packet) can never be larger than 4096 bytes and never smaller than 10, this is by design in the protocol definition
                throw new Exception("Invalid packet received.");

            byte[] rconpacket = new byte[size];
            await stream.ReadAsync(rconpacket, 0, size, ct);
            ct.ThrowIfCancellationRequested();
            EvaluateRconResponse(rconpacket);
        }

        private async Task RconThreadSend(TcpClient client, CancellationToken ct)
        {
            using var stream = client.GetStream();
            foreach (var packet in _queue.GetConsumingEnumerable())
            {
                await stream.WriteAsync(packet.GetRconRequest(), 0, packet.Length, ct);
                ct.ThrowIfCancellationRequested();
            }
            await stream.FlushAsync(ct);
            await RconThreadReceive(stream, ct);
        }

        private void StartRconJob()
        {
            lock (_lock)
            {
                if (_cts != null) return;
                _cts = new CancellationTokenSource();
                Task.Run(() => RconThread(_cts.Token), _cts.Token);
            }
        }

        private void StopRconJob()
        {
            lock (_lock)
            {
                if (_cts == null) return;
                _cts.Cancel();
                _cts = null;
            }
        }

        // Message contains int32 length, int32 request id, int32 type, string body null terminated, string null terminator
        // It is important to note that the length is not including itself, so the minimum length is 10 bytes
        // The maximum length is 4096 bytes, this is by design in the protocol definition
        // https://developer.valvesoftware.com/wiki/Source_RCON_Protocol
        private class RconPacket
        {
            public byte[] body = new byte[0];
            public int id;
            public RconPacketType type;

            public int Length => body.Length + 10;

            public static byte[] GetAutoBytes(int integer)
            {
                var bytes = BitConverter.GetBytes(integer);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                return bytes;
            }

            public static int GetAutoInt32(byte[] data)
            {
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(data);
                return BitConverter.ToInt32(data);
            }

            public byte[] GetRconRequest()
            {
                var packet = new List<byte>();
                packet.AddRange(GetAutoBytes(Length));
                packet.AddRange(GetAutoBytes(id));
                packet.AddRange(GetAutoBytes((int)type));
                packet.AddRange(body);
                packet.AddRange(new byte[] { 0, 0 });

                return packet.ToArray();
            }
        }
    }

    public class RconEventArgs
    {
        public RconEventArgs(int id, string response, Exception? exception = null)
        {
            Id = id;
            Response = response;
            Exception = exception;
        }

        public Exception? Exception { get; }

        public int Id { get; }

        public string Response { get; }
    }
}