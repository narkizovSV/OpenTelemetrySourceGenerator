# OpenTelemetrySourceGenerator

A source generator that creates extension methods to wrap interface calls with tracing (`Activity`/span).

## What’s in the repository

- `sources/TraceUtils` – attributes and runtime utilities.
- `sources/TraceUtils.SourceGenerator` – Roslyn Source Generator.
- `sources/TraceUtils.SourceGenerator.Tests` – generator tests (NUnit + Verify snapshots).

## How to use

### 1. Project setup

Add the following NuGet references to the project:

+ `TraceUtils`
+ `TraceUtils.SourceGenerator`
+ `OpenTelemetry` (official .NET SDK)

### 2. Interface annotations

+ **On methods**: `[ActivityOperation("OperationName", ActivityType.Internal)]` — defines the operation name and activity type.
+ **On arguments**: `[SpanTag("tag.name")]` or `[SpanTag("tag.name", shouldSerialize: true)]` — writes parameters to the span as tags (the second option serializes complex types).

### 3. Calling generated methods

The generator creates extension methods in `TraceUtils.Extensions`:

+ **sync**: `MethodNameWithTrace(...)`
+ **async**: `MethodNameWithTraceAsync(...)`

Instead of calling `_service.Method(...)` directly, call `_service.MethodWithTrace(...)` so the invocation is captured in a span.

### 4. Registration in OpenTelemetry

When configuring `OpenTelemetry TracerProvider`, register the activity source from `TraceUtils` (via `AddSource(TraceUtils.Utils.ServiceName)`) so that generated spans are exported by the selected exporter.

```csharp
using OpenTelemetry;
using OpenTelemetry.Trace;

// When configuring the trace provider:
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(TraceUtils.Utils.ServiceName)
        .AddYourExporter(...));
```