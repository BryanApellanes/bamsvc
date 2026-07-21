# RegistrationService Request & Response Flow

## BamServer Flow (TCP / UDP / HTTP via HttpListener)

```
                        ┌─────────────────────────┐
                        │    BAM Protocol Client   │
                        │                          │
                        │  MethodInvocationRequest │
                        │  {                       │
                        │    operationIdentifier:  │
                        │      "...Registration    │
                        │       Service+Register   │
                        │       Person",           │
                        │    arguments: [...]      │
                        │  }                       │
                        └────────┬────────────────┘
                                 │
              ┌──────────────────┼──────────────────┐
              ▼                  ▼                   ▼
        ┌───────────┐    ┌────────────┐     ┌─────────────┐
        │    TCP     │    │    HTTP    │     │     UDP     │
        │ TcpClient  │    │ HttpListener│    │  UdpClient  │
        └─────┬─────┘    └─────┬──────┘     └──────┬──────┘
              │                │                    │
              ▼                ▼                    ▼
        ┌─────────────────────────────────────────────────┐
        │         BamServerContextProvider                 │
        │  CreateServerContext(transport, requestId)       │
        │  → Parse BAM headers + content                  │
        │  → Create IBamServerContext                     │
        └────────────────────┬────────────────────────────┘
                             │
                             ▼
        ┌─────────────────────────────────────────────────┐
        │         BamRequestPipeline.RunPipeline()         │
        │                                                  │
        │  ┌─────────────────────────────────────────┐    │
        │  │ 1. CommandResolver                      │    │
        │  │    Parse OperationIdentifier             │    │
        │  │    "RegistrationService+RegisterPerson" │    │
        │  │    → Command { Type, Method, Args }     │    │
        │  └──────────────────┬──────────────────────┘    │
        │                     │                            │
        │  ┌──────────────────▼──────────────────────┐    │
        │  │ 2. AnonymousAccessHandler               │    │
        │  │    Check [AnonymousAccess] attribute     │    │
        │  │    RegisterPerson → yes, encrypted       │    │
        │  │    GetProfile    → yes, unencrypted      │    │
        │  └──────────────────┬──────────────────────┘    │
        │                     │                            │
        │  ┌──────────────────▼──────────────────────┐    │
        │  │ 3. SessionInitialization                │    │
        │  │    Encrypted anonymous: ECDH session    │    │
        │  │    Plain anonymous: skip                │    │
        │  └──────────────────┬──────────────────────┘    │
        │                     │                            │
        │  ┌──────────────────▼──────────────────────┐    │
        │  │ 4. Decryption (if encrypted)            │    │
        │  │    RequestSecurityValidator.DecryptBody()│    │
        │  └──────────────────┬──────────────────────┘    │
        │                     │                            │
        │  ┌──────────────────▼──────────────────────┐    │
        │  │ 5. ActorResolver (skipped for anon)     │    │
        │  │ 6. Authentication (skipped for anon)    │    │
        │  └──────────────────┬──────────────────────┘    │
        │                     │                            │
        │  ┌──────────────────▼──────────────────────┐    │
        │  │ 7. AuthorizationCalculator              │    │
        │  │    [RequiredAccess(BamAccess.Execute)]   │    │
        │  │    Anonymous → granted Execute           │    │
        │  └──────────────────┬──────────────────────┘    │
        └─────────────────────┼────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────────────┐
        │     BamRequestProcessor.ProcessRequestContext()  │
        │                                                  │
        │  MethodInvocationRequest.ServerInitialize()      │
        │    → ComponentRegistry.Get<RegistrationService>()│
        │    → resolve IAccountManager, IProfileManager    │
        │                                                  │
        │  MethodInvocationRequest.Invoke()                │
        │    → MethodInfo.Invoke(instance, args)           │
        │    → RegistrationService.RegisterPerson(...)     │
        │    → returns AccountData                         │
        └────────────────────┬────────────────────────────┘
                             │
                             ▼
        ┌─────────────────────────────────────────────────┐
        │       ResponseProvider.CreateResponse()          │
        │                                                  │
        │  Serialize AccountData → JSON                   │
        │  Write to transport OutputStream                │
        │  response.Send()                                │
        └────────────────────┬────────────────────────────┘
                             │
                             ▼
                   ┌──────────────────┐
                   │   BAM Protocol   │
                   │    Response      │
                   │                  │
                   │  { personHandle: │
                   │    "abc123" }    │
                   └──────────────────┘
```

## WebApplicationBamServer Flow (ASP.NET Core)

```
                        ┌─────────────────────────┐
                        │       HTTP Client        │
                        └────────┬────────────────┘
                                 │
                ┌────────────────┼────────────────────┐
                │                │                     │
                ▼                ▼                     ▼
     ┌──────────────┐  ┌──────────────────┐  ┌───────────────────┐
     │ POST          │  │ GET              │  │ POST/PUT/...      │
     │ /api/register │  │ /api/profile/{h} │  │ /{**path}         │
     │               │  │                  │  │ (catch-all)       │
     │ REST JSON:    │  │ Route param:     │  │                   │
     │ {             │  │   handle         │  │ BAM Protocol JSON:│
     │  firstName,   │  │                  │  │ MethodInvocation  │
     │  lastName,    │  │                  │  │ Request           │
     │  ...          │  │                  │  │                   │
     │ }             │  │                  │  │                   │
     └──────┬───────┘  └────────┬─────────┘  └─────────┬─────────┘
            │                   │                       │
            ▼                   ▼                       │
     ┌──────────────────────────────────┐               │
     │   PATHWAY A: Direct REST Call    │               │
     │                                  │               │
     │  Deserialize to                  │               │
     │  PersonRegistrationRequest       │               │
     │         │                        │               │
     │         ▼                        │               │
     │  registrationService             │               │
     │    .RegisterPerson(...)          │               │
     │         │                        │               │
     │         ▼                        │               │
     │  registrationService             │               │
     │    .GetProfile(handle)           │               │
     │         │                        │               │
     │  No pipeline.                    │               │
     │  No session/auth/encryption.     │               │
     │  No attribute checks.            │               │
     └───────────────┬──────────────────┘               │
                     │                                  │
                     │                                  ▼
                     │          ┌────────────────────────────────────┐
                     │          │  PATHWAY B: BAM Pipeline           │
                     │          │                                    │
                     │          │  AspNetCoreBamRequest(httpContext)  │
                     │          │    → ReadContentAsync()            │
                     │          │                                    │
                     │          │  AspNetCoreBamServerContext         │
                     │          │    → requestId, bamRequest         │
                     │          │                                    │
                     │          │  BamRequestPipeline.RunPipeline()  │
                     │          │    → Same 7 stages as BamServer    │
                     │          │    → CommandResolver               │
                     │          │    → AnonymousAccess check         │
                     │          │    → Session / Decryption          │
                     │          │    → Authorization                 │
                     │          │                                    │
                     │          │  BamRequestProcessor               │
                     │          │    .ProcessRequestContext()         │
                     │          │    → Resolve RegistrationService   │
                     │          │    → MethodInfo.Invoke()           │
                     │          └───────────────┬────────────────────┘
                     │                          │
                     ▼                          ▼
            ┌─────────────────────────────────────────────┐
            │        ASP.NET Core Response                 │
            │                                              │
            │  httpContext.Response.ContentType =           │
            │    "application/json"                        │
            │                                              │
            │  Pathway A: Results.Json(accountData)        │
            │  Pathway B: encoder.Stringify(result)        │
            │             → WriteAsync to Response         │
            └──────────────────┬──────────────────────────┘
                               │
                               ▼
                     ┌──────────────────┐
                     │   HTTP Response  │
                     │                  │
                     │  200 OK          │
                     │  { personHandle: │
                     │    "abc123" }    │
                     └──────────────────┘
```

## Shared ComponentRegistry (Both Servers)

```
┌────────────────────────────────────────────────────────────────┐
│                    BamServerOptions                             │
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                  ComponentRegistry                        │  │
│  │                                                           │  │
│  │  IServerIdentity ──────► ServerIdentity("bamsvc")         │  │
│  │  ServerSessionSchemaRepository ──► (from SessionDatabase) │  │
│  │  IAccountRepository ──► SessionSchemaAccountRepository    │  │
│  │  IAccountManager ─────► AccountManager                    │  │
│  │  IProfileManager ─────► ProfileManager                    │  │
│  │  ICommandResolver ────► CommandResolver                   │  │
│  │  IBamRequestProcessor ► BamRequestProcessor               │  │
│  │  RegistrationService ─► RegistrationService               │  │
│  │  ...                                                      │  │
│  └──────────────────────────────────────────────────────────┘  │
│         ▲                                    ▲                  │
│         │                                    │                  │
│  ┌──────┴───────────┐            ┌───────────┴──────────┐      │
│  │   BamServer       │            │ WebApplicationBam    │      │
│  │   (TCP/UDP/HTTP)  │            │ Server (ASP.NET)     │      │
│  │                   │            │                      │      │
│  │ BamRequestPipeline│            │ BamRequestPipeline   │      │
│  │ (same options)    │            │ (same options)       │      │
│  └───────────────────┘            └──────────────────────┘      │
└────────────────────────────────────────────────────────────────┘
```
