using Bam.Data.SQLite;
using Bam.Net;
using Bam.Presentation;
using Bam.Protocol.Server;
using Bam.Server;
using Bam.Svc;
using Bam.Svc.Pages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

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
BamPlatform.Servers.Add(webServer);

webServer.Starting += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer starting on port {httpPort}...");
webServer.Started += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer started: http://localhost:{httpPort}");
webServer.RequestExceptionThrown += (_, _) => Console.Error.WriteLine($"[bamsvc] Request error: {webServer.LastExceptionMessage}");

// Resolve RegistrationService for REST endpoints (same instance the pipeline uses)
var registrationService = options.ComponentRegistry.Get<RegistrationService>();

webServer.ConfigureRoutes = app =>
{
    app.MapPages(new IndexPage(), new RegisterPage(), new RegisterResultPage());

    app.MapPost("/api/register", async (HttpContext ctx) =>
    {
        var request = await ctx.Request.ReadFromJsonAsync<PersonRegistrationRequest>();
        if (request == null || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return Results.Json(new { error = "FirstName and LastName are required" }, statusCode: 400);
        }

        try
        {
            var accountData = registrationService.RegisterPerson(
                request.FirstName, request.LastName, request.Email, request.Phone, request.Handle);
            return Results.Json(new { personHandle = accountData.PersonHandle });
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: 500);
        }
    });

    app.MapGet("/api/profile/{handle}", (string handle) =>
    {
        var result = registrationService.GetProfile(handle);
        if (result == null)
        {
            return Results.Json(new { error = "Profile not found" }, statusCode: 404);
        }

        return Results.Json(result);
    });
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
