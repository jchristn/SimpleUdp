# Change Log

## Current Version

v3.3.1

- Update the `Caching` dependency to `5.1.2`
- Update test tooling: Touchstone `0.2.0`, NUnit `5.0.0`, NUnit.Analyzers `4.15.0`, NUnit3TestAdapter `6.3.0`, Microsoft.NET.Test.Sdk `18.10.1`, coverlet.collector `10.1.0`
- Package verification test now derives the expected package version from the built assembly and skips the in-repo package during its build
- Harden receive-loop-stop telemetry assertions against disposed stops reported asynchronously by earlier test cases
- No public API changes

v3.3.0

- Add built-in telemetry: a `Meter` and `ActivitySource` named `SimpleUdp` emitting send, receive, handler, receive-loop, endpoint lifecycle, and recent-endpoints cache metrics plus `simpleudp send` / `simpleudp receive` / `simpleudp start` spans with stage children (see `TELEMETRY.md`)
- Add `SimpleUdpTelemetryNames` with every meter, source, instrument, span, and attribute name as public constants
- Add a Grafana dashboard at `assets/grafana/simpleudp.json`
- Raise the socket send buffer to at least 65535 bytes so `MaxDatagramSize` payloads up to 65507 bytes can be sent on macOS (default send buffer 9216)
- Dispose the socket when the constructor fails to bind
- No new dependencies

v3.2.0

- Update the `Caching` dependency to `5.0.1` and refresh test tooling (Microsoft.NET.Test.Sdk, coverlet, NUnit, NUnit3TestAdapter, xUnit runner)
- Add synchronous concurrent-send serialization and post-dispose `EnableBroadcast` coverage
- No public API changes

v3.1.1

- Add Touchstone-based shared test descriptors plus console, xUnit, and NUnit runners under `src/`
- Add broad coverage for parsing, model validation, endpoint detection, send validation, broadcast, payload integrity, async/concurrent sends, endpoint cache bounds, multi-process startup order, package verification, and disposal behavior
- Keep the receive loop alive on Windows after sending to a UDP port that is not listening yet
- Prevent invalid destination IP sends from leaking the internal send semaphore

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
