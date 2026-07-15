# bamsvc

An ASP.NET Core host for the Bam protocol, gatewaying identity operations to `bamid` over TCP.

## Overview

bamsvc builds a `WebApplicationBamServer` (from `bam.server`) and maps `IdentityGatewayRoutes` onto it — `POST /api/register` and `GET /api/profile/{handle}` — public REST convenience routes that forward to `bamid`'s generated `RegistrationServiceClient` over TCP (port 24515 by default, derived from bamid's server name) rather than running any identity logic in-process. It can optionally also start a TCP/UDP `BamServer` alongside the HTTP host when run with `--tcp-udp`.

This is a recent architectural shift: bamsvc used to host a local `RegistrationService`/`AccountManager`/`IProfileManager` directly (backed by a SQLite session repository) and its own `IndexPage`/`RegisterPage`/`RegisterResultPage`. Both the identity logic and the pages have since moved out — identity to the standalone `bamid` service (consumed here as a client), and the pages to `bamux`, which now serves the registration UI directly and calls its own identically-shaped `IdentityUxRoutes` (talking to `bamid` independently, not through bamsvc). bamsvc's `/api/register`/`/api/profile/{handle}` remain as a separate, non-UI-facing gateway onto the same `bamid` backend.

**Intended role:** `bamsvc` is the internal backend application communication service layer — it hosts BAM protocol endpoints for use *between* Bam-based applications/services, not for public consumption. The public-facing counterpart, exposing Bam services over plain HTTP for external API clients, is intended to be `bamapi` (currently an unimplemented scaffold — see its README).

## Key Classes

| Class | Description |
|---|---|
| `Program` (top-level statements) | Builds the `bamid`-backed `RegistrationServiceClient`, wires up `IdentityGatewayRoutes`, and starts the `WebApplicationBamServer` (optionally also a TCP/UDP `BamServer`). |
| `IdentityGatewayRoutes` | Maps `/api/register` and `/api/profile/{handle}` onto `IRegistrationService` calls against `bamid`. |

## Dependencies

**Project References:** `bam.server`, `bam.protocol`, `bam.base`, `bam.configuration`, `bam.data.objects`, `bam.data`, `bam.encryption`, `bam.storage.encryption`, `bamid.client` (private repo — the generated client for `bamid`, the standalone identity/user-management host).

**Target Framework:** net10.0, `Microsoft.NET.Sdk.Web`.

## Usage Examples

```bash
# Run the service (defaults to HTTP port 8080)
dotnet run --project bamsvc/bamsvc.csproj

# Override the port, and/or also start the TCP/UDP BamServer
dotnet run --project bamsvc/bamsvc.csproj -- --port=8081 --tcp-udp
```

```bash
curl -X POST http://localhost:8080/api/register \
  -H "Content-Type: application/json" \
  -d '{"firstName":"Jane","lastName":"Doe","email":"jane@example.com"}'

curl http://localhost:8080/api/profile/{handle}
```

## Running Tests

```bash
dotnet run --project bamsvc.tests/bamsvc.tests.csproj -- --ut
```

## Known Gaps / Not Yet Implemented

- **`IdentityGatewayRoutes` maps its two routes with raw `app.MapPost`/`app.MapGet`, not through `bam.server`'s route-to-pipeline bridge** (`[RoutePrefix]`/`[RoutePath]` + `RouteHandlerRegistrar`). Whatever access-control/encryption enforcement applies now lives inside `bamid`'s own service methods (via its `IRegistrationService` implementation) and/or the `BamClientProtocols.Tcp` session layer, not in a pipeline stage local to this repo — I have not verified what `bamid` itself enforces, since it's a separate private repository outside this audit's scope.
- A previous version of this note described a local `RegistrationService` with a verified `[AnonymousAccess(encryptionRequired: true)]` bypass; that code has since been removed entirely in favor of the `bamid`-backed gateway above, so that specific finding no longer applies.
