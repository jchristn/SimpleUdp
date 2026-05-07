# Change Log

## Current Version

v3.1.0

- Add `EnableBroadcast` for opt-in UDP broadcast sends without changing existing send or receive APIs
- Preserve the single bound socket model introduced in `v3.0.0`

v3.0.0

- Drop target frameworks below `.NET 8.0`
- Use a single bound socket for both send and receive so outbound datagrams originate from the configured local port

v2.0.x

- Retarget to .NET 8.0
- Removal of `Start`, `Stop` APIs, and, the started event
- Better multi-platform compatibility (Windows, Mac OSX, Ubuntu)

## Previous Versions

v1.2.x
- Support for broadcast endpoints (set IP to null), thank you @charleypeng
- Resolve issue associated with rapid send operations, thank you @seatrix

v1.1.x

- ```Events.Started``` and ```Events.Stopped```
- ```UdpEndpoint.Start``` and ```UdpEndpoint.Stop``` APIs


v1.0.0

- Initial release
