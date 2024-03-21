namespace SimpleUdp
{
    using System;

    /// <summary>
    /// Datagram received by this endpoint.
    /// </summary>
    public class Datagram
    {
        #region Public-Members

        /// <summary>
        /// IP address of the remote endpoint.
        /// </summary>
        public string Ip
        {
            get
            {
                return _Ip;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Ip));
                _Ip = value;
            }
        }

        /// <summary>
        /// Port number of the remote endpoint.
        /// </summary>
        public int Port
        {
            get
            {
                return _Port;
            }
            set
            {
                if (value < 0 || value > 65535) throw new ArgumentOutOfRangeException(nameof(Port));
                _Port = value;
            }
        }

        /// <summary>
        /// Data received from the remote endpoint.
        /// </summary>
        public byte[] Data
        {
            get
            {
                return _Data;
            }
            set
            {
                if (value == null) value = Array.Empty<byte>();
                _Data = value;
            }
        }

        #endregion

        #region Private-Members

        private string _Ip = "127.0.0.1";
        private int _Port = 0;
        private byte[] _Data = Array.Empty<byte>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Datagram()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="ip">IP address of the remote endpoint.</param>
        /// <param name="port">Port of the remote endpoint.</param>
        /// <param name="data">Data.</param>
        internal Datagram(string ip, int port, byte[] data)
        {
            Ip = ip;
            Port = port;
            Data = data;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
