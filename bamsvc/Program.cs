using Bam.Data.SQLite;
using Bam.Net;
using Bam.Presentation;
using Bam.Protocol.Server;
using Bam.Server;
using Bam.Svc;
using Bam.Svc.Pages;
using Microsoft.AspNetCore.Builder;

var serverName = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "bamsvc";
var httpPort = 8080;
var enableTcpUdp = args.Contains("--tcp-udp");

var portArg = args.FirstOrDefault(a => a.StartsWith("--port="));
if (portArg != null && int.TryParse(portArg["--port=".Length..], out var parsedPort))
{
    httpPort = parsedPort;
}

var options = new BamServerOptions();
options.ServerName = serverName;
options.HttpHostBinding = new HostBinding(httpPort);
options.SessionDatabase = new SQLiteDatabase(new FileInfo("./.bam/bamsvc.sqlite"));

// Application service — resolved by DI via IAccountManager + IProfileManager
options.ComponentRegistry.For<RegistrationService>().Use<RegistrationService>();

var webServer = new WebApplicationBamServer(options);
webServer.AddRouteHandler<RegistrationService>();
BamPlatform.Servers.Add(webServer);

webServer.Starting += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer starting on port {httpPort}...");
webServer.Started += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer started: http://localhost:{httpPort}");
webServer.RequestExceptionThrown += (_, _) => Console.Error.WriteLine($"[bamsvc] Request error: {webServer.LastExceptionMessage}");

webServer.ConfigureRoutes = app =>
{
    app.MapPages(new IndexPage(), new RegisterPage(), new RegisterResultPage());
};

await webServer.StartAsync();

BamServer? bamServer = null;
if (enableTcpUdp)
{
    options.EnableHttpListener = false;
    bamServer = new BamServer(options);
    await bamServer.StartAsync();
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
