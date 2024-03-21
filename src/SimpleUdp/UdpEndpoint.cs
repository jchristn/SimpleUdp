namespace SimpleUdp
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Caching;

    /// <summary>
    /// UDP endpoint, both client and server.
    /// </summary>
    public class UdpEndpoint : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Event to fire when a new endpoint is detected.
        /// </summary>
        public event EventHandler<EndpointMetadata> EndpointDetected;

        /// <summary>
        /// Event to fire when a datagram is received.
        /// </summary>
        public event EventHandler<Datagram> DatagramReceived;

        /// <summary>
        /// Event to fire when the server is stopped.
        /// </summary>
        public event EventHandler ServerStopped;

        /// <summary>
        /// Retrieve a list of (up to) the 100 most recently seen endpoints.
        /// </summary>
        public List<string> Endpoints
        {
            get
            {
                return _RemoteSockets.GetKeys();
            }
        }

        /// <summary>
        /// Maximum datagram size, must be greater than zero and less than or equal to 65507.
        /// </summary>
        public int MaxDatagramSize
        {
            get
            {
                return _MaxDatagramSize;
            }
            set
            {
                if (value < 1 || value > 65507) throw new ArgumentException("MaxDatagramSize must be greater than zero and less than or equal to 65507.");
                _MaxDatagramSize = value;
            }
        }

        #endregion

        #region Private-Members

        private bool _Disposed = false;
        private string _Ip = null;
        private int _Port = 0;
        private IPAddress _IPAddress;
        private Socket _Socket = null;
        private int _MaxDatagramSize = 65507;
        private EndPoint _Endpoint = new IPEndPoint(IPAddress.Any, 0);
        private UdpClient _UdpClient = null;
        private AsyncCallback _ReceiveCallback = null;

        private LRUCache<string, Socket> _RemoteSockets = new LRUCache<string, Socket>(100, 1);
         
        private SemaphoreSlim _SendLock = new SemaphoreSlim(1, 1);

        #endregion

        #region Internal-Classes

        internal class State
        {
            internal State(int bufferSize)
            {
                Buffer = new byte[bufferSize];
            }

            internal byte[] Buffer = null;
        }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the UDP endpoint.
        /// <para>If you wish to also receive datagrams, set the 'DatagramReceived' event and call 'StartServer()'.</para>
        /// </summary>
        /// <param name="ip">IP address on which to listen.</param>
        /// <param name="port">Port number on which to listen.</param>
        public UdpEndpoint(string ip, int port)
        {
            if (port < 0 || port > 65535) throw new ArgumentException("Port must be greater than or equal to zero and less than or equal to 65535.");
            _Ip = ip;
            _Port = port;

            if (String.IsNullOrEmpty(ip)) _IPAddress = IPAddress.Any;
            else _IPAddress = IPAddress.Parse(ip);

            _UdpClient = new UdpClient();
            _UdpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _UdpClient.ExclusiveAddressUse = false;

            State state = new State(_MaxDatagramSize);

            _Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            _Socket.Bind(new IPEndPoint(_IPAddress, _Port));

            _Socket.BeginReceiveFrom(state.Buffer, 0, _MaxDatagramSize, SocketFlags.None, ref _Endpoint, _ReceiveCallback = (ar) =>
            {
                try
                {
                    State so = (State)ar.AsyncState;
                    int bytes = _Socket.EndReceiveFrom(ar, ref _Endpoint);

                    string senderIpPort = _Endpoint.ToString();
                    string senderIp = null;
                    int senderPort = 0;
                    Common.ParseIpPort(senderIpPort, out senderIp, out senderPort);

                    if (!_RemoteSockets.Contains(senderIpPort))
                    {
                        _RemoteSockets.AddReplace(senderIpPort, _Socket);
                        EndpointDetected?.Invoke(this, new EndpointMetadata(senderIp, senderPort));
                    }

                    if (bytes == so.Buffer.Length)
                    {
                        DatagramReceived?.Invoke(this, new Datagram(senderIp, senderPort, so.Buffer));
                    }
                    else
                    {
                        byte[] buffer = new byte[bytes];
                        Buffer.BlockCopy(so.Buffer, 0, buffer, 0, bytes);
                        DatagramReceived?.Invoke(this, new Datagram(senderIp, senderPort, buffer));
                    }

                    _Socket.BeginReceiveFrom(so.Buffer, 0, _MaxDatagramSize, SocketFlags.None, ref _Endpoint, _ReceiveCallback, so);
                }
                catch (Exception)
                {
                    ServerStopped?.Invoke(this, EventArgs.Empty);
                }
            }, state);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed) return;

            if (disposing)
            {
                _UdpClient?.Dispose();

                if (_Socket != null)
                {
                    _Socket.Close();
                }
            }

            _Disposed = true;
        }

        /// <summary>
        /// Send a datagram to the specific IP address and UDP port.
        /// This will throw a SocketException if the report UDP port is unreachable.
        /// </summary>
        /// <param name="ip">IP address.</param>
        /// <param name="port">Port.</param>
        /// <param name="text">Text to send.</param>
        /// <param name="ttl">Time to live, the maximum number of routers the packet is allowed to traverse.  Minimum is 0, default is 64.</param>
        public void Send(string ip, int port, string text, short ttl = 64)
        {
            if (String.IsNullOrEmpty(ip)) throw new ArgumentNullException(nameof(ip));
            if (port < 0 || port > 65535) throw new ArgumentException("Port is out of range; must be greater than or equal to zero and less than or equal to 65535.");
            if (String.IsNullOrEmpty(text)) throw new ArgumentNullException(nameof(text));
            if (ttl < 0) throw new ArgumentOutOfRangeException(nameof(ttl));
            byte[] data = Encoding.UTF8.GetBytes(text);
            if (data.Length > _MaxDatagramSize) throw new ArgumentException("Data exceed maximum datagram size (" + data.Length + " data bytes, " + _MaxDatagramSize + " bytes).");
            SendInternal(ip, port, data, ttl); 
        }

        /// <summary>
        /// Send a datagram to the specific IP address and UDP port.
        /// This will throw a SocketException if the report UDP port is unreachable.
        /// </summary>
        /// <param name="ip">IP address.</param>
        /// <param name="port">Port.</param>
        /// <param name="data">Bytes.</param>
        /// <param name="ttl">Time to live, the maximum number of routers the packet is allowed to traverse.  Minimum is 0, default is 64.</param>
        public void Send(string ip, int port, byte[] data, short ttl = 64)
        {
            if (String.IsNullOrEmpty(ip)) throw new ArgumentNullException(nameof(ip));
            if (port < 0 || port > 65535) throw new ArgumentException("Port is out of range; must be greater than or equal to zero and less than or equal to 65535.");
            if (data == null || data.Length < 1) throw new ArgumentNullException(nameof(data));
            if (data.Length > _MaxDatagramSize) throw new ArgumentException("Data exceed maximum datagram size (" + data.Length + " data bytes, " + _MaxDatagramSize + " bytes).");
            if (ttl < 0) throw new ArgumentOutOfRangeException(nameof(ttl));
            SendInternal(ip, port, data, ttl);
        }

        /// <summary>
        /// Send a datagram asynchronously to the specific IP address and UDP port.
        /// This will throw a SocketException if the report UDP port is unreachable.
        /// </summary>
        /// <param name="ip">IP address.</param>
        /// <param name="port">Port.</param>
        /// <param name="text">Text to send.</param>
        /// <param name="ttl">Time to live, the maximum number of routers the packet is allowed to traverse.  Minimum is 0, default is 64.</param>
        public async Task SendAsync(string ip, int port, string text, short ttl = 64)
        {
            if (String.IsNullOrEmpty(ip)) throw new ArgumentNullException(nameof(ip));
            if (port < 0 || port > 65535) throw new ArgumentException("Port is out of range; must be greater than or equal to zero and less than or equal to 65535.");
            if (String.IsNullOrEmpty(text)) throw new ArgumentNullException(nameof(text));
            byte[] data = Encoding.UTF8.GetBytes(text);
            if (data.Length > _MaxDatagramSize) throw new ArgumentException("Data exceed maximum datagram size (" + data.Length + " data bytes, " + _MaxDatagramSize + " bytes).");
            if (ttl < 0) throw new ArgumentOutOfRangeException(nameof(ttl));
            await SendInternalAsync(ip, port, data, ttl).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a datagram asynchronously to the specific IP address and UDP port.
        /// This will throw a SocketException if the report UDP port is unreachable.
        /// </summary>
        /// <param name="ip">IP address.</param>
        /// <param name="port">Port.</param>
        /// <param name="data">Bytes.</param> 
        /// <param name="ttl">Time to live, the maximum number of routers the packet is allowed to traverse.  Minimum is 0, default is 64.</param>
        public async Task SendAsync(string ip, int port, byte[] data, short ttl = 64)
        {
            if (String.IsNullOrEmpty(ip)) throw new ArgumentNullException(nameof(ip));
            if (port < 0 || port > 65535) throw new ArgumentException("Port is out of range; must be greater than or equal to zero and less than or equal to 65535.");
            if (data == null || data.Length < 1) throw new ArgumentNullException(nameof(data));
            if (data.Length > _MaxDatagramSize) throw new ArgumentException("Data exceed maximum datagram size (" + data.Length + " data bytes, " + _MaxDatagramSize + " bytes).");
            if (ttl < 0) throw new ArgumentOutOfRangeException(nameof(ttl));
            await SendInternalAsync(ip, port, data, ttl).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private void SendInternal(string ip, int port, byte[] data, short ttl)
        {
            _SendLock.Wait();

            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), port);

            try
            {
                _UdpClient.Ttl = ttl;
                _UdpClient.Send(data, data.Length, ipe);
            }
            finally
            {
                _SendLock.Release();
            }
        }

        private async Task SendInternalAsync(string ip, int port, byte[] data, short ttl)
        {
            await _SendLock.WaitAsync();

            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), port);

            try
            {
                _UdpClient.Ttl = ttl;
                await _UdpClient.SendAsync(data, data.Length, ipe).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {

            }
            catch (OperationCanceledException)
            {

            }
            finally
            {
                _SendLock.Release();
            }
        }

        #endregion
    }
}
