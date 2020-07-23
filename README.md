![alt tag](https://github.com/jchristn/simpleudp/blob/master/assets/icon.ico)

# SimpleUdp

## Simple wrapper for UDP client and server in C# 

[![NuGet Version](https://img.shields.io/nuget/v/SimpleUdp.svg?style=flat)](https://www.nuget.org/packages/SimpleUdp/) [![NuGet](https://img.shields.io/nuget/dt/SimpleUdp.svg)](https://www.nuget.org/packages/SimpleUdp)    

SimpleUdp provides simple methods for creating your own UDP-based sockets application, enabling easy integration of sending data, receiving data, and building state machines.  
 
## New in v1.0.0

- Initial release

## Help or Feedback

Need help or have feedback?  Please file an issue here!

## Need TCP Instead?

I have you covered.

- WatsonTcp - easiest way to build TCP-based applications with built-in framing 
  - Github: https://github.com/jchristn/watsontcp
  - NuGet: https://www.nuget.org/packages/WatsonTcp/ 
  - [![NuGet Version](https://img.shields.io/nuget/v/WatsonTcp.svg?style=flat)](https://www.nuget.org/packages/WatsonTcp/)
- SimpleTcp - lightweight TCP wrapper without framing
  - Github: https://github.com/jchristn/simpletcp
  - NuGet: https://www.nuget.org/packages/SuperSimpleTcp/ 
  - [![NuGet Version](https://img.shields.io/nuget/v/SuperSimpleTcp.svg?style=flat)](https://www.nuget.org/packages/SuperSimpleTcp/)
- CavemanTcp - TCP wrapper exposing controls for sending and receiving data to build state machines
  - Github: https://github.com/jchristn/cavemantcp
  - NuGet: https://www.nuget.org/packages/CavemanTcp/ 
  - [![NuGet Version](https://img.shields.io/nuget/v/CavemanTcp.svg?style=flat)](https://www.nuget.org/packages/CavemanTcp/)
  
Don't know what to use?  Just ask!  File an issue, I'll be happy to help.

## Simple Example

Start a node.
```
using SimpleUdp;

UdpEndpoint udp = new UdpEndpoint("127.0.0.1", 8000);
udp.EndpointDetected += EndpointDetected;

// only if you want to receive messages...
udp.DatagramReceived += DatagramReceived;
udp.StartServer();

// send a message...
udp.Send("127.0.0.1", 8001, "Hello to my friend listening on port 8001!");

static void EndpointDetected(object sender, EndpointMetadata md)
{
  Console.WriteLine("Endpoint detected: " + md.Ip + ":" + md.Port);
}

static void DatagramReceived(object sender, Datagram dg)
{
  Console.WriteLine("[" + dg.Ip + ":" + dg.Port + "]: " + Encoding.UTF8.GetString(dg.Data));
} 
```

## Or Use the Node Project

Start node 1.
```
Node\bin\Debug\netcoreapp3.1> node 127.0.0.1 8000
```

Start node 2.
```
Node\bin\Debug\netcoreapp3.1> node 127.0.0.1 8001
```

Send message from node 1 to node 2.  To do this, enter a command as follows:
```
[ip:port] [msg]
```
i.e.
```
127.0.0.1:8001 hello to my friend running on port 8001!
```

```
[127.0.0.1:8000 Command/? for help]: 127.0.0.1:8001 hello to my friend on port 8001!
```

Send message from node 2 to node 1.
```
[127.0.0.1:8001 Command/? for help]: Endpoint detected: 127.0.0.1:8000
[127.0.0.1:8000]: hello to my friend on port 8001!
127.0.0.1:8000 hello back to you my friend!
```
 
## Running under Mono

.NET Core is the preferred environment for cross-platform deployment on Windows, Linux, and Mac.  For those that use Mono, SimpleUdp should work well in Mono environments.  It is recommended that you execute the containing EXE using --server and after using the Mono Ahead-of-Time Compiler (AOT).

```
mono --aot=nrgctx-trampolines=8096,nimt-trampolines=8096,ntrampolines=4048 --server myapp.exe
mono --server myapp.exe
```

## Version History

Please refer to CHANGELOG.md.
