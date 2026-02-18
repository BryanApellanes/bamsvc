using Bam.Data.SQLite;
using Bam.DependencyInjection;
using Bam.Net;
using Bam.Presentation;
using Bam.Protocol;
using Bam.Protocol.Data;
using Bam.Protocol.Data.Server.Dao.Repository;
using Bam.Protocol.Profile;
using Bam.Protocol.Profile.Registration;
using Bam.Protocol.Server;
using Bam.Server;
using Bam.Svc;
using Bam.Svc.Pages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

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

// Bootstrap registration dependencies
var registry = new ServiceRegistry();
registry.AddEncryptedProfileRepository("./.bam/profile");
registry.For<IProfileManager>().Use<ProfileManager>();

var profileManager = registry.Get<IProfileManager>();
var sessionRepo = new ServerSessionSchemaRepository
{
    Database = new SQLiteDatabase(new FileInfo("./.bam/bamsvc.sqlite"))
};
sessionRepo.Initialize();
var accountManager = new AccountManager(profileManager, sessionRepo, serverName);

// Parse admin port
var adminPort = 8081;
var adminPortArg = args.FirstOrDefault(a => a.StartsWith("--admin-port="));
if (adminPortArg != null && int.TryParse(adminPortArg["--admin-port=".Length..], out var parsedAdminPort))
{
    adminPort = parsedAdminPort;
}

// Start admin/registration web UI
var adminBuilder = WebApplication.CreateBuilder();
var adminApp = adminBuilder.Build();
adminApp.Urls.Add($"http://localhost:{adminPort}");

adminApp.MapPages(new IndexPage(), new RegisterPage(), new RegisterResultPage());

adminApp.MapPost("/api/register", async (HttpContext ctx) =>
{
    var request = await ctx.Request.ReadFromJsonAsync<PersonRegistrationRequest>();
    if (request == null || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
    {
        return Results.Json(new { error = "FirstName and LastName are required" }, statusCode: 400);
    }

    try
    {
        var registrationData = new PersonRegistrationData
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email ?? string.Empty,
            Phone = request.Phone ?? string.Empty,
            Handle = request.Handle ?? string.Empty,
        };

        var accountData = accountManager.RegisterAccount(registrationData);
        return Results.Json(new { personHandle = accountData.PersonHandle });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 500);
    }
});

adminApp.MapGet("/api/profile/{handle}", (string handle) =>
{
    var profile = profileManager.FindProfileByHandle(handle);
    if (profile == null)
    {
        return Results.Json(new { error = "Profile not found" }, statusCode: 404);
    }

    return Results.Json(new
    {
        profileHandle = profile.ProfileHandle,
        personHandle = profile.PersonHandle,
        name = profile.Name,
        deviceHandle = profile.DeviceHandle,
    });
});

await adminApp.StartAsync();
Console.WriteLine($"[bamsvc] Admin UI: http://localhost:{adminPort}");

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
await adminApp.StopAsync();
await BamPlatform.StopServersAsync();
Console.WriteLine("[bamsvc] Stopped.");
