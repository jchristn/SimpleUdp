using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleUdp
{
    /// <summary>
    /// Remote endpoint metadata.
    /// </summary>
    public class EndpointMetadata
    {
        /// <summary>
        /// IP address of the remote endpoint.
        /// </summary>
        public string Ip { get; }

        /// <summary>
        /// Port number of the remote endpoint.
        /// </summary>
        public int Port { get; }

        internal EndpointMetadata()
        {

        }
         
        internal EndpointMetadata(string ip, int port)
        {
            if (String.IsNullOrEmpty(ip)) throw new ArgumentNullException(nameof(ip));
            if (port < 0 || port > 65535) throw new ArgumentException("Port must be greater than or equal to zero and less than or equal to 65535.");
            Ip = ip;
            Port = port;
        }
    }
}
