using System;
using System.Collections.Generic;
using System.Text;
using SimpleUdp;

namespace Node
{
    class Program
    {
        static UdpEndpoint _UdpEndpoint;

        static void Main(string[] args)
        {
            _UdpEndpoint = new UdpEndpoint("127.0.0.1", Convert.ToInt32(args[0]));
            _UdpEndpoint.EndpointDetected += EndpointDetected;
            _UdpEndpoint.DatagramReceived += DatagramReceived;
            _UdpEndpoint.StartServer();
            
            while (true)
            {
                Console.Write("Command [? for help]: ");
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
