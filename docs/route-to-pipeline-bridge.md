# Route-to-Pipeline Bridge: bamsvc Changes

> See also: [bam.server route-to-pipeline-bridge.md](../../bam.server/docs/route-to-pipeline-bridge.md) for the framework-side implementation.

## Problem

In `bamsvc/Program.cs`, `RegistrationService` was consumed in two conflicting ways:

1. **Via BamPipeline** (catch-all route `{**path}`): Full initialization pipeline — command resolution, `[AnonymousAccess]` checks, ECDH encryption/decryption, session management, authorization.

2. **Via direct minimal API routes** (`MapPost("/api/register", ...)` and `MapGet("/api/profile/{handle}", ...)`): Called `registrationService.RegisterPerson(...)` directly — bypassing the pipeline entirely. The `[AnonymousAccess(encryptionRequired: true)]` attribute on `RegisterPerson` was silently ignored.

## Changes

### `RegistrationService.cs`

Added route attributes (all existing attributes preserved):

```csharp
[RequiredAccess(BamAccess.Execute)]
[RoutePrefix("/api/registration")]
public class RegistrationService
{
    [AnonymousAccess(encryptionRequired: true)]
    [RoutePath("/register", "POST")]
    public AccountData RegisterPerson(...) { ... }

    [AnonymousAccess]
    [RoutePath("/profile/{handle}", "GET")]
    public object? GetProfile(string handle) { ... }
}
```

### `Program.cs`

Replaced ~30 lines of manual route registration with one line:

```csharp
var webServer = new WebApplicationBamServer(options);
webServer.AddRouteHandler<RegistrationService>();  // replaces manual MapPost/MapGet
```

**Removed:**
- `registrationService` local variable (was resolved from `ComponentRegistry` for direct use)
- `MapPost("/api/register", ...)` lambda with manual validation and error handling
- `MapGet("/api/profile/{handle}", ...)` lambda

**Resulting routes:**
- `POST /api/registration/register` — flows through full pipeline, encryption enforced
- `GET /api/registration/profile/{handle}` — flows through full pipeline, anonymous access allowed

## Route Path Change

The routes changed slightly due to the prefix-based pattern:

| Before | After |
|--------|-------|
| `POST /api/register` | `POST /api/registration/register` |
| `GET /api/profile/{handle}` | `GET /api/registration/profile/{handle}` |

## Verification

1. `dotnet build` from `bamtk.sln` — no compilation errors
2. `GET /api/registration/profile/testhandle` — should flow through pipeline (anonymous access allowed)
3. `POST /api/registration/register` without session — should fail (encryption required but no session)
4. Catch-all path still works for BAM protocol clients
