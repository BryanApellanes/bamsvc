using System.Text.Json;
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
var registrationService = new RegistrationService(accountManager, profileManager);

// Create WebApplicationBamServer with ConfigureRoutes
var webServer = await BamPlatform.CreateWebApplicationServerAsync(serverName, httpPort);
webServer.Starting += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer starting on port {httpPort}...");
webServer.Started += (_, _) => Console.WriteLine($"[bamsvc] WebApplicationBamServer started: http://localhost:{httpPort}");
webServer.RequestExceptionThrown += (_, _) => Console.Error.WriteLine($"[bamsvc] Request error: {webServer.LastExceptionMessage}");

webServer.ConfigureRoutes = app =>
{
    // HTML pages
    app.MapPages(new IndexPage(), new RegisterPage(), new RegisterResultPage());

    // REST registration API
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

    // MethodInvocationRequest endpoint for registration
    app.MapPost("/bam/invoke/register", async (HttpContext ctx) =>
    {
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        MethodInvocationRequest? invocationRequest;
        try
        {
            invocationRequest = await ctx.Request.ReadFromJsonAsync<MethodInvocationRequest>(jsonOptions);
        }
        catch (JsonException ex)
        {
            return Results.Json(new { error = $"Invalid request body: {ex.Message}" }, statusCode: 400);
        }

        if (invocationRequest == null || string.IsNullOrEmpty(invocationRequest.OperationIdentifier))
        {
            return Results.Json(new { error = "operationIdentifier is required" }, statusCode: 400);
        }

        var parts = invocationRequest.OperationIdentifier.Split('+', ',');
        if (parts.Length < 2)
        {
            return Results.Json(new { error = "Invalid operationIdentifier format" }, statusCode: 400);
        }

        var typeName = parts[0].Trim();
        var methodName = parts[1].Trim();

        if (typeName != typeof(RegistrationService).FullName)
        {
            return Results.Json(new { error = "Only RegistrationService methods may be invoked" }, statusCode: 403);
        }

        string? ArgValue(string name)
        {
            var arg = invocationRequest.Arguments?.FirstOrDefault(a =>
                string.Equals(a.ParameterName, name, StringComparison.OrdinalIgnoreCase));
            if (arg == null) return null;
            return arg.Value is JsonElement je ? je.GetString() : arg.Value?.ToString();
        }

        try
        {
            object? result = methodName switch
            {
                nameof(RegistrationService.RegisterPerson) => registrationService.RegisterPerson(
                    ArgValue("firstName") ?? throw new ArgumentException("firstName is required"),
                    ArgValue("lastName") ?? throw new ArgumentException("lastName is required"),
                    ArgValue("email"),
                    ArgValue("phone"),
                    ArgValue("handle")),
                nameof(RegistrationService.GetProfile) => registrationService.GetProfile(
                    ArgValue("handle") ?? throw new ArgumentException("handle is required")),
                _ => throw new InvalidOperationException($"Unknown method: {methodName}")
            };

            return Results.Json(result);
        }
        catch (ArgumentException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: 400);
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: 500);
        }
    });
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
