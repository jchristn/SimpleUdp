namespace SimpleUdp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Caching;

    /// <summary>
    /// UDP endpoint, both client and server.
    /// <para>Emits metrics and traces through the BCL Meter and ActivitySource named in <see cref="SimpleUdpTelemetryNames"/>; emission is a near-zero-cost no-op when nothing is subscribed.</para>
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

        /// <summary>
        /// Gets or sets a value indicating whether this endpoint can send UDP broadcast packets.
        /// </summary>
        public bool EnableBroadcast
        {
            get
            {
                return _Socket.EnableBroadcast;
            }
            set
            {
                _Socket.EnableBroadcast = value;
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
        private AsyncCallback _ReceiveCallback = null;

        private static readonly int _RemoteSocketsCapacity = 100;
        private LRUCache<string, Socket> _RemoteSockets = new LRUCache<string, Socket>(_RemoteSocketsCapacity, 1);
        private long _RemoteSocketsCount = 0;
         
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
        /// <para>If you wish to receive datagrams, subscribe to the 'DatagramReceived' event.</para>
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

            State state = new State(_MaxDatagramSize);

            Activity startActivity = SimpleUdpTelemetry.StartActivity(SimpleUdpTelemetryNames.SpanStart, ActivityKind.Internal);
            startActivity?.SetTag(SimpleUdpTelemetryNames.AttrNetworkLocalAddress, _IPAddress.ToString());
            startActivity?.SetTag(SimpleUdpTelemetryNames.AttrNetworkLocalPort, _Port);

            try
            {
                _Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                DisableUdpConnectionReset(_Socket);
                EnsureSendBufferFitsMaxDatagram(_Socket);
                _Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
                _Socket.Bind(new IPEndPoint(_IPAddress, _Port));

                _Socket.BeginReceiveFrom(state.Buffer, 0, _MaxDatagramSize, SocketFlags.None, ref _Endpoint, _ReceiveCallback = ReceiveCallback, state);
            }
            catch (Exception e)
            {
                SimpleUdpTelemetry.RecordEndpointStart(SimpleUdpTelemetryNames.OutcomeFailure, e);
                SimpleUdpTelemetry.SetFailure(startActivity, e);
                SimpleUdpTelemetry.StopActivity(startActivity);
                _Socket?.Dispose();
                throw;
            }

            if (startActivity != null && _Socket.LocalEndPoint is IPEndPoint boundEndpoint)
            {
                startActivity.SetTag(SimpleUdpTelemetryNames.AttrNetworkLocalPort, boundEndpoint.Port);
                startActivity.SetTag(SimpleUdpTelemetryNames.AttrMaxDatagramSize, _MaxDatagramSize);
            }

            SimpleUdpTelemetry.RecordEndpointStart(SimpleUdpTelemetryNames.OutcomeSuccess, null);
            SimpleUdpTelemetry.AddEndpointsActive(1);
            SimpleUdpTelemetry.AddRemoteEndpointsCapacity(_RemoteSocketsCapacity);
            SimpleUdpTelemetry.SetSuccess(startActivity);
            SimpleUdpTelemetry.StopActivity(startActivity);
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

            _Disposed = true;

            if (disposing)
            {
                if (_Socket != null)
                {
                    _Socket.Close();
                }
            }

            SimpleUdpTelemetry.AddEndpointsActive(-1);
            SimpleUdpTelemetry.AddRemoteEndpointsCapacity(-_RemoteSocketsCapacity);
            SimpleUdpTelemetry.AddRemoteEndpointsCached(-Interlocked.Exchange(ref _RemoteSocketsCount, 0));
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
            long start = Stopwatch.GetTimestamp();
            Activity activity = StartSendActivity(ip, port, ttl, SimpleUdpTelemetryNames.SendModeSync);
            byte[] data = null;

            try
            {
                if (String.IsNullOrEmpty(ip)) throw new ArgumentNullException(nameof(ip));
                if (port < 0 || port > 65535) throw new ArgumentException("Port is out of range; must be greater than or equal to zero and less than or equal to 65535.");
                if (String.IsNullOrEmpty(text)) throw new ArgumentNullException(nameof(text));
                if (ttl < 0) throw new ArgumentOutOfRangeException(nameof(ttl));
                data = Encoding.UTF8.GetBytes(text);
                if (data.Length > _MaxDatagramSize) throw new ArgumentException("Data exceed maximum datagram size (" + data.Length + " data bytes, " + _MaxDatagramSize + " bytes).");
                SendInternal(ip, port, data, ttl);
            }
            catch (Exception e)
            {
                CompleteSend(activity, SimpleUdpTelemetryNames.SendModeSync, SimpleUdpTelemetryNames.OutcomeFailure, start, data, e);
                throw;
            }

            CompleteSend(activity, SimpleUdpTelemetryNames.SendModeSync, SimpleUdpTelemetryNames.OutcomeSuccess, start, data, null);
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
            long start = Stopwatch.GetTimestamp();
            Activity activity = StartSendActivity(ip, port, ttl, SimpleUdpTelemetryNames.SendModeSync);

            try
            {
                if (String.IsNullOrEmpty(ip)) throw new ArgumentNullException(nameof(ip));
                if (port < 0 || port > 65535) throw new ArgumentException("Port is out of range; must be greater than or equal to zero and less than or equal to 65535.");
                if (data == null || data.Length < 1) throw new ArgumentNullException(nameof(data));
                if (data.Length > _MaxDatagramSize) throw new ArgumentException("Data exceed maximum datagram size (" + data.Length + " data bytes, " + _MaxDatagramSize + " bytes).");
                if (ttl < 0) throw new ArgumentOutOfRangeException(nameof(ttl));
                SendInternal(ip, port, data, ttl);
            }
            catch (Exception e)
            {
                CompleteSend(activity, SimpleUdpTelemetryNames.SendModeSync, SimpleUdpTelemetryNames.OutcomeFailure, start, data, e);
                throw;
            }

            CompleteSend(activity, SimpleUdpTelemetryNames.SendModeSync, SimpleUdpTelemetryNames.OutcomeSuccess, start, data, null);
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
            long start = Stopwatch.GetTimestamp();
            Activity activity = StartSendActivity(ip, port, ttl, SimpleUdpTelemetryNames.SendModeAsync);
            byte[] data = null;
            bool sent = false;

            try
            {
                if (String.IsNullOrEmpty(ip)) throw new ArgumentNullException(nameof(ip));
                if (port < 0 || port > 65535) throw new ArgumentException("Port is out of range; must be greater than or equal to zero and less than or equal to 65535.");
                if (String.IsNullOrEmpty(text)) throw new ArgumentNullException(nameof(text));
                data = Encoding.UTF8.GetBytes(text);
                if (data.Length > _MaxDatagramSize) throw new ArgumentException("Data exceed maximum datagram size (" + data.Length + " data bytes, " + _MaxDatagramSize + " bytes).");
                if (ttl < 0) throw new ArgumentOutOfRangeException(nameof(ttl));
                sent = await SendInternalAsync(ip, port, data, ttl).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                CompleteSend(activity, SimpleUdpTelemetryNames.SendModeAsync, SimpleUdpTelemetryNames.OutcomeFailure, start, data, e);
                throw;
            }

            CompleteSend(activity, SimpleUdpTelemetryNames.SendModeAsync, sent ? SimpleUdpTelemetryNames.OutcomeSuccess : SimpleUdpTelemetryNames.OutcomeCanceled, start, data, null);
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
            long start = Stopwatch.GetTimestamp();
            Activity activity = StartSendActivity(ip, port, ttl, SimpleUdpTelemetryNames.SendModeAsync);
            bool sent = false;

            try
            {
                if (String.IsNullOrEmpty(ip)) throw new ArgumentNullException(nameof(ip));
                if (port < 0 || port > 65535) throw new ArgumentException("Port is out of range; must be greater than or equal to zero and less than or equal to 65535.");
                if (data == null || data.Length < 1) throw new ArgumentNullException(nameof(data));
                if (data.Length > _MaxDatagramSize) throw new ArgumentException("Data exceed maximum datagram size (" + data.Length + " data bytes, " + _MaxDatagramSize + " bytes).");
                if (ttl < 0) throw new ArgumentOutOfRangeException(nameof(ttl));
                sent = await SendInternalAsync(ip, port, data, ttl).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                CompleteSend(activity, SimpleUdpTelemetryNames.SendModeAsync, SimpleUdpTelemetryNames.OutcomeFailure, start, data, e);
                throw;
            }

            CompleteSend(activity, SimpleUdpTelemetryNames.SendModeAsync, sent ? SimpleUdpTelemetryNames.OutcomeSuccess : SimpleUdpTelemetryNames.OutcomeCanceled, start, data, null);
        }

        #endregion

        #region Private-Methods

        private static void DisableUdpConnectionReset(Socket socket)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

            const int SioUdpConnReset = unchecked((int)0x9800000C);
            socket.IOControl((IOControlCode)SioUdpConnReset, new byte[] { 0 }, null);
        }

        private static void EnsureSendBufferFitsMaxDatagram(Socket socket)
        {
            // macOS defaults the UDP send buffer to net.inet.udp.maxdgram (9216), which rejects larger datagrams with EMSGSIZE.
            try
            {
                if (socket.SendBufferSize < 65535) socket.SendBufferSize = 65535;
            }
            catch (SocketException)
            {
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            long start = Stopwatch.GetTimestamp();
            Activity activity = SimpleUdpTelemetry.StartRootActivity(SimpleUdpTelemetryNames.SpanReceive, ActivityKind.Consumer);

            try
            {
                State so = (State)ar.AsyncState;
                int bytes = _Socket.EndReceiveFrom(ar, ref _Endpoint);

                string senderIpPort = _Endpoint.ToString();
                string senderIp = null;
                int senderPort = 0;
                Common.ParseIpPort(senderIpPort, out senderIp, out senderPort);

                int bytesToCopy = Math.Min(bytes, _MaxDatagramSize);
                bool truncated = bytesToCopy < bytes;
                bool isNew = !_RemoteSockets.Contains(senderIpPort);

                if (activity != null)
                {
                    activity.SetTag(SimpleUdpTelemetryNames.AttrNetworkPeerAddress, senderIp);
                    activity.SetTag(SimpleUdpTelemetryNames.AttrNetworkPeerPort, senderPort);
                    activity.SetTag(SimpleUdpTelemetryNames.AttrNetworkLocalPort, LocalPort());
                    activity.SetTag(SimpleUdpTelemetryNames.AttrDatagramSize, bytesToCopy);
                    activity.SetTag(SimpleUdpTelemetryNames.AttrNewEndpoint, isNew);
                    if (truncated) activity.SetTag(SimpleUdpTelemetryNames.AttrTruncated, true);
                }

                SimpleUdpTelemetry.RecordReceived(bytesToCopy, truncated);

                _RemoteSockets.AddReplace(senderIpPort, _Socket);
                if (!_Disposed) UpdateRemoteSocketsTelemetry(isNew);

                if (isNew)
                {
                    SimpleUdpTelemetry.RecordRemoteEndpointDetected();
                    OnEndpointDetected(new EndpointMetadata(senderIp, senderPort));
                }

                byte[] buffer = new byte[bytesToCopy];
                Buffer.BlockCopy(so.Buffer, 0, buffer, 0, bytesToCopy);
                OnDatagramReceived(new Datagram(senderIp, senderPort, buffer));

                SimpleUdpTelemetry.RecordReceiveDuration(SimpleUdpTelemetryNames.OutcomeSuccess, SimpleUdpTelemetry.ElapsedSeconds(start));
                SimpleUdpTelemetry.SetSuccess(activity);
                SimpleUdpTelemetry.StopActivity(activity);
                activity = null;

                _Socket.BeginReceiveFrom(so.Buffer, 0, _MaxDatagramSize, SocketFlags.None, ref _Endpoint, _ReceiveCallback, so);
            }
            catch (Exception e)
            {
                if (_Disposed || e is ObjectDisposedException)
                {
                    SimpleUdpTelemetry.RecordReceiveLoopStop(SimpleUdpTelemetryNames.StopReasonDisposed, null);
                    activity?.SetTag(SimpleUdpTelemetryNames.AttrStopReason, SimpleUdpTelemetryNames.StopReasonDisposed);
                }
                else
                {
                    SimpleUdpTelemetry.RecordReceiveLoopStop(SimpleUdpTelemetryNames.StopReasonError, e);
                    SimpleUdpTelemetry.RecordReceiveDuration(SimpleUdpTelemetryNames.OutcomeFailure, SimpleUdpTelemetry.ElapsedSeconds(start));
                    SimpleUdpTelemetry.SetFailure(activity, e);
                    activity?.SetTag(SimpleUdpTelemetryNames.AttrStopReason, SimpleUdpTelemetryNames.StopReasonError);
                }

                SimpleUdpTelemetry.StopActivity(activity);
                ServerStopped?.Invoke(this, EventArgs.Empty);
            }
        }

        private void UpdateRemoteSocketsTelemetry(bool isNew)
        {
            try
            {
                long count = _RemoteSockets.Count();
                long previous = Interlocked.Exchange(ref _RemoteSocketsCount, count);
                SimpleUdpTelemetry.AddRemoteEndpointsCached(count - previous);
                if (isNew) SimpleUdpTelemetry.RecordRemoteEndpointsEvicted(previous + 1 - count);
            }
            catch
            {
            }
        }

        private int LocalPort()
        {
            try
            {
                if (_Socket.LocalEndPoint is IPEndPoint local) return local.Port;
            }
            catch
            {
            }

            return _Port;
        }

        private Activity StartSendActivity(string ip, int port, short ttl, string mode)
        {
            Activity activity = SimpleUdpTelemetry.StartActivity(SimpleUdpTelemetryNames.SpanSend, ActivityKind.Client);
            if (activity == null) return null;

            try
            {
                activity.SetTag(SimpleUdpTelemetryNames.AttrNetworkPeerAddress, ip);
                activity.SetTag(SimpleUdpTelemetryNames.AttrNetworkPeerPort, port);
                activity.SetTag(SimpleUdpTelemetryNames.AttrNetworkLocalPort, LocalPort());
                activity.SetTag(SimpleUdpTelemetryNames.AttrSendMode, mode);
                activity.SetTag(SimpleUdpTelemetryNames.AttrTtl, (int)ttl);
                activity.SetTag(SimpleUdpTelemetryNames.AttrMaxDatagramSize, _MaxDatagramSize);
            }
            catch
            {
            }

            return activity;
        }

        private void CompleteSend(Activity activity, string mode, string outcome, long start, byte[] data, Exception e)
        {
            SimpleUdpTelemetry.RecordSend(mode, outcome, e, SimpleUdpTelemetry.ElapsedSeconds(start));

            if (outcome == SimpleUdpTelemetryNames.OutcomeSuccess && data != null)
            {
                SimpleUdpTelemetry.RecordSentBytes(data.Length);
            }

            if (activity != null)
            {
                if (data != null) activity.SetTag(SimpleUdpTelemetryNames.AttrDatagramSize, data.Length);
                activity.SetTag(SimpleUdpTelemetryNames.AttrOutcome, outcome);
                if (e != null) SimpleUdpTelemetry.SetFailure(activity, e);
                else if (outcome == SimpleUdpTelemetryNames.OutcomeSuccess) SimpleUdpTelemetry.SetSuccess(activity);
                SimpleUdpTelemetry.StopActivity(activity);
            }
        }

        private static void CompleteStage(Activity activity, string stage, string outcome, long start, Exception e)
        {
            SimpleUdpTelemetry.RecordSendStage(stage, outcome, SimpleUdpTelemetry.ElapsedSeconds(start));
            if (e != null) SimpleUdpTelemetry.SetFailure(activity, e);
            else if (outcome == SimpleUdpTelemetryNames.OutcomeSuccess) SimpleUdpTelemetry.SetSuccess(activity);
            SimpleUdpTelemetry.StopActivity(activity);
        }

        private void OnEndpointDetected(EndpointMetadata metadata)
        {
            EventHandler<EndpointMetadata> handler = EndpointDetected;
            if (handler == null) return;

            foreach (EventHandler<EndpointMetadata> subscriber in handler.GetInvocationList())
            {
                long start = Stopwatch.GetTimestamp();
                Activity activity = SimpleUdpTelemetry.StartStageActivity(SimpleUdpTelemetryNames.EventEndpointDetected);

                try
                {
                    subscriber(this, metadata);
                    SimpleUdpTelemetry.RecordHandler(SimpleUdpTelemetryNames.EventEndpointDetected, SimpleUdpTelemetryNames.OutcomeSuccess, null, SimpleUdpTelemetry.ElapsedSeconds(start));
                    SimpleUdpTelemetry.SetSuccess(activity);
                }
                catch (Exception e)
                {
                    SimpleUdpTelemetry.RecordHandler(SimpleUdpTelemetryNames.EventEndpointDetected, SimpleUdpTelemetryNames.OutcomeFailure, e, SimpleUdpTelemetry.ElapsedSeconds(start));
                    SimpleUdpTelemetry.SetFailure(activity, e);
                }
                finally
                {
                    SimpleUdpTelemetry.StopActivity(activity);
                }
            }
        }

        private void OnDatagramReceived(Datagram datagram)
        {
            EventHandler<Datagram> handler = DatagramReceived;
            if (handler == null) return;

            foreach (EventHandler<Datagram> subscriber in handler.GetInvocationList())
            {
                long start = Stopwatch.GetTimestamp();
                Activity activity = SimpleUdpTelemetry.StartStageActivity(SimpleUdpTelemetryNames.EventDatagramReceived);

                try
                {
                    subscriber(this, datagram);
                    SimpleUdpTelemetry.RecordHandler(SimpleUdpTelemetryNames.EventDatagramReceived, SimpleUdpTelemetryNames.OutcomeSuccess, null, SimpleUdpTelemetry.ElapsedSeconds(start));
                    SimpleUdpTelemetry.SetSuccess(activity);
                }
                catch (Exception e)
                {
                    SimpleUdpTelemetry.RecordHandler(SimpleUdpTelemetryNames.EventDatagramReceived, SimpleUdpTelemetryNames.OutcomeFailure, e, SimpleUdpTelemetry.ElapsedSeconds(start));
                    SimpleUdpTelemetry.SetFailure(activity, e);
                }
                finally
                {
                    SimpleUdpTelemetry.StopActivity(activity);
                }
            }
        }

        private void SendInternal(string ip, int port, byte[] data, short ttl)
        {
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), port);

            long queuedStart = Stopwatch.GetTimestamp();
            Activity queued = SimpleUdpTelemetry.StartStageActivity(SimpleUdpTelemetryNames.StageQueued);
            SimpleUdpTelemetry.AddSendQueued(1);

            try
            {
                _SendLock.Wait();
            }
            catch (Exception e)
            {
                SimpleUdpTelemetry.AddSendQueued(-1);
                CompleteStage(queued, SimpleUdpTelemetryNames.StageQueued, SimpleUdpTelemetryNames.OutcomeFailure, queuedStart, e);
                throw;
            }

            SimpleUdpTelemetry.AddSendQueued(-1);
            CompleteStage(queued, SimpleUdpTelemetryNames.StageQueued, SimpleUdpTelemetryNames.OutcomeSuccess, queuedStart, null);
            SimpleUdpTelemetry.AddSendActive(1);

            long transmitStart = Stopwatch.GetTimestamp();
            Activity transmit = SimpleUdpTelemetry.StartStageActivity(SimpleUdpTelemetryNames.StageTransmit);

            try
            {
                _Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, (int)ttl);
                _Socket.SendTo(data, ipe);
                CompleteStage(transmit, SimpleUdpTelemetryNames.StageTransmit, SimpleUdpTelemetryNames.OutcomeSuccess, transmitStart, null);
            }
            catch (Exception e)
            {
                CompleteStage(transmit, SimpleUdpTelemetryNames.StageTransmit, SimpleUdpTelemetryNames.OutcomeFailure, transmitStart, e);
                throw;
            }
            finally
            {
                SimpleUdpTelemetry.AddSendActive(-1);
                _SendLock.Release();
            }
        }

        private async Task<bool> SendInternalAsync(string ip, int port, byte[] data, short ttl)
        {
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), port);

            long queuedStart = Stopwatch.GetTimestamp();
            Activity queued = SimpleUdpTelemetry.StartStageActivity(SimpleUdpTelemetryNames.StageQueued);
            SimpleUdpTelemetry.AddSendQueued(1);

            try
            {
                await _SendLock.WaitAsync();
            }
            catch (Exception e)
            {
                SimpleUdpTelemetry.AddSendQueued(-1);
                CompleteStage(queued, SimpleUdpTelemetryNames.StageQueued, SimpleUdpTelemetryNames.OutcomeFailure, queuedStart, e);
                throw;
            }

            SimpleUdpTelemetry.AddSendQueued(-1);
            CompleteStage(queued, SimpleUdpTelemetryNames.StageQueued, SimpleUdpTelemetryNames.OutcomeSuccess, queuedStart, null);
            SimpleUdpTelemetry.AddSendActive(1);

            long transmitStart = Stopwatch.GetTimestamp();
            Activity transmit = SimpleUdpTelemetry.StartStageActivity(SimpleUdpTelemetryNames.StageTransmit);

            try
            {
                _Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, (int)ttl);
                await Task.Factory.FromAsync(
                    (callback, state) => _Socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, ipe, callback, state),
                    _Socket.EndSendTo,
                    null).ConfigureAwait(false);
                CompleteStage(transmit, SimpleUdpTelemetryNames.StageTransmit, SimpleUdpTelemetryNames.OutcomeSuccess, transmitStart, null);
                return true;
            }
            catch (OperationCanceledException)
            {
                CompleteStage(transmit, SimpleUdpTelemetryNames.StageTransmit, SimpleUdpTelemetryNames.OutcomeCanceled, transmitStart, null);
                return false;
            }
            catch (Exception e)
            {
                CompleteStage(transmit, SimpleUdpTelemetryNames.StageTransmit, SimpleUdpTelemetryNames.OutcomeFailure, transmitStart, e);
                throw;
            }
            finally
            {
                SimpleUdpTelemetry.AddSendActive(-1);
                _SendLock.Release();
            }
        }

        #endregion
    }
}
