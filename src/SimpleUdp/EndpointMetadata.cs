namespace SimpleUdp
{
    using System;

    /// <summary>
    /// Remote endpoint metadata.
    /// </summary>
    public class EndpointMetadata
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

        #endregion

        #region Private-Members

        private string _Ip = "127.0.0.1";
        private int _Port = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public EndpointMetadata()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="ip">IP address of the remote endpoint.</param>
        /// <param name="port">Port of the remote endpoint.</param> 
        internal EndpointMetadata(string ip, int port)
        {
            Ip = ip;
            Port = port;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
