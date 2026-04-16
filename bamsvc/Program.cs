using Bam.Data.SQLite;
using Bam.Messaging;
using Bam.Presentation;
using Bam.Protocol.Server;
using Bam.Server;
using Bam.Svc;
using Bam.Svc.Pages;
using Bam.UserAccounts;
using Microsoft.AspNetCore.Builder;

var config = BamsvcConfiguration.FromArgs(args);

var options = new BamServerOptions();
options.ServerName = config.ServerName;
options.HttpHostBinding = new HostBinding(config.HttpPort);
options.SessionDatabase = new SQLiteDatabase(new FileInfo("./.bam/bamsvc.sqlite"));

// User account lifecycle services
options.ComponentRegistry.For<IRegistrationValidator>().Use<DefaultRegistrationValidator>();
options.ComponentRegistry.For<IRegistrationNotifier>().Use<EmailRegistrationNotifier>();
options.ComponentRegistry.For<IAccountConfirmation>().Use<DeviceKeyAccountConfirmation>();
options.ComponentRegistry.For<IRoleGroupMapper>().Use<RoleGroupMapper>();
options.ComponentRegistry.For<IUserAccountService>().Use<UserAccountService>();

// Application services
options.ComponentRegistry.For<RegistrationService>().Use<RegistrationService>();
options.ComponentRegistry.For<ActorRegistrationService>().Use<ActorRegistrationService>();

// Web service registry — enforces [WebService] attribute on resolved types
var webServiceRegistry = new WebServiceRegistry();
webServiceRegistry.For<RegistrationService>().Use<RegistrationService>();
webServiceRegistry.For<ActorRegistrationService>().Use<ActorRegistrationService>();
options.ComponentRegistry.Set<WebServiceRegistry>(webServiceRegistry);

WebApplicationBamServer? webServer = null;
BamServer? bamServer = null;

// HTTPS requires WebApplicationBamServer (ASP.NET Core Kestrel handles TLS)
if (config.EnableHttps)
{
    var httpsOptions = new BamServerOptions();
    httpsOptions.ServerName = $"{config.ServerName}-https";
    httpsOptions.HttpHostBinding = new HostBinding(config.HttpsPort);
    httpsOptions.SessionDatabase = options.SessionDatabase;
    httpsOptions.ComponentRegistry.Include(options.ComponentRegistry);

    webServer = new WebApplicationBamServer(httpsOptions);
    webServer.AddRouteHandler<RegistrationService>();
    webServer.AddRouteHandler<ActorRegistrationService>();
    BamPlatform.Servers.Add(webServer);

    webServer.Starting += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer (HTTPS) starting on port {config.HttpsPort}...");
    webServer.Started += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer (HTTPS) started: https://localhost:{config.HttpsPort}");
    webServer.RequestExceptionThrown += (_, _) => Console.Error.WriteLine($"[bamsvc] HTTPS request error: {webServer.LastExceptionMessage}");

    webServer.ConfigureRoutes = app =>
    {
        app.MapPages(new IndexPage(), new RegisterPage(), new RegisterResultPage());
    };

    await webServer.StartAsync();
}

// HTTP (non-TLS) — also uses WebApplicationBamServer for the HTTP endpoint
if (config.EnableHttp)
{
    var httpServer = new WebApplicationBamServer(options);
    httpServer.AddRouteHandler<RegistrationService>();
    httpServer.AddRouteHandler<ActorRegistrationService>();
    BamPlatform.Servers.Add(httpServer);

    httpServer.Starting += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer (HTTP) starting on port {config.HttpPort}...");
    httpServer.Started += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer (HTTP) started: http://localhost:{config.HttpPort}");
    httpServer.RequestExceptionThrown += (_, _) => Console.Error.WriteLine($"[bamsvc] HTTP request error: {httpServer.LastExceptionMessage}");

    httpServer.ConfigureRoutes = app =>
    {
        app.MapPages(new IndexPage(), new RegisterPage(), new RegisterResultPage());
    };

    await httpServer.StartAsync();
}

// TCP and/or UDP — uses BamServer (raw socket listeners)
if (config.EnableTcp || config.EnableUdp)
{
    var bamOptions = new BamServerOptions();
    bamOptions.ServerName = $"{config.ServerName}-tcpudp";
    bamOptions.EnableHttpListener = false;
    bamOptions.TcpPort = config.TcpPort;
    bamOptions.UdpPort = config.UdpPort;
    bamOptions.SessionDatabase = options.SessionDatabase;
    bamOptions.ComponentRegistry.Include(options.ComponentRegistry);

    bamServer = new BamServer(bamOptions);
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

var cts = new CancellationTokenSource();
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
