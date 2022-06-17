using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Caching;

namespace SimpleUdp
{
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

        /// <summary>
        /// Events.
        /// </summary>
        public SimpleUdpEvents Events
        {
            get
            {
                return _Events;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Events));
                _Events = value;
            }
        }
        
        public IPEndPoint LocalIPEndPoint => _Socket.LocalEndPoint as IPEndPoint;

        public bool Disposed { get; private set; }
        public bool Started { get; private set; }

        #endregion

        #region Private-Members
        
        private Socket _Socket = null;
        private int _MaxDatagramSize = 65507;
        private EndPoint _Endpoint = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback _ReceiveCallback = null;

        private LRUCache<string, Socket> _RemoteSockets = new LRUCache<string, Socket>(100, 1, null, false);
         
        private SemaphoreSlim _SendLock = new SemaphoreSlim(1, 1);

        private SimpleUdpEvents _Events = new SimpleUdpEvents();

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
        /// If you wish to also receive datagrams, set the 'DatagramReceived' event and call 'Start()'.
        /// </summary>
        /// <param name="hostname">Local hostname.</param>
        /// <param name="port">Local port number.</param>
        public UdpEndpoint(string hostname, int port = 0) : this(new UdpClient(hostname, port))
        {
        }

        /// <summary>
        /// Instantiate the UDP endpoint.
        /// If you wish to also receive datagrams, set the 'DatagramReceived' event and call 'Start()'.
        /// </summary>
        /// <param name="port">Local port number.</param>
        public UdpEndpoint(int port = 0) : this(new UdpClient(port))
        {
        }

        /// <summary>
        /// Use existing Socket from UdpClient.
        /// If you wish to also receive datagrams, set the 'DatagramReceived' event and call 'Start()'.
        /// </summary>
        public UdpEndpoint(UdpClient udpClient) : this(udpClient.Client)
        {
            _Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
        }

        /// <summary>
        /// Use existing Socket.
        /// If you wish to also receive datagrams, set the 'DatagramReceived' event and call 'Start()'.
        /// </summary>
        public UdpEndpoint(Socket socket)
        {
            _Socket = socket;
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
            if (Disposed) return;

            if (disposing)
            {
                Disposed = true;
                try
                {
                    _Socket.Dispose();
                } finally
                {
                    Started = false;
                }
            }
        }


        /// <summary>
        /// Start the UDP listener to receive datagrams.  Before calling this method, set the 'DatagramReceived' event.
        /// </summary>
        /// <exception cref="InvalidOperationException">Already started</exception>
        public void Start()
        {
            if (Started) throw new InvalidOperationException("Already started");
            Started = true;

            State state = new State(_MaxDatagramSize);

            _Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            _Events.HandleStarted(this);

            _Socket.BeginReceiveFrom(state.Buffer, 0, _MaxDatagramSize, SocketFlags.None, ref _Endpoint, _ReceiveCallback = (ar) =>
            {
                try
                {
                    State so = (State)ar.AsyncState;
                    _Socket.BeginReceiveFrom(so.Buffer, 0, _MaxDatagramSize, SocketFlags.None, ref _Endpoint, _ReceiveCallback, so);
                    int bytes = _Socket.EndReceiveFrom(ar, ref _Endpoint);

                    string ipPort = _Endpoint.ToString();
                    string ip = null;
                    int port = 0;
                    Common.ParseIpPort(ipPort, out ip, out port);

                    if (!_RemoteSockets.Contains(ipPort))
                    {
                        _RemoteSockets.AddReplace(ipPort, _Socket);
                        EndpointDetected?.Invoke(this, new EndpointMetadata(ip, port));
                    }

                    if (bytes == so.Buffer.Length)
                    {
                        DatagramReceived?.Invoke(this, new Datagram(ip, port, so.Buffer));
                    }
                    else
                    {
                        byte[] buffer = new byte[bytes];
                        Buffer.BlockCopy(so.Buffer, 0, buffer, 0, bytes);
                        DatagramReceived?.Invoke(this, new Datagram(ip, port, buffer));
                    }
                }
                catch (Exception)
                {
                    _Events.HandleStopped(this);
                }
            }, state);            
        }

        /// <summary>
        /// Stop the UDP listener.
        /// </summary>
        public void Stop()
        {
            try
            {
                _Socket.Close();
            } finally
            {
                Started = false;
            }
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
            byte[] data = Encoding.UTF8.GetBytes(text);
            SendInternal(ip, port, data, 0, data.Length, ttl); 
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
            SendInternal(ip, port, data, 0, data.Length, ttl);
        }

        /// <summary>
        /// Send a datagram to the specific IP address and UDP port with specific offset and size.
        /// This will throw a SocketException if the report UDP port is unreachable.
        /// </summary>
        /// <param name="ip">IP address.</param>
        /// <param name="port">Port.</param>
        /// <param name="data">Bytes.</param>
        /// <param name="ttl">Time to live, the maximum number of routers the packet is allowed to traverse.  Minimum is 0, default is 64.</param>
        public void SendOffset(string ip, int port, byte[] data, int offset, int size, short ttl = 64)
        {
            SendInternal(ip, port, data, offset, size, ttl);
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
            byte[] data = Encoding.UTF8.GetBytes(text);
            await SendInternalAsync(ip, port, data, 0, data.Length, ttl).ConfigureAwait(false);
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
            await SendInternalAsync(ip, port, data, 0, data.Length, ttl).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a datagram asynchronously to the specific IP address and UDP port with specific offset and size.
        /// This will throw a SocketException if the report UDP port is unreachable.
        /// </summary>
        /// <param name="ip">IP address.</param>
        /// <param name="port">Port.</param>
        /// <param name="data">Bytes.</param> 
        /// <param name="ttl">Time to live, the maximum number of routers the packet is allowed to traverse.  Minimum is 0, default is 64.</param>
        public async Task SendOffsetAsync(string ip, int port, byte[] data, int offset, int size, short ttl = 64)
        {
            await SendInternalAsync(ip, port, data, offset, size, ttl).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private void SendInternal(string ip, int port, byte[] data, int offset, int size, short ttl)
        {
            if (ttl < 0) throw new ArgumentOutOfRangeException(nameof(ttl));
            if (data == null || data.Length == 0) throw new ArgumentNullException(nameof(data));
            if (data.Length > _MaxDatagramSize) throw new ArgumentException("Data exceed maximum datagram size (" + data.Length + " data bytes, " + _MaxDatagramSize + " bytes).");

            _SendLock.Wait();

            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), port);

            try
            {
                _Socket.Ttl = ttl;
                _Socket.SendTo(data, offset, size, SocketFlags.None, ipe);
            }
            finally
            {
                _SendLock.Release();
            }
        }

        private async Task SendInternalAsync(string ip, int port, byte[] data, int offset, int size, short ttl)
        {
            if (ttl < 0) throw new ArgumentOutOfRangeException(nameof(ttl));
            if (data == null || data.Length == 0) throw new ArgumentNullException(nameof(data));
            if (data.Length > _MaxDatagramSize) throw new ArgumentException("Data exceed maximum datagram size (" + data.Length + " data bytes, " + _MaxDatagramSize + " bytes).");

            await _SendLock.WaitAsync();

            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), port);

            try
            {
                _Socket.Ttl = ttl;
                await _Socket.SendToAsync(new ArraySegment<byte>(data, offset, size), SocketFlags.None, ipe).ConfigureAwait(false);
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