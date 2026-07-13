# bamsvc

An ASP.NET Core host for the Bam protocol, currently implementing person registration and profile lookup over both HTML pages and a REST API.

## Overview

bamsvc builds a `WebApplicationBamServer` (from `bam.server`) with a `RegistrationService` wired into its `ComponentRegistry`, backed by an `AccountManager` and `IProfileManager` and a SQLite-based `ServerSessionSchemaRepository` for session data. It serves three HTML pages (`IndexPage`, `RegisterPage`, `RegisterResultPage`, via `bam.presentation`'s `MapPages`) and two REST endpoints: `POST /api/register` (creates a person account) and `GET /api/profile/{handle}` (looks up a profile by handle). It can optionally also start a TCP/UDP `BamServer` alongside the HTTP host when run with `--tcp-udp`.

**Intended role:** `bamsvc` is the internal backend application communication service layer — it hosts BAM protocol endpoints for use *between* Bam-based applications/services, not for public consumption. The public-facing counterpart, exposing Bam services over plain HTTP for external API clients, is intended to be `bamapi` (currently an unimplemented scaffold — see its README).

## Key Classes

| Class | Description |
|---|---|
| `Program` (top-level statements) | Builds a `WebApplicationBamServer`, wires up `RegistrationService`/`AccountManager`, maps HTML pages and the REST registration API, and optionally starts a TCP/UDP `BamServer`. |
| `RegistrationService` | `[RequiredAccess(BamAccess.Execute)]` service exposing `RegisterPerson` (`[AnonymousAccess(encryptionRequired: true)]`) and `GetProfile` (`[AnonymousAccess]`). |
| `IndexPage` / `RegisterPage` / `RegisterResultPage` | HTML pages served via `bam.presentation`'s `MapPages`. |
| `PersonRegistrationRequest` | Request body for `POST /api/register`. |

## Dependencies

**Project References:** `bam.server`, `bam.protocol`, `bam.base`, `bam.configuration`, `bam.data.objects`, `bam.data`, `bam.encryption`, `bam.storage.encryption`, `bam.presentation`.

**Target Framework:** net10.0, `Microsoft.NET.Sdk.Web`.

## Usage Examples

```bash
# Run the service (defaults to HTTP port 8080)
dotnet run --project bamsvc/bamsvc.csproj

# Override the port, and/or also start the TCP/UDP BamServer
dotnet run --project bamsvc/bamsvc.csproj -- --port=8081 --tcp-udp
```

```bash
# Register a person
curl -X POST http://localhost:8080/api/register \
  -H "Content-Type: application/json" \
  -d '{"firstName":"Jane","lastName":"Doe","email":"jane@example.com"}'

# Look up a profile
curl http://localhost:8080/api/profile/{handle}
```

## Running Tests

```bash
dotnet run --project bamsvc.tests/bamsvc.tests.csproj -- --ut
```

## Known Gaps / Not Yet Implemented

- **The REST endpoints bypass the BAM pipeline's security enforcement.** `RegistrationService.RegisterPerson` is decorated `[AnonymousAccess(encryptionRequired: true)]` and `GetProfile` with `[AnonymousAccess]`, but `Program.cs` calls both directly from raw `app.MapPost`/`app.MapGet` handlers instead of routing through the pipeline. This is exactly the problem `bam.server`'s [route-to-pipeline bridge](https://github.com/BryanApellanes/bam.server/blob/bam_server/docs/route-to-pipeline-bridge.md) design doc calls out: security attributes on a directly-called service method are silently ignored — no encryption is actually enforced on `/api/register` today despite `encryptionRequired: true`, and no authorization/session-management stage runs.
- That design doc explicitly references a companion `bamsvc/docs/route-to-pipeline-bridge.md` "for the consumer-side changes" — no such file (or any `docs/` directory) currently exists in this repository, so the bridge has not yet been adopted here.
- `GetProfile` returns `object?` rather than a concrete type.
