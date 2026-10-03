# SimpleUdp Telemetry

SimpleUdp measures itself and hands the numbers to whatever your host process already runs. It emits metrics through a .NET `Meter` and traces through an `ActivitySource`, both named **`SimpleUdp`**, and stops there. The library takes no dependency on OpenTelemetry, Radiant, or any exporter, and never opens a connection to a backend. **SimpleUdp emits, your host collects.** With nothing subscribed, each recording point is a cheap `Enabled`/`HasListeners` check and allocates nothing.

Available from v3.3.0. Nothing to turn on inside SimpleUdp: subscribe a collector to the two names below and the data flows.

| Source | Name | Version |
|---|---|---|
| `System.Diagnostics.Metrics.Meter` | `SimpleUdp` | library version |
| `System.Diagnostics.ActivitySource` | `SimpleUdp` | library version |

All names (instruments, spans, attributes, and label values) are public constants on `SimpleUdp.SimpleUdpTelemetryNames`. Treat them as API.

## Subscribing

### Radiant

```csharp
RadiantSettings settings = new RadiantSettings("my-udp-service");
settings.Sources.AddMeter("SimpleUdp");            // or SimpleUdpTelemetryNames.MeterName
settings.Sources.AddActivitySource("SimpleUdp");   // or SimpleUdpTelemetryNames.ActivitySourceName

using (RadiantHost host = RadiantHost.Start(settings))
{
    using (UdpEndpoint udp = new UdpEndpoint("127.0.0.1", 8000))
    {
        // run the app
    }
}
```

### OpenTelemetry SDK

```csharp
using MeterProvider meters = Sdk.CreateMeterProviderBuilder()
    .AddMeter("SimpleUdp")
    .AddPrometheusHttpListener()   // or .AddOtlpExporter()
    .Build();

using TracerProvider traces = Sdk.CreateTracerProviderBuilder()
    .AddSource("SimpleUdp")
    .AddOtlpExporter()             // e.g. Tempo at http://127.0.0.1:4317
    .Build();
```

### In-process (tests, custom sinks)

Use `System.Diagnostics.Metrics.MeterListener` filtered on `instrument.Meter.Name == "SimpleUdp"` and an `ActivityListener` whose `ShouldListenTo` matches `source.Name == "SimpleUdp"`. `src/Test.Shared/TelemetryCapture.cs` is a complete example.

Histogram bucket boundaries are a collector concern (OpenTelemetry views or a Radiant latency preset). SimpleUdp emits raw measurements and computes no quantiles in process.

## Metrics catalog

Prometheus names assume the standard OpenTelemetry Prometheus exporter conversion (dots to underscores, unit and `_total` suffixes, `{annotation}` units dropped). Label keys are converted the same way (`simpleudp.send.mode` becomes `simpleudp_send_mode`, `error.type` becomes `error_type`).

| Instrument | Type | Unit | Labels | Prometheus series | Description |
|---|---|---|---|---|---|
| `simpleudp.send.duration` | Histogram | s | `simpleudp.send.mode`, `outcome`, `error.type` | `simpleudp_send_duration_seconds` | End-to-end duration of a `Send`/`SendAsync` call: validation, slot wait, and transmit. |
| `simpleudp.send.datagrams` | Counter | {datagram} | `simpleudp.send.mode`, `outcome`, `error.type` | `simpleudp_send_datagrams_total` | Send calls by outcome. Validation failures are included. |
| `simpleudp.send.stage.duration` | Histogram | s | `stage`, `outcome` | `simpleudp_send_stage_duration_seconds` | Per-stage send latency. `queued` = waiting for the endpoint's single send slot; `transmit` = setting TTL and writing to the socket. |
| `simpleudp.send.stage.events` | Counter | {event} | `stage`, `outcome` | `simpleudp_send_stage_events_total` | Per-stage executions by outcome. |
| `simpleudp.send.queued` | UpDownCounter | {send} | none | `simpleudp_send_queued` | Sends currently waiting for a send slot (queue depth). |
| `simpleudp.send.active` | UpDownCounter | {send} | none | `simpleudp_send_active` | Sends currently holding a send slot. Capacity is 1 per endpoint, so compare against `simpleudp_endpoints_active`. |
| `simpleudp.sent.bytes` | Counter | By | none | `simpleudp_sent_bytes_total` | Payload bytes sent successfully. |
| `simpleudp.datagram.size` | Histogram | By | `network.io.direction` (`transmit`, `receive`) | `simpleudp_datagram_size_bytes` | Payload size distribution. |
| `simpleudp.receive.datagrams` | Counter | {datagram} | none | `simpleudp_receive_datagrams_total` | Datagrams received. |
| `simpleudp.received.bytes` | Counter | By | none | `simpleudp_received_bytes_total` | Payload bytes received and delivered to handlers. |
| `simpleudp.receive.truncated` | Counter | {datagram} | none | `simpleudp_receive_truncated_total` | Datagrams cut down to `MaxDatagramSize` (data loss). |
| `simpleudp.receive.duration` | Histogram | s | `outcome` | `simpleudp_receive_duration_seconds` | Time to process one received datagram, including every event handler. The receive loop is serial, so this bounds receive throughput. |
| `simpleudp.receive.loop.stops` | Counter | {stop} | `simpleudp.stop.reason` (`disposed`, `error`), `error.type` | `simpleudp_receive_loop_stops_total` | Receive loop terminations. `error` means the endpoint has stopped receiving without being disposed. |
| `simpleudp.handler.duration` | Histogram | s | `simpleudp.event`, `outcome`, `error.type` | `simpleudp_handler_duration_seconds` | Duration of each application handler (`endpoint_detected`, `datagram_received`). |
| `simpleudp.handler.invocations` | Counter | {invocation} | `simpleudp.event`, `outcome`, `error.type` | `simpleudp_handler_invocations_total` | Handler invocations by outcome. SimpleUdp swallows handler exceptions to keep the loop alive; this is where they show up. |
| `simpleudp.remote_endpoints.detected` | Counter | {endpoint} | none | `simpleudp_remote_endpoints_detected_total` | New remote endpoints seen. |
| `simpleudp.remote_endpoints.cached` | UpDownCounter | {endpoint} | none | `simpleudp_remote_endpoints_cached` | Remote endpoints held in the recent-endpoints caches (all local endpoints). |
| `simpleudp.remote_endpoints.capacity` | UpDownCounter | {endpoint} | none | `simpleudp_remote_endpoints_capacity` | Total cache capacity (100 per local endpoint). |
| `simpleudp.remote_endpoints.evicted` | Counter | {endpoint} | none | `simpleudp_remote_endpoints_evicted_total` | Remote endpoints evicted from the cache (LRU churn). |
| `simpleudp.endpoints.active` | UpDownCounter | {endpoint} | none | `simpleudp_endpoints_active` | Local `UdpEndpoint` instances bound and not disposed. |
| `simpleudp.endpoint.starts` | Counter | {endpoint} | `outcome`, `error.type` | `simpleudp_endpoint_starts_total` | Construction (socket bind) attempts by outcome, e.g. `System.Net.Sockets.SocketException` on a port conflict. |
| `simpleudp.build.info` | ObservableGauge | {info} | `simpleudp.version` | `simpleudp_build_info` | Always 1; carries the library version. |

### Label values

| Label | Values |
|---|---|
| `outcome` | `success`, `failure`, `canceled` |
| `stage` | `queued`, `transmit` |
| `simpleudp.send.mode` | `sync`, `async` |
| `simpleudp.event` | `endpoint_detected`, `datagram_received` |
| `simpleudp.stop.reason` | `disposed`, `error` |
| `network.io.direction` | `transmit`, `receive` |
| `error.type` | Fully qualified exception type name, e.g. `System.ArgumentException`, `System.FormatException`, `System.ObjectDisposedException`, `System.Net.Sockets.SocketException` |

Every label is bounded. IP addresses, ports, and payloads never appear on metrics; addresses and ports go on spans only, and payloads appear nowhere.

## Spans catalog

| Span | Kind | Parent | Attributes | Status |
|---|---|---|---|---|
| `simpleudp send` | Client | Caller's `Activity.Current` | `network.transport`=`udp`, `network.peer.address`, `network.peer.port`, `network.local.port`, `simpleudp.send.mode`, `simpleudp.ttl`, `simpleudp.max_datagram_size`, `simpleudp.datagram.size`, `outcome`, `error.type` on failure | `Ok` on success; `Error` with an `exception` event on failure; unset when canceled |
| `stage:queued` | Internal | `simpleudp send` | `network.transport` | `Ok` / `Error` |
| `stage:transmit` | Internal | `simpleudp send` | `network.transport`, `error.type` on failure | `Ok` / `Error` |
| `simpleudp receive` | Consumer | **None (root)** | `network.transport`, `network.peer.address`, `network.peer.port`, `network.local.port`, `simpleudp.datagram.size`, `simpleudp.remote_endpoint.new`, `simpleudp.truncated` when truncated, `simpleudp.stop.reason` when the loop ends | `Ok`; `Error` with an `exception` event when the socket faults |
| `stage:endpoint_detected` | Internal | `simpleudp receive` | `network.transport`, `error.type` on failure | `Ok`; `Error` with an `exception` event when the handler throws |
| `stage:datagram_received` | Internal | `simpleudp receive` | `network.transport`, `error.type` on failure | `Ok`; `Error` with an `exception` event when the handler throws |
| `simpleudp start` | Internal | Caller's `Activity.Current` | `network.transport`, `network.local.address`, `network.local.port` (the bound port, including ephemeral ports), `simpleudp.max_datagram_size` | `Ok`; `Error` with an `exception` event on a bind failure |

Context propagation:

- **Send** spans nest under whatever `Activity.Current` the caller has, so a send made inside a Watson route or your own span joins that trace.
- **Receive** spans are always roots. The receive loop runs on I/O completion threads that would otherwise inherit whatever context was current when the endpoint was constructed, and that would chain every datagram ever received into one trace.
- **Handlers** run with `Activity.Current` set to their `stage:*` span, so any span or `ILogger` call inside your `DatagramReceived`/`EndpointDetected` handler is part of the receive trace. That covers the hand-off from the background receive loop to your code.
- **Across the wire**, UDP has no headers, so SimpleUdp does not inject or extract W3C `traceparent`. Doing so would change the payload and break interop with non-SimpleUdp peers. If you need cross-process traces, put a `traceparent` inside your own message format and start a span with it as the parent inside your handler.

## Configuration

There is nothing to configure inside SimpleUdp. Telemetry is always emitted to the `SimpleUdp` meter and source, and costs effectively nothing until a listener subscribes. To turn it off, don't subscribe (or drop the names from your collector). Exporter endpoints, service names, sampling, and histogram buckets belong to the host's collector (for example Radiant's `RadiantSettings` or the OpenTelemetry SDK). Bind the collector's endpoints and any Prometheus scrape listener to `127.0.0.1` or an internal network, never a public interface.

## How to answer the on-call questions

| Question | Look at |
|---|---|
| Are sends failing, and why? | `sum by (error_type, simpleudp_send_mode) (rate(simpleudp_send_datagrams_total{outcome="failure"}[5m]))` |
| Where does send time go? | `simpleudp_send_stage_duration_seconds` p95 by `stage`. High `queued` means slot contention; high `transmit` means the socket or kernel is slow. |
| Is send concurrency backing up? | `simpleudp_send_queued` |
| Is the receive loop alive? | `increase(simpleudp_receive_loop_stops_total{simpleudp_stop_reason="error"}[5m])` and `rate(simpleudp_receive_datagrams_total[5m])` |
| Is a handler slow or throwing? | `simpleudp_handler_duration_seconds` p95 by `simpleudp_event`; `simpleudp_handler_invocations_total{outcome="failure"}` by `error_type` |
| Are we losing data? | `simpleudp_receive_truncated_total` |
| Are we churning peers? | `rate(simpleudp_remote_endpoints_evicted_total[5m])`, `simpleudp_remote_endpoints_cached / simpleudp_remote_endpoints_capacity` |
| Did an endpoint fail to start? | `simpleudp_endpoint_starts_total{outcome="failure"}` by `error_type` |
| One specific slow or failed datagram | Tempo: search `name="simpleudp send"` or `"simpleudp receive"` with `status=error`, then filter by `network.peer.address`/`network.peer.port` |

## Recommended PromQL alerts

```yaml
groups:
  - name: simpleudp
    rules:
      - alert: SimpleUdpReceiveLoopFaulted
        expr: increase(simpleudp_receive_loop_stops_total{simpleudp_stop_reason="error"}[5m]) > 0
        labels: { severity: critical }
        annotations:
          summary: "A SimpleUdp endpoint stopped receiving without being disposed ({{ $labels.error_type }})."

      - alert: SimpleUdpSendErrorRatioHigh
        expr: |
          sum(rate(simpleudp_send_datagrams_total{outcome="failure"}[5m]))
            / clamp_min(sum(rate(simpleudp_send_datagrams_total[5m])), 1e-9) > 0.05
        for: 10m
        labels: { severity: warning }
        annotations:
          summary: "More than 5% of SimpleUdp sends are failing."

      - alert: SimpleUdpHandlerExceptions
        expr: sum by (simpleudp_event, error_type) (rate(simpleudp_handler_invocations_total{outcome="failure"}[5m])) > 0
        for: 5m
        labels: { severity: warning }
        annotations:
          summary: "SimpleUdp {{ $labels.simpleudp_event }} handler is throwing {{ $labels.error_type }} (exceptions are swallowed)."

      - alert: SimpleUdpSendQueueBacklog
        expr: max_over_time(simpleudp_send_queued[5m]) > 50
        for: 5m
        labels: { severity: warning }
        annotations:
          summary: "SimpleUdp sends are queuing behind the per-endpoint send slot."

      - alert: SimpleUdpHandlerSlow
        expr: |
          histogram_quantile(0.95, sum by (le, simpleudp_event) (rate(simpleudp_handler_duration_seconds_bucket[5m]))) > 0.1
        for: 10m
        labels: { severity: warning }
        annotations:
          summary: "SimpleUdp {{ $labels.simpleudp_event }} handler p95 above 100ms; the receive loop is serial and will drop datagrams."

      - alert: SimpleUdpDatagramsTruncated
        expr: increase(simpleudp_receive_truncated_total[15m]) > 0
        labels: { severity: warning }
        annotations:
          summary: "Received datagrams are larger than MaxDatagramSize and are being truncated."

      - alert: SimpleUdpEndpointStartFailures
        expr: increase(simpleudp_endpoint_starts_total{outcome="failure"}[5m]) > 0
        labels: { severity: warning }
        annotations:
          summary: "A SimpleUdp endpoint failed to bind ({{ $labels.error_type }})."
```

## Dashboard

SimpleUdp is a library, so it ships no compose stack of its own. The host service's `compose.yaml` and Grafana provisioning own that. To help, `assets/grafana/simpleudp.json` is a ready-to-provision dashboard (Prometheus datasource UID `prometheus`, Tempo UID `tempo`). Drop it into the host's dashboards directory, in the product folder, as the "UDP" domain dashboard.

| Row | Panels |
|---|---|
| Overview | Active endpoints, send rate, receive rate, send error ratio, receive loop faults (1h), handler failures (1h) |
| Send | Send rate by outcome and mode, send p50/p95/p99, per-stage p95 (`queued`, `transmit`), send slot queued vs active vs capacity, failures by `error.type` |
| Receive | Bytes sent and received, receive processing p95 and handler p95 by event, datagram size p95 by direction, handler failures by `error.type`, truncations, loop stops by reason |
| Endpoints | Remote endpoints cached vs capacity, detection and eviction rate, endpoint starts by outcome, library version table |

A `job` variable scopes every panel, and a dashboard link opens Tempo filtered to failed `simpleudp.*` spans.

## Testing

`src/Test.Shared/TelemetryTestSuite.cs` (run by `Test.Automated`, `Test.Xunit`, and `Test.Nunit`) subscribes an in-memory `MeterListener` and `ActivityListener` and verifies every instrument, the span tree for send and receive, root-span behavior on receive, handler context propagation, every failure path (validation, invalid IP, disposed socket, handler exceptions, bind conflicts, truncation, cache eviction, loop stop), and that the no-listener path never throws.
