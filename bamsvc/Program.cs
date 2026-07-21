using Bam.Data.Objects;
using Bam.Data.SQLite;
using Bam.Identity;
using Bam.Identity.Clients;
using Bam.Protocol;
using Bam.Protocol.Client;
using Bam.Protocol.Server;
using Bam.Server;
using Bam.Svc;
using Bam.UserAccounts;

BamsvcConfiguration config = BamsvcConfiguration.FromArgs(args);

// bamid-backed identity: registration/profile calls go to bamid over TCP rather than
// hosting identity logic in-process (see BamsvcConfiguration.BamidTcpPort).
BamClient bamidClient = new BamClient(new JsonObjectDataEncoder(), BamClient.DefaultHttpBaseAddress, new HostBinding("localhost", config.BamidTcpPort));
IRegistrationService registrationService = new RegistrationServiceClient(bamidClient, BamClientProtocols.Tcp);

BamServerOptions options = new BamServerOptions();
options.ServerName = config.ServerName;
options.HttpHostBinding = new HostBinding(config.HttpPort);
options.SessionDatabase = new SQLiteDatabase(new FileInfo("./.bam/bamsvc.sqlite"));

// User account lifecycle services
options.ComponentRegistry.For<IRegistrationValidator>().Use<DefaultRegistrationValidator>();
options.ComponentRegistry.For<IRegistrationNotifier>().Use<EmailRegistrationNotifier>();
options.ComponentRegistry.For<IAccountConfirmation>().Use<DeviceKeyAccountConfirmation>();
options.ComponentRegistry.For<IRoleGroupMapper>().Use<RoleGroupMapper>();
options.ComponentRegistry.For<IUserAccountService>().Use<UserAccountService>();
options.ComponentRegistry.For<IRegistrationService>().UseSingleton(registrationService);

// Application services routed through the pipeline
options.ComponentRegistry.For<ActorRegistrationService>().Use<ActorRegistrationService>();
options.ComponentRegistry.For<IdentityGateway>().Use<IdentityGateway>();

// Web service registry — enforces [WebService] attribute on types resolved for remote execution
WebServiceRegistry webServiceRegistry = new WebServiceRegistry();
webServiceRegistry.For<ActorRegistrationService>().Use<ActorRegistrationService>();
webServiceRegistry.For<IdentityGateway>().Use<IdentityGateway>();
options.ComponentRegistry.Set<WebServiceRegistry>(webServiceRegistry);

WebApplicationBamServer? httpsServer = null;
WebApplicationBamServer? httpServer = null;
BamServer? bamServer = null;

// HTTPS requires WebApplicationBamServer (ASP.NET Core Kestrel handles TLS)
if (config.EnableHttps)
{
    BamServerOptions httpsOptions = new BamServerOptions();
    httpsOptions.ServerName = $"{config.ServerName}-https";
    httpsOptions.HttpHostBinding = new HostBinding(config.HttpsPort);
    httpsOptions.SessionDatabase = options.SessionDatabase;
    httpsOptions.ComponentRegistry.Include(options.ComponentRegistry);

    httpsServer = new WebApplicationBamServer(httpsOptions);
    httpsServer.AddRouteHandler<ActorRegistrationService>();
    httpsServer.AddRouteHandler<IdentityGateway>();
    BamPlatform.Servers.Add(httpsServer);

    httpsServer.Starting += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer (HTTPS) starting on port {config.HttpsPort}...");
    httpsServer.Started += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer (HTTPS) started: https://localhost:{config.HttpsPort}");
    httpsServer.RequestExceptionThrown += (_, _) => Console.Error.WriteLine($"[bamsvc] HTTPS request error: {httpsServer.LastExceptionMessage}");

    await httpsServer.StartAsync();
}

// HTTP (non-TLS) — also uses WebApplicationBamServer for the HTTP endpoint
if (config.EnableHttp)
{
    httpServer = new WebApplicationBamServer(options);
    httpServer.AddRouteHandler<ActorRegistrationService>();
    httpServer.AddRouteHandler<IdentityGateway>();
    BamPlatform.Servers.Add(httpServer);

    httpServer.Starting += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer (HTTP) starting on port {config.HttpPort}...");
    httpServer.Started += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer (HTTP) started: http://localhost:{config.HttpPort}");
    httpServer.RequestExceptionThrown += (_, _) => Console.Error.WriteLine($"[bamsvc] HTTP request error: {httpServer.LastExceptionMessage}");

    await httpServer.StartAsync();
}

// TCP and/or UDP — uses BamServer (raw socket listeners)
if (config.EnableTcp || config.EnableUdp)
{
    BamServerOptions tcpUdpOptions = new BamServerOptions();
    tcpUdpOptions.ServerName = $"{config.ServerName}-tcpudp";
    tcpUdpOptions.EnableHttpListener = false;
    tcpUdpOptions.TcpPort = config.TcpPort;
    tcpUdpOptions.UdpPort = config.UdpPort;
    tcpUdpOptions.SessionDatabase = options.SessionDatabase;
    tcpUdpOptions.ComponentRegistry.Include(options.ComponentRegistry);

    bamServer = new BamServer(tcpUdpOptions);
    BamPlatform.Servers.Add(bamServer);
    await bamServer.StartAsync();

    if (config.EnableTcp)
    {
        Console.WriteLine($"[bamsvc] BamServer TCP listening on port {config.TcpPort}");
    }
    if (config.EnableUdp)
    {
        Console.WriteLine($"[bamsvc] BamServer UDP listening on port {config.UdpPort}");
    }
}

Console.WriteLine("[bamsvc] Press Ctrl+C to stop.");

CancellationTokenSource cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
}

Console.WriteLine("[bamsvc] Shutting down...");
await BamPlatform.StopServersAsync();
Console.WriteLine("[bamsvc] Stopped.");
