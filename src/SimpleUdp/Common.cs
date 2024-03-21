namespace SimpleUdp
{
    using System;

    /// <summary>
    /// Commonly used static methods for SimpleUdp.
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Parse an IP:port string to its parts.
        /// </summary>
        /// <param name="ipPort">IP:port.</param>
        /// <param name="ip">IP address.</param>
        /// <param name="port">Port number.</param>
        public static void ParseIpPort(string ipPort, out string ip, out int port)
        {
            if (String.IsNullOrEmpty(ipPort)) throw new ArgumentNullException(nameof(ipPort));

            ip = null;
            port = -1;

            int colonIndex = ipPort.LastIndexOf(':');
            if (colonIndex != -1)
            {
                ip = ipPort.Substring(0, colonIndex);
                port = Convert.ToInt32(ipPort.Substring(colonIndex + 1));
            }
        }
    }
}
