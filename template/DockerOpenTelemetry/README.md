# Docker OpenTelemetry

This folder provides a simple local dashboard for viewing telemetry from the template service.

## What it includes

- Aspire Dashboard in Docker
- OTLP gRPC endpoint for local app telemetry
- `run.cmd` helper for quick startup

## How to use it

Start the dashboard:

```powershell
.\run.cmd
```

Or run Docker directly:

```powershell
docker run --rm -it -d -p 18888:18888 -p 4317:18889 -e ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true --name aspire-dashboard mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

Then point your app at:

```text
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
```

Open `http://localhost:18888` to view traces, metrics, and logs.
