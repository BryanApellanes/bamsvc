# bamsvc

A multi-protocol BAM host that gateways identity operations to `bamid` over TCP and exposes actor registration with attribute-driven, pipeline-enforced access control.

## Overview

bamsvc hosts a `WebApplicationBamServer` (from `bam.server`) whose routes are registered through the **route-to-pipeline bridge** (`[RoutePrefix]`/`[RoutePath]` + `RouteHandlerRegistrar`): every request — including the public REST convenience routes — runs the full `BamRequestPipeline` (command resolution, anonymous-access check, session/encryption, authorization) rather than bypassing it with raw `MapPost`/`MapGet` mappings.

Two `[WebService]` classes are exposed:

- **`IdentityGateway`** — `POST /api/register` and `GET /api/profile/{handle}`: public REST convenience routes that forward to `bamid`'s generated `RegistrationServiceClient` over TCP (port 24515 by default, derived from bamid's server name; override with `--bamid-tcp-port=`) rather than running identity logic in-process. Outcome computation lives in `IdentityGatewayRoutes`.
- **`ActorRegistrationService`** — `/api/actors/*`: agent/organization registration and role assignment backed in-process by `bam.useraccounts`' `IUserAccountService`. Accounts/roles are a distinct concern from AuthN, which belongs to `bamid`.

Identity logic and the registration HTML pages moved out of bamsvc in the bamid refactor — identity to the standalone `bamid` service (consumed here as a client), the pages to `bamux` (which talks to `bamid` independently, not through bamsvc).

**Intended role:** `bamsvc` is the internal backend application communication service layer — it hosts BAM protocol endpoints for use *between* Bam-based applications/services, not for public consumption. The public-facing counterpart, exposing Bam services over plain HTTP for external API clients, is intended to be `bamapi` (currently an unimplemented scaffold — see its README).

## Starting the Service

```bash
# Default: HTTP on port 8080, server name "bamsvc"
dotnet run --project bamsvc/bamsvc.csproj

# Custom port / server name
dotnet run --project bamsvc/bamsvc.csproj -- myserver --port=9090

# HTTPS listener (port 8443 by default; override with --https-port=)
dotnet run --project bamsvc/bamsvc.csproj -- --https

# TCP and/or UDP BamServer listeners (raw sockets, no HTTP)
dotnet run --project bamsvc/bamsvc.csproj -- --tcp --udp        # or --tcp-udp for both
dotnet run --project bamsvc/bamsvc.csproj -- --tcp-udp --tcp-port=9413 --udp-port=9414

# Point at a bamid deployed under a non-default name/port
dotnet run --project bamsvc/bamsvc.csproj -- --bamid-tcp-port=24515
```

All flags are parsed by `BamsvcConfiguration.FromArgs`.

## Key Classes

| Class | Description |
|---|---|
| `Program` (top-level statements) | Parses `BamsvcConfiguration`, builds the `bamid`-backed `RegistrationServiceClient`, wires the `ComponentRegistry` and `WebServiceRegistry`, registers route handlers via the bridge, and starts the enabled servers (HTTP/HTTPS `WebApplicationBamServer`, TCP/UDP `BamServer`). |
| `BamsvcConfiguration` | Multi-protocol startup configuration parsed from CLI arguments. |
| `IdentityGateway` | `[WebService]` pipeline surface for `/api/register` and `/api/profile/{handle}`. |
| `IdentityGatewayRoutes` | Computes gateway outcomes by delegating to `IRegistrationService` calls against `bamid`. |
| `ActorRegistrationService` | `[WebService]` pipeline surface for `/api/actors/*`, backed by `IUserAccountService`. |

## Endpoints

### `IdentityGateway` (public REST convenience, anonymous)

- `POST /api/register` — `{ "firstName", "lastName", "email"?, "phone"?, "handle"? }`; `firstName`/`lastName` required.
- `GET /api/profile/{handle}` — public profile lookup; outcome carries 404 when not found.

### `ActorRegistrationService` (`/api/actors`, `[RequiredAccess(BamAccess.Execute)]`)

| Route | Method | Access |
|---|---|---|
| `/api/actors/register-agent` | POST | `[AnonymousAccess(encryptionRequired: true)]` — registers an autonomous actor; PII requires an encrypted session |
| `/api/actors/register-org` | POST | authenticated |
| `/api/actors/assign-roles` | POST | authenticated |
| `/api/actors/remove-roles` | POST | authenticated |
| `/api/actors/{handle}` | GET | `[AnonymousAccess]` — public actor profile lookup |

### BAM Protocol (catch-all route)

Any request not matched by the mapped routes is handled by `WebApplicationBamServer` through the `BamRequestPipeline`. The request body must be a JSON-serialized `MethodInvocationRequest`:

```json
{
  "operationIdentifier": "Bam.Svc.ActorRegistrationService+RegisterAgent",
  "arguments": { "name": "...", "personHandle": "...", "deviceHandle": "...", "handle": "..." }
}
```

The `operationIdentifier` format is `TypeName+MethodName`; the type must be resolvable from the `ComponentRegistry`. Arguments are matched to method parameters by name (case-insensitive). Bridge-mapped routes synthesize this same request shape internally, which is why they get identical pipeline enforcement.

## Pipeline Flow

Every request passes through `BamServerContextInitializer`, which follows a three-path branching model:

```
Request received
    |
    v
[1] Command Resolution (attempt 1)
    Parse MethodInvocationRequest, extract TypeName + MethodName
    |
    v
[2] Anonymous Access Check
    Read [AnonymousAccess] attribute from resolved command
    Determine: plain anonymous / encrypted anonymous / authenticated
    |
    +------ Plain Anonymous ------+
    |                             |
    |   Skip session, actor,      |
    |   and authentication        |
    |                             |
    +-- Encrypted Anonymous ---+  |
    |                          |  |
    |   Initialize session     |  |
    |   (ECDH key exchange)    |  |
    |   Decrypt request body   |  |
    |   Re-resolve command     |  |
    |                          |  |
    +---- Authenticated ----+  |  |
    |                       |  |  |
    |   Initialize session  |  |  |
    |   Resolve actor       |  |  |
    |   Authenticate (JWT,  |  |  |
    |   signature, nonce)   |  |  |
    |   Re-resolve command  |  |  |
    |                       |  |  |
    +-----------------------+--+--+
    |
    v
[3] Authorization
    AuthorizationCalculator compares actor access level
    against [RequiredAccess] attribute on the command.
    Anonymous commands with [AnonymousAccess] are granted
    Execute access automatically.
    |
    v
[4] Dispatch
    BamRequestProcessor deserializes the MethodInvocationRequest,
    resolves the service instance from ComponentRegistry,
    invokes the method via reflection, and returns the result.
```

### Security headers

| Header | Purpose |
|--------|---------|
| `X-Bam-Session-Id` | Session identifier (required for encrypted/authenticated) |
| `Authorization` | `Bearer <JWT>` (required for authenticated) |
| `X-Bam-Body-Signature` | ECDSA signature of the request body |
| `X-Bam-Body-Signature-Algorithm` | Signature algorithm (default: SHA256WITHECDSA) |
| `X-Bam-Nonce` | Random nonce value |
| `X-Bam-Nonce-Hash` | HMAC-SHA256 of body using nonce as key |

## Failure Responses

| Status Code | Meaning | Common Cause |
|-------------|---------|--------------|
| 400 | Bad request | Malformed JSON or missing required fields |
| 403 | Denied | Actor access level is below `[RequiredAccess]` |
| 404 | Not found | Profile not found |
| 419 | Session initialization failed | Invalid or expired session ID |
| 420 | Session required | Encrypted/authenticated request sent without a session |
| 460 | Actor resolution failed | Client public key not associated with a known profile |
| 461 | Command resolution failed | `operationIdentifier` doesn't match a registered type/method |
| 462 | Authorization calculation failed | Authorization check encountered an error |
| 500 | Server error | Unhandled exception during processing |

## Access Control Attributes

Access control is declarative via attributes on service classes and methods:

- `[RequiredAccess(access)]` — sets the minimum `BamAccess` level (Denied < Read < Execute < Write)
- `[AnonymousAccess(allowAnonymous, encryptionRequired)]` — allows unauthenticated callers; method-level overrides class-level
- When anonymous access is allowed, the pipeline grants `Execute` access automatically without requiring an actor
- `[WebService]` — marks a class eligible for remote execution; `WebServiceRegistry` refuses to resolve unadorned classes (`ClassNotAWebServiceException`)

## Dependencies

**Project References:** `bam.server`, `bam.protocol`, `bam.base`, `bam.configuration`, `bam.data.objects`, `bam.data` (SQLite session store), `bam.encryption`, `bam.storage.encryption`, `bam.useraccounts` (in-process account/role management), `bamid.client` (private repo — the generated client for `bamid`, the standalone identity host).

**Target Framework:** net10.0, `Microsoft.NET.Sdk.Web`.

## Running Tests

```bash
dotnet run --project bamsvc.tests/bamsvc.tests.csproj -- --ut
```

## Not Yet Verified

- What `bamid` itself enforces inside its `IRegistrationService` implementation and the `BamClientProtocols.Tcp` session layer is a separate private repository outside this repo's scope; the local pipeline now enforces access/encryption attributes on bamsvc's own routes either way.
