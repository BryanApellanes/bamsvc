# bamsvc

A BAM protocol service providing person registration and profile lookup with attribute-driven access control.

## Overview

bamsvc hosts a `WebApplicationBamServer` that exposes two interfaces:

- **REST API** -- conventional JSON endpoints for non-BAM clients (`/api/register`, `/api/profile/{handle}`)
- **BAM protocol** -- a catch-all route that feeds requests through the `BamRequestPipeline` for command resolution, authentication, authorization, and dispatch

All BAM protocol requests are handled by `RegistrationService`, which is registered in the server's `ComponentRegistry` so the pipeline can resolve it automatically. There is no manual endpoint for BAM invocations; the pipeline handles dispatch based on the `OperationIdentifier` in the request body.

## Starting the Service

```bash
# Default: HTTP on port 8080, server name "bamsvc"
dotnet run --project bamsvc

# Custom port
dotnet run --project bamsvc -- --port=9090

# Custom server name
dotnet run --project bamsvc -- myserver

# Enable TCP/UDP BamServer alongside HTTP
dotnet run --project bamsvc -- --tcp-udp
```

## Endpoints

### HTML Pages

| Path | Description |
|------|-------------|
| `/` | Index page |
| `/register` | Registration form |
| `/register-result` | Registration result page |

### REST API

These endpoints accept standard JSON and do not require sessions or BAM protocol headers.

#### `POST /api/register`

Registers a new person account.

```json
{
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane@example.com",
  "phone": "555-1234",
  "handle": "janedoe"
}
```

`firstName` and `lastName` are required. Returns `{ "personHandle": "..." }` on success.

#### `GET /api/profile/{handle}`

Returns public profile data for the given handle. Returns 404 if not found.

```json
{
  "profileHandle": "...",
  "personHandle": "...",
  "name": "...",
  "deviceHandle": "..."
}
```

### BAM Protocol (catch-all route)

Any request not matched by the above routes is handled by `WebApplicationBamServer` through the `BamRequestPipeline`. The request body must be a JSON-serialized `MethodInvocationRequest`.

## BAM Protocol Request Format

```json
{
  "operationIdentifier": "Bam.Svc.RegistrationService+RegisterPerson",
  "arguments": [
    { "parameterName": "firstName", "value": "Jane" },
    { "parameterName": "lastName", "value": "Doe" },
    { "parameterName": "email", "value": "jane@example.com" },
    { "parameterName": "phone", "value": "555-1234" },
    { "parameterName": "handle", "value": "janedoe" }
  ]
}
```

The `operationIdentifier` format is `TypeName+MethodName` (or `TypeName,MethodName`). The type name must be the fully qualified .NET type name. Arguments are matched to method parameters by name (case-insensitive).

## Available Operations

### `RegistrationService.RegisterPerson`

Registers a new person account. Contains PII (name, email, phone), so requests **must be encrypted**.

| Property | Value |
|----------|-------|
| Operation | `Bam.Svc.RegistrationService+RegisterPerson` |
| Required access | `Execute` |
| Anonymous access | Allowed |
| Encryption required | **Yes** |

**Pipeline path:** Encrypted anonymous -- the pipeline initializes a session (for ECDH key exchange), decrypts the request body, re-resolves the command, then authorizes.

### `RegistrationService.GetProfile`

Looks up a public profile by handle. No sensitive data in the request.

| Property | Value |
|----------|-------|
| Operation | `Bam.Svc.RegistrationService+GetProfile` |
| Required access | `Execute` |
| Anonymous access | Allowed |
| Encryption required | No |

**Pipeline path:** Plain anonymous -- the pipeline resolves the command, confirms anonymous access is allowed, skips session/actor/authentication, and authorizes directly.

## Pipeline Flow

Every BAM protocol request passes through `BamServerContextInitializer`, which follows a three-path branching model:

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

## Ensuring a Request Succeeds

### For plain anonymous requests (e.g. `GetProfile`)

1. The request body must be valid JSON conforming to `MethodInvocationRequest`.
2. The `operationIdentifier` must resolve to a type registered in the `ComponentRegistry` and a public method on that type.
3. The method must have `[AnonymousAccess]` (with `encryptionRequired: false`, which is the default).
4. All required method parameters must be present in the `arguments` array with correct `parameterName` values.

### For encrypted anonymous requests (e.g. `RegisterPerson`)

All of the above, plus:

5. A session must be established first. The client sends a session-start request; the server responds with a server public key and session ID.
6. The client derives a shared AES key via ECDH (client private key + server public key).
7. The request body must be AES-encrypted using the derived key.
8. The `X-Bam-Session-Id` header must contain the active session ID.
9. The server decrypts the body, then re-resolves the command from the decrypted content.

### For authenticated requests

All of the above (session required), plus:

10. The `Authorization` header must contain a valid `Bearer` JWT token.
11. The JWT must not be expired.
12. The JWT signature must be verifiable using the client's public key stored in the session.
13. The actor must be resolvable from the client's public key (profile lookup by key hash).
14. The actor's group memberships must grant sufficient access (>= the `[RequiredAccess]` level).

### Optional security headers

| Header | Purpose |
|--------|---------|
| `X-Bam-Session-Id` | Session identifier (required for encrypted/authenticated) |
| `Authorization` | `Bearer <JWT>` (required for authenticated) |
| `X-Bam-Body-Signature` | ECDSA signature of the request body |
| `X-Bam-Body-Signature-Algorithm` | Signature algorithm (default: SHA256WITHECDSA) |
| `X-Bam-Nonce` | Random nonce value |
| `X-Bam-Nonce-Hash` | HMAC-SHA256 of body using nonce as key |

## Failure Responses

When the pipeline fails, the server returns a JSON error with a status code indicating the failure stage:

| Status Code | Meaning | Common Cause |
|-------------|---------|--------------|
| 400 | Bad request | Malformed JSON or missing required fields |
| 403 | Denied | Actor access level is below `[RequiredAccess]` |
| 404 | Not found | Profile not found (REST API only) |
| 419 | Session initialization failed | Invalid or expired session ID |
| 420 | Session required | Encrypted/authenticated request sent without a session |
| 460 | Actor resolution failed | Client public key not associated with a known profile |
| 461 | Command resolution failed | `operationIdentifier` doesn't match a registered type/method |
| 462 | Authorization calculation failed | Authorization check encountered an error |
| 500 | Server error | Unhandled exception during processing |

## Access Control Attributes

Access control is declarative via attributes on service classes and methods:

```csharp
[RequiredAccess(BamAccess.Execute)]       // class-level: all methods require Execute
public class RegistrationService
{
    [AnonymousAccess(encryptionRequired: true)]  // PII in request
    public AccountData RegisterPerson(...) { ... }

    [AnonymousAccess]                            // public data lookup
    public object? GetProfile(string handle) { ... }
}
```

- `[RequiredAccess(access)]` -- sets the minimum `BamAccess` level (Denied < Read < Execute < Write)
- `[AnonymousAccess(allowAnonymous, encryptionRequired)]` -- allows unauthenticated callers; method-level overrides class-level
- When anonymous access is allowed, the pipeline grants `Execute` access automatically without requiring an actor

## Tests

```bash
dotnet run --project bamsvc.tests -- --ut
```

5 unit tests verify attribute-driven pipeline behavior:
- `AllowAnonymousAccessForRegisterPerson`
- `RequireEncryptionForRegisterPerson`
- `AllowAnonymousAccessForGetProfile`
- `NotRequireEncryptionForGetProfile`
- `GrantAccessForAnonymousCallerOnBothMethods`

## Dependencies

| Reference | Purpose |
|-----------|---------|
| `bam.server` | `WebApplicationBamServer`, `BamPlatform` |
| `bam.protocol` | Protocol types, attributes, `Command`, `MethodInvocationRequest` |
| `bam.base` | Core utilities, headers, extensions |
| `bam.configuration` | Configuration management |
| `bam.data.objects` | Data object model |
| `bam.data` | Data access (SQLite session store) |
| `bam.encryption` | Cryptographic primitives (ECDH, AES, ECDSA) |
| `bam.storage.encryption` | Encrypted profile repository |
| `bam.presentation` | HTML page rendering |

**Target Framework:** net10.0
**SDK:** Microsoft.NET.Sdk.Web
