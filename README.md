# bamsvc

A minimal ASP.NET Core web service providing a BAM protocol endpoint with data storage, encryption, and configuration support.

## Overview

bamsvc is an ASP.NET Core web application (using the Web SDK) that serves as a lightweight service host for the BAM protocol stack. It bootstraps a `WebApplication` using the standard ASP.NET Core builder pattern with a single `GET /` endpoint returning "Hello World!" and an additional catch-all `app.Run` handler for custom request processing.

The project references a broad set of BAM framework libraries -- bam.protocol, bam.base, bam.configuration, bam.data.objects, bam.data, bam.encryption, and bam.storage.encryption -- indicating it is designed to serve as a backend service capable of handling BAM protocol requests, managing structured data objects, and providing encrypted storage. However, the current `Program.cs` contains only scaffolding code with an empty `app.Run` handler body.

The service is positioned as the server-side counterpart to the BAM protocol client libraries, providing endpoints that BAM clients can connect to for data operations, configuration management, and encrypted communication.

## Key Classes

| Class | Description |
|---|---|
| `Program` (top-level statements) | Application entry point: configures and runs the ASP.NET Core web host |

## Dependencies

**Project References:**
- `bam.protocol` -- BAM protocol definitions and message handling
- `bam.base` -- Core BAM framework library
- `bam.configuration` -- Configuration management
- `bam.data.objects` -- Data object model and repositories
- `bam.data` -- Data access layer
- `bam.encryption` -- Encryption primitives
- `bam.storage.encryption` -- Encrypted storage providers

**Target Framework:** net10.0
**SDK:** Microsoft.NET.Sdk.Web

## Usage Examples

```bash
# Run the service
dotnet run --project bamsvc

# The service starts and listens for HTTP requests
# GET / returns "Hello World!"
```

```csharp
// Current Program.cs structure:
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run(async context =>
{
    // Request handling to be implemented
});
```

## Known Gaps / Not Yet Implemented

- **The `app.Run` handler body is empty** -- no BAM protocol request processing is implemented.
- No middleware, authentication, or authorization is configured.
- No service registrations are made in the DI container despite having many project references.
- None of the referenced libraries (bam.protocol, bam.data.objects, bam.encryption, etc.) are actually used in the code.
- No configuration files (appsettings.json, etc.) are included.
- No API controllers, endpoints, or request handlers beyond the "Hello World!" route.
