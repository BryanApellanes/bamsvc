using Bam.Net;
using Bam.Protocol.Server;
using Bam.Server;

var serverName = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "bamsvc";
var httpPort = 8080;
var enableTcpUdp = args.Contains("--tcp-udp");

// Parse optional port
var portArg = args.FirstOrDefault(a => a.StartsWith("--port="));
if (portArg != null && int.TryParse(portArg["--port=".Length..], out var parsedPort))
{
    httpPort = parsedPort;
}

// Start WebApplicationBamServer for HTTP protocol handling
var webServer = await BamPlatform.CreateWebApplicationServerAsync(serverName, httpPort);
webServer.Starting += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer starting on port {httpPort}...");
webServer.Started += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer started: http://localhost:{httpPort}");
webServer.RequestExceptionThrown += (_, _) => Console.Error.WriteLine($"[bamsvc] Request error: {webServer.LastExceptionMessage}");
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
