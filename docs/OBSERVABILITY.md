# Observability Guide

This document describes the observability features implemented in the Multi-Tenant Identity API, including structured logging with Serilog and distributed tracing with OpenTelemetry.

## Table of Contents

1. [Overview](#overview)
2. [Structured Logging with Serilog](#structured-logging-with-serilog)
3. [Distributed Tracing with OpenTelemetry](#distributed-tracing-with-opentelemetry)
4. [Metrics Collection](#metrics-collection)
5. [Exception Handling](#exception-handling)
6. [Configuration](#configuration)
7. [Tools and Dashboards](#tools-and-dashboards)

---

## Overview

The application implements a complete observability stack:

- **Structured Logging**: Serilog for rich, structured logs
- **Distributed Tracing**: OpenTelemetry for request tracing across services
- **Metrics**: Performance and runtime metrics collection
- **Exception Tracking**: Standardized ProblemDetails with detailed logging

---

## Structured Logging with Serilog

### Features

- **Multiple Sinks**:
  - Console output for development
  - File logging with daily rotation
  - Seq integration for log aggregation (optional)

- **Enrichers**:
  - Environment name (Development, Production, etc.)
  - Machine name
  - Thread ID
  - Request context

- **Log Levels**:
  - Information: General application flow
  - Warning: Microsoft framework warnings
  - Error: Caught exceptions
  - Fatal: Application crashes

### Log Format

**Console Output:**
```
[14:23:45 INF] MultiTenantIdentityApi.API.Controllers.AuthController: User login attempt for email@example.com
```

**File Output:**
```
2025-01-22 14:23:45.123 +00:00 [INF] MultiTenantIdentityApi.API.Controllers.AuthController: User login attempt for email@example.com
```

### Request Logging

Every HTTP request is automatically logged with:
- Request method and path
- Status code
- Response time
- Request host and user agent
- Remote IP address
- Tenant ID (from X-Tenant-Id header)

**Example:**
```
HTTP GET /api/tenants responded 200 in 45.3210 ms
```

### Log Locations

- **Console**: Real-time output during development
- **Files**: `logs/log-YYYYMMDD.txt` (30-day retention)
- **Seq**: `http://localhost:5341` (if configured)

---

## Distributed Tracing with OpenTelemetry

### What is Traced

The application automatically instruments:

1. **ASP.NET Core Requests**:
   - HTTP method, path, status code
   - Request duration
   - Exceptions (when they occur)
   - Excludes: `/health`, `/swagger` endpoints

2. **HTTP Client Calls**:
   - Outgoing HTTP requests
   - Response times and status codes

3. **SQL Database Queries**:
   - Query text (in development)
   - Execution time
   - Parameters (configurable)

### Trace Context

Each request gets a unique trace ID that propagates through the entire request chain:

```
TraceId: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
SpanId: 00f067aa0ba902b7
ParentId: 7d62e3a041f69eb4
```

### Exporters

Traces are exported to:
- **Console**: For development debugging
- **OTLP**: For production collectors (Jaeger, Zipkin, etc.)

### Example Trace Flow

```
Request: POST /api/auth/login
├─ ASP.NET Core Handler [45ms]
│  ├─ Tenant Resolution [2ms]
│  ├─ SQL Query: SELECT * FROM Users WHERE Email = @email [12ms]
│  ├─ Password Verification [25ms]
│  └─ JWT Token Generation [6ms]
└─ Response: 200 OK
```

---

## Metrics Collection

### Collected Metrics

1. **ASP.NET Core Metrics**:
   - Request count
   - Request duration
   - Active requests
   - Failed requests

2. **HTTP Client Metrics**:
   - Outgoing request count
   - Request duration
   - Failed requests

3. **Runtime Metrics**:
   - GC collections
   - Thread pool usage
   - Exception count
   - Memory usage

4. **Process Metrics**:
   - CPU usage
   - Memory usage
   - Handle count

### Metrics Exporters

- **Console**: Development debugging
- **OTLP**: Production metrics collectors (Prometheus, etc.)

---

## Exception Handling

### IExceptionHandler Implementation

The application uses `IExceptionHandler` (ASP.NET Core 8+) for centralized exception handling.

**Location**: `src/API/Handlers/GlobalExceptionHandler.cs`

### ProblemDetails Response

All exceptions are converted to RFC 7807 ProblemDetails:

**Production:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Invalid Request",
  "status": 400,
  "detail": "Required parameter is missing: userId",
  "instance": "/api/users",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

**Development (with debug info):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Invalid Request",
  "status": 400,
  "detail": "Required parameter is missing: userId",
  "instance": "/api/users",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "exceptionType": "ArgumentNullException",
  "stackTrace": "at MultiTenantIdentityApi.API.Controllers...",
  "innerException": {
    "message": "Inner exception details",
    "type": "InvalidOperationException"
  }
}
```

### Exception Mapping

| Exception Type | HTTP Status | Logged Level |
|---|---|---|
| `DomainException` | 400 Bad Request | Warning |
| `TenantNotFoundException` | 404 Not Found | Warning |
| `UnauthorizedTenantAccessException` | 403 Forbidden | Warning |
| `UnauthorizedAccessException` | 401 Unauthorized | Warning |
| `ArgumentException` | 400 Bad Request | Warning |
| Other Exceptions | 500 Internal Server Error | Error |

---

## Configuration

### appsettings.json

```json
{
  "OpenTelemetry": {
    "ServiceName": "MultiTenantIdentityApi",
    "ServiceVersion": "1.0.0",
    "OtlpEndpoint": "http://localhost:4317"
  },
  "Serilog": {
    "SeqServerUrl": "http://localhost:5341"
  }
}
```

### Environment-Specific Configuration

**Development:**
- Console and file logging enabled
- Detailed exception information in responses
- SQL query text logged in traces
- All trace data exported to console

**Production:**
- Minimal exception details in responses
- No SQL query text in traces (for security)
- Traces exported to OTLP collector only
- File logging with rotation

---

## Tools and Dashboards

### 1. Seq (Log Aggregation)

**Setup:**
```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
```

**Access**: http://localhost:5341

**Features**:
- Real-time log streaming
- Powerful query language
- Log filtering and searching
- Dashboards and alerts
- Retention policies

### 2. Jaeger (Distributed Tracing)

**Setup:**
```bash
docker run --name jaeger -d \
  -e COLLECTOR_OTLP_ENABLED=true \
  -p 16686:16686 \
  -p 4317:4317 \
  -p 4318:4318 \
  jaegertracing/all-in-one:latest
```

**Access**: http://localhost:16686

**Features**:
- Trace visualization
- Service dependency graph
- Performance analysis
- Trace comparison

### 3. Prometheus + Grafana (Metrics)

**Setup:**
```bash
# Prometheus
docker run --name prometheus -d \
  -p 9090:9090 \
  -v $(pwd)/prometheus.yml:/etc/prometheus/prometheus.yml \
  prom/prometheus

# Grafana
docker run --name grafana -d \
  -p 3000:3000 \
  grafana/grafana
```

**Access**:
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000

### 4. OpenTelemetry Collector (Optional)

**Setup:**
```bash
docker run --name otel-collector -d \
  -p 4317:4317 \
  -p 4318:4318 \
  -v $(pwd)/otel-collector-config.yaml:/etc/otel-collector-config.yaml \
  otel/opentelemetry-collector:latest \
  --config=/etc/otel-collector-config.yaml
```

---

## Quick Start

### 1. Run with Default Configuration

```bash
cd src/API
dotnet run
```

Logs will appear in:
- Console
- `logs/log-YYYYMMDD.txt`

### 2. Run with Seq

```bash
# Start Seq
docker run --name seq -d -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest

# Run API
cd src/API
dotnet run
```

View logs: http://localhost:5341

### 3. Run with Jaeger

```bash
# Start Jaeger
docker run --name jaeger -d \
  -e COLLECTOR_OTLP_ENABLED=true \
  -p 16686:16686 \
  -p 4317:4317 \
  jaegertracing/all-in-one:latest

# Run API
cd src/API
dotnet run
```

View traces: http://localhost:16686

---

## Best Practices

### Logging

1. **Use Structured Logging**:
   ```csharp
   _logger.LogInformation("User {UserId} logged in from {IpAddress}", userId, ipAddress);
   ```

2. **Use Appropriate Log Levels**:
   - `Trace`: Very detailed, high-volume
   - `Debug`: Detailed, debug information
   - `Information`: General flow
   - `Warning`: Unexpected but handled
   - `Error`: Errors and exceptions
   - `Fatal`: Application crashes

3. **Don't Log Sensitive Data**:
   - No passwords
   - No API keys
   - No personal data (PII)
   - Sanitize user input

### Tracing

1. **Add Custom Spans**:
   ```csharp
   using var activity = Activity.Current?.Source.StartActivity("CustomOperation");
   activity?.SetTag("tenant.id", tenantId);
   // ... operation code
   ```

2. **Tag Important Data**:
   ```csharp
   activity?.SetTag("user.id", userId);
   activity?.SetTag("tenant.id", tenantId);
   ```

3. **Record Exceptions**:
   ```csharp
   activity?.RecordException(exception);
   ```

### Metrics

1. **Create Custom Metrics**:
   ```csharp
   var meter = new Meter("MultiTenantIdentityApi");
   var counter = meter.CreateCounter<int>("custom.operation.count");
   counter.Add(1, new KeyValuePair<string, object?>("tenant.id", tenantId));
   ```

---

## Troubleshooting

### Seq Not Receiving Logs

Check Seq configuration in appsettings.json:
```json
{
  "Serilog": {
    "SeqServerUrl": "http://localhost:5341"
  }
}
```

Verify Seq is running:
```bash
docker ps | grep seq
```

### Jaeger Not Receiving Traces

Check OTLP endpoint in appsettings.json:
```json
{
  "OpenTelemetry": {
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

Verify Jaeger is running with OTLP enabled:
```bash
docker logs jaeger
```

### High Log Volume

Adjust minimum log level in Program.cs:
```csharp
.MinimumLevel.Information()  // Change to Warning or Error
```

---

## Performance Impact

### Logging
- **Console**: Minimal impact (~1-2ms per request)
- **File**: Low impact (~2-5ms per request)
- **Seq**: Low impact (~5-10ms per request)

### Tracing
- **Collection**: Minimal impact (~1-3ms per request)
- **Export**: Asynchronous, no blocking

### Metrics
- **Collection**: Negligible impact (<1ms)
- **Export**: Asynchronous, no blocking

---

## Production Recommendations

1. **Use External Collectors**: Don't log to local files in production
2. **Set Retention Policies**: Limit log and trace retention
3. **Use Sampling**: Sample traces in high-traffic scenarios
4. **Monitor Costs**: Cloud logging/tracing can be expensive
5. **Secure Endpoints**: Protect Seq, Jaeger, Prometheus endpoints
6. **Use HTTPS**: Secure communication with collectors
7. **Configure Alerts**: Set up alerts for errors and performance issues

---

## Additional Resources

- [Serilog Documentation](https://serilog.net/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Seq Documentation](https://docs.datalust.co/docs)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)
- [Prometheus Documentation](https://prometheus.io/docs/)
