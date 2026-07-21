# bamsvc Parity — Multi-Protocol Support & WebService Registry

## Overview

Update bamsvc to support configurable multi-protocol exposure (HTTP, HTTPS, TCP, UDP) and introduce a `WebServiceRegistry` that enforces the `[WebService]` attribute on classes resolved for remote execution.

## Part 1: WebServiceAttribute & WebServiceRegistry (bam.server)

### 1.1 — WebServiceAttribute
- File: `bam.server/WebServiceAttribute.cs`
- Marker attribute extending `System.Attribute`, targeting classes
- Designates classes eligible for remote execution via web service endpoints

### 1.2 — ClassNotAWebServiceException
- File: `bam.server/ClassNotAWebServiceException.cs`
- Extends `Exception`, includes the offending type name
- Thrown when `WebServiceRegistry` resolves a class lacking `[WebService]`

### 1.3 — WebServiceRegistry
- File: `bam.server/WebServiceRegistry.cs`
- Extends `ServiceRegistry` (which extends `DependencyProvider`)
- Overrides virtual `Get<T>()`, `Get(Type)`, `Get<T>(params object[])`, `Get(Type, params object[])` methods
- After resolving, validates the concrete type has `[WebService]`; throws `ClassNotAWebServiceException` if not

### 1.4 — DependencyProvider changes
- Made four `Get` method overloads `virtual` on `DependencyProvider` to enable the override chain:
  - `Get<T>()`
  - `Get(Type type)`
  - `Get<T>(params object[] ctorParams)`
  - `Get(Type type, params object[] ctorParams)`

## Part 2: Multi-Protocol bamsvc Configuration

### 2.1 — BamsvcConfiguration
- File: `bamsvc/BamsvcConfiguration.cs`
- Properties: `ServerName`, `HttpPort`, `HttpsPort`, `TcpPort`, `UdpPort`, `EnableHttp`, `EnableHttps`, `EnableTcp`, `EnableUdp`, `CertificatePath`
- Static `FromArgs(string[])` factory parses CLI arguments
- CLI flags: `--port=`, `--https`, `--https-port=`, `--tcp`, `--udp`, `--tcp-udp`, `--tcp-port=`, `--udp-port=`, `--cert=`

### 2.2 — Program.cs updates
- Replaced manual arg parsing with `BamsvcConfiguration.FromArgs(args)`
- **ComponentRegistry kept** — registers all user account lifecycle services plus application services
- **WebServiceRegistry added** alongside ComponentRegistry — registers only `[WebService]`-adorned service classes
- Protocol decision logic:
  - `EnableHttps` → `WebApplicationBamServer` on HTTPS port
  - `EnableHttp` → `WebApplicationBamServer` on HTTP port (default, always on)
  - `EnableTcp`/`EnableUdp` → `BamServer` with `EnableHttpListener = false`

## Part 3: Actor Registration (bamsvc)

### 3.1 — ActorRegistrationService
- File: `bamsvc/ActorRegistrationService.cs`
- Adorned with `[WebService]`, `[RequiredAccess(BamAccess.Execute)]`, `[RoutePrefix("/api/actors")]`
- Endpoints:
  - `POST /api/actors/register-agent` — `[AnonymousAccess(encryptionRequired: true)]` — registers an autonomous actor
  - `POST /api/actors/register-org` — registers an organization (authenticated)
  - `POST /api/actors/assign-roles` — assigns roles (authenticated)
  - `POST /api/actors/remove-roles` — removes roles (authenticated)
  - `GET /api/actors/{handle}` — `[AnonymousAccess]` — lookup actor by handle

### 3.2 — Request DTOs
- `AgentRegistrationRequest` — Name, Handle, PersonHandle, DeviceHandle
- `OrgRegistrationRequest` — Name, Handle
- `RoleAssignmentRequest` — PersonHandle, RoleNames

### 3.3 — RegistrationService updated
- Added `[WebService]` attribute to existing `RegistrationService` class

## Part 4: svcgateway.io

svcgateway.io consumes bamsvc via the bamtk submodule. Updating bamsvc automatically makes actor registration available. Submodule pointer update propagates the changes.

## Files Changed

| File | Project | Change |
|------|---------|--------|
| `DependencyProvider.cs` | bam.base | Made 4 `Get` overloads `virtual` |
| `WebServiceAttribute.cs` | bam.server | New — marker attribute |
| `ClassNotAWebServiceException.cs` | bam.server | New — exception type |
| `WebServiceRegistry.cs` | bam.server | New — validating registry |
| `BamsvcConfiguration.cs` | bamsvc | New — protocol config model |
| `Program.cs` | bamsvc | Updated — multi-protocol startup |
| `RegistrationService.cs` | bamsvc | Added `[WebService]` |
| `ActorRegistrationService.cs` | bamsvc | New — actor endpoints |
| `AgentRegistrationRequest.cs` | bamsvc | New — DTO |
| `OrgRegistrationRequest.cs` | bamsvc | New — DTO |
| `RoleAssignmentRequest.cs` | bamsvc | New — DTO |
