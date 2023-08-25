using SteamKit2.GC.Artifact.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Trebuchet;

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
        private ConcurrentQueue<RconPacket> _queue = new ConcurrentQueue<RconPacket>();
        private int _timeout;

        public Rcon(IPEndPoint endpoint, string password, int timeout = 5000, int keepAlive = 0)
        {
            _endpoint = endpoint;
            _timeout = timeout;
            _password = password;
            _keepAlive = keepAlive;
        }

        public event EventHandler<RconEventArgs>? RconResponded;

        public event EventHandler<RconEventArgs>? RconSent;

        public void Cancel()
        {
            lock (_lock)
            {
                _cts?.Cancel();
            }
        }

        public int Send(string data)
        {
            if (data.Length > 4086)
                throw new ArgumentOutOfRangeException("data", "Data must be less than 4086 characters.");

            var packet = new RconPacket
            {
                id = _id++,
                type = RconPacketType.SERVERDATA_EXECCOMMAND,
                body = data
            };

            _queue.Enqueue(packet);
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
                RconResponded?.Invoke(this, new RconEventArgs(string.Empty, ex));
        }

        protected void OnRconSent(RconEventArgs e)
        {
            RconSent?.Invoke(this, e);
        }

        private RconPacket BuildAuthPacket()
        {
            var packet = new RconPacket
            {
                id = ++_id,
                type = RconPacketType.SERVERDATA_AUTH,
                body = _password
            };

            return packet;
        }

        private string? EvaluateRconResponse(BinaryReader br)
        {
            int size = br.ReadInt32();
            if (size < 10 || size > 4096) // rcon message (called packet) can never be larger than 4096 bytes and never smaller than 10, this is by design in the protocol definition
                throw new Exception("Invalid packet received.");

            int id = br.ReadInt32();
            RconPacketType type = (RconPacketType)br.ReadInt32();
            string? response = br.ReadNullTerminatedString();
            if (br.ReadByte() != 0)
                throw new Exception("Invalid packet received.");

            if (id == -1)
                throw new Exception("Authentication failed.");

            return response;
        }

        private async Task RconThread(CancellationToken ct)
        {
            var client = new TcpClient();
            client.SendTimeout = _timeout;
            client.ReceiveTimeout = _timeout;
            try
            {
                client.Connect(_endpoint);
                if (!client.Connected)
                    throw new Exception("Failed to connect to server.");

                RconThreadLogin(client.GetStream(), ct);
                ct.ThrowIfCancellationRequested();

                await RconThreadLoop(ct, client);
            }
            catch (Exception ex)
            {
                OnRconResponded(ex);
            }
            finally
            {
                client.Dispose();
                StopRconJob();
            }
        }

        private void RconThreadLogin(NetworkStream stream, CancellationToken ct)
        {
            var auth = BuildAuthPacket();
            using var bw = new BinaryWriter(stream, Encoding.ASCII, true);
            auth.WriteBinary(bw);
            ct.ThrowIfCancellationRequested();
            RconThreadReceive(stream, ct);
        }

        private async Task RconThreadLoop(CancellationToken ct, TcpClient client)
        {
            DateTime lastKeepAlive = DateTime.Now;
            while (!ct.IsCancellationRequested && client.Connected)
            {
                if (_queue.Count != 0)
                {
                    lastKeepAlive = DateTime.Now;
                    RconThreadSend(client, ct);
                }

                if (_keepAlive == 0 || (DateTime.Now - lastKeepAlive).TotalMilliseconds > _keepAlive)
                    return;
                await Task.Delay(100);
            }
        }

        private void RconThreadReceive(NetworkStream stream, CancellationToken ct)
        {
            using var br = new BinaryReader(stream, Encoding.ASCII, true);
            ct.ThrowIfCancellationRequested();
            string body = EvaluateRconResponse(br) ?? string.Empty;
            OnRconResponded(new RconEventArgs(body));
        }

        private void RconThreadSend(TcpClient client, CancellationToken ct)
        {
            using var bw = new BinaryWriter(client.GetStream(), Encoding.ASCII, true);
            while (_queue.TryDequeue(out var packet))
            {
                packet.WriteBinary(bw);
                if (!string.IsNullOrEmpty(packet.body))
                    OnRconSent(new RconEventArgs(packet.body));
                ct.ThrowIfCancellationRequested();
            }
            RconThreadReceive(client.GetStream(), ct);
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
                _cts?.Cancel();
                _cts = null;
            }
        }

        private void WriteEmpty(BinaryWriter bw, int id)
        {
            bw.Write(10);
            bw.Write(id);
            bw.Write((int)RconPacketType.SERVERDATA_RESPONSE_VALUE);
            bw.Write((byte)0);
            bw.Write((byte)0);
        }

        // Message contains int32 length, int32 request id, int32 type, string body null terminated, string null terminator
        // It is important to note that the length is not including itself, so the minimum length is 10 bytes
        // The maximum length is 4096 bytes, this is by design in the protocol definition
        // https://developer.valvesoftware.com/wiki/Source_RCON_Protocol
        private class RconPacket
        {
            public string body = string.Empty;
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

            public void WriteBinary(BinaryWriter bw)
            {
                bw.Write(Length);
                bw.Write(id);
                bw.Write((int)type);
                bw.WriteNullTerminatedString(body);
                bw.Write((byte)0);
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