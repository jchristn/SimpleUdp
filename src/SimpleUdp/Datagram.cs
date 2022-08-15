using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleUdp
{
    /// <summary>
    /// Datagram received by this endpoint.
    /// </summary>
    public class Datagram
    {
        /// <summary>
        /// IP address of the remote endpoint.
        /// </summary>
        public string Ip { get; }

        /// <summary>
        /// Port number of the remote endpoint.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Data received from the remote endpoint.
        /// </summary>
        public byte[] Data { get; }

        internal Datagram()
        {

        }

        internal Datagram(string ip, int port, byte[] data)
        {
            //if (String.IsNullOrEmpty(ip)) throw new ArgumentNullException(nameof(ip));
            if (port < 0 || port > 65535) throw new ArgumentException("Port must be greater than or equal to zero and less than or equal to 65535.");

            Ip = ip;
            Port = port;
            Data = data;
        }
    }
}
