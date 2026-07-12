using Bam.Data.Objects;
using Bam.Identity;
using Bam.Identity.Clients;
using Bam.Protocol;
using Bam.Protocol.Client;
using Bam.Protocol.Server;
using Bam.Server;
using Bam.Svc;
using Microsoft.AspNetCore.Builder;

string serverName = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "bamsvc";
int httpPort = 8080;
bool enableTcpUdp = args.Contains("--tcp-udp");

// Parse optional port
string? portArg = args.FirstOrDefault(a => a.StartsWith("--port="));
if (portArg != null && int.TryParse(portArg["--port=".Length..], out int parsedPort))
{
    httpPort = parsedPort;
}

BamServerOptions options = new BamServerOptions();
options.ServerName = serverName;
options.HttpHostBinding = new HostBinding(httpPort);

// bamid's TCP endpoint. bamid derives this port deterministically from its server name
// ("bamid") via UseNameBasedPort — observed to be 24515 as long as bamid's server name stays
// "bamid". Override via --bamid-tcp-port= if bamid is deployed under a different name/port.
int bamidTcpPort = 24515;
string? bamidTcpPortArg = args.FirstOrDefault(a => a.StartsWith("--bamid-tcp-port="));
if (bamidTcpPortArg != null && int.TryParse(bamidTcpPortArg["--bamid-tcp-port=".Length..], out int parsedBamidTcpPort))
{
    bamidTcpPort = parsedBamidTcpPort;
}

BamClient bamidClient = new BamClient(new JsonObjectDataEncoder(), BamClient.DefaultHttpBaseAddress, new HostBinding("localhost", bamidTcpPort));
IRegistrationService registrationService = new RegistrationServiceClient(bamidClient, BamClientProtocols.Tcp);
IdentityGatewayRoutes identityGatewayRoutes = new IdentityGatewayRoutes(registrationService);

WebApplicationBamServer webServer = new WebApplicationBamServer(options);
BamPlatform.Servers.Add(webServer);

webServer.Starting += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer starting on port {httpPort}...");
webServer.Started += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer started: http://localhost:{httpPort}");
webServer.RequestExceptionThrown += (_, _) => Console.Error.WriteLine($"[bamsvc] Request error: {webServer.LastExceptionMessage}");

webServer.ConfigureRoutes = (WebApplication app) =>
{
    identityGatewayRoutes.MapRoutes(app);
};

await webServer.StartAsync();

// Optionally start BamServer for TCP/UDP
BamServer? bamServer = null;
if (enableTcpUdp)
{
    bamServer = new BamServerBuilder()
        .ServerName(serverName)
        .UseNameBasedPort()
        .OnStarted((_, _) => Console.WriteLine($"[bamsvc] BamServer started: TCP={bamServer!.TcpPort}, UDP={bamServer.UdpPort}"))
        .Build();
    await bamServer.StartAsync();
}

Console.WriteLine("[bamsvc] Press Ctrl+C to stop.");

// Wait for shutdown signal
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
