using System;
using System.Collections.Generic;
using System.Text;
using SimpleUdp;

namespace Node
{
    class Program
    {
        static string _Ip = null;
        static int _Port = 0;
        static UdpEndpoint _UdpEndpoint;

        static void Main(string[] args)
        {
            if (args == null || args.Length < 2)
            {
                Usage();
                return;
            }
            /*
             * 
             * 
             * Usage:
             *    node 127.0.0.1 8000
             * 
             * Starts the endpoint on IP address 127.0.0.1 port 8000.
             *
             * 
             * 
             */

            _Ip = args[0];
            _Port = Convert.ToInt32(args[1]);

            _UdpEndpoint = new UdpEndpoint(_Ip, _Port);
            _UdpEndpoint.EndpointDetected += EndpointDetected;
            _UdpEndpoint.DatagramReceived += DatagramReceived;
            _UdpEndpoint.StartServer();
            
            while (true)
            {
                Console.Write("[" + _Ip + ":" + _Port + " Command/? for help]: ");
                string userInput = Console.ReadLine();
                if (String.IsNullOrEmpty(userInput)) continue;

                if (userInput.Equals("?"))
                {
                    Menu();
                }
                else if (userInput.Equals("q"))
                {
                    break;
                }
                else if (userInput.Equals("cls"))
                {
                    Console.Clear();
                }
                else if (userInput.Equals("list"))
                {
                    List<string> recents = _UdpEndpoint.Endpoints;
                    if (recents != null)
                    {
                        Console.WriteLine("Recent endpoints");
                        foreach (string endpoint in recents) Console.WriteLine("  " + endpoint);
                    }
                    else
                    {
                        Console.WriteLine("None");
                    }
                }
                else
                {
                    string[] parts = userInput.Split(new char[] { ' ' }, 2);
                    if (parts.Length != 2) continue;

                    string msg = parts[1];
                    string ipPort = parts[0];
                    int colonIndex = parts[0].LastIndexOf(':');
                    string ip = ipPort.Substring(0, colonIndex);
                    int port = Convert.ToInt32(ipPort.Substring(colonIndex + 1));

                    _UdpEndpoint.Send(ip, port, msg);
                }
            }

            Console.ReadLine();
        }

        static void Usage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("> node 127.0.0.1 8000");
            Console.WriteLine("Starts the endpoint on IP address 127.0.0.1 port 8000");
        }

        static void Menu()
        {
            Console.WriteLine("");
            Console.WriteLine("Available commands");
            Console.WriteLine("  q       quit");
            Console.WriteLine("  ?       help, this menu");
            Console.WriteLine("  cls     clear the screen");
            Console.WriteLine("  list    list recent endpoints");
            Console.WriteLine("");
            Console.WriteLine("To send a message, use the form 'ip:port message', i.e.");
            Console.WriteLine("127.0.0.1:8001 hello world!");
            Console.WriteLine("");
        }

        static void EndpointDetected(object sender, EndpointMetadata md)
        {
            Console.WriteLine("Endpoint detected: " + md.Ip + ":" + md.Port);
        }

        static void DatagramReceived(object sender, Datagram dg)
        {
            Console.WriteLine("[" + dg.Ip + ":" + dg.Port + "]: " + Encoding.UTF8.GetString(dg.Data));
        }
    }
}
