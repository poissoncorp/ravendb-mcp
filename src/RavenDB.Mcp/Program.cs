using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Raven.Client.Documents;
using RavenDB.Mcp.Configuration;
using RavenDB.Mcp.RavenDB;

var configPath = GetConfigPath(args);
var builder = Host.CreateApplicationBuilder(args);

// Friendly RAVENDB_* environment variables (the inputs declared in .mcp/server.json) map onto
// RavenDbOptions. Added before --config so an explicit --config file overrides env values.
builder.Configuration.AddInMemoryCollection(MapRavenEnvironment());

if (configPath is not null)
    builder.Configuration.AddJsonFile(Path.GetFullPath(configPath), optional: false, reloadOnChange: false);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddOptions<RavenDbOptions>()
    .Bind(builder.Configuration)
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<RavenDbOptions>, RavenDbOptionsValidator>();

builder.Services.AddSingleton<IDocumentStore>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RavenDbOptions>>().Value;
    return DocumentStoreFactory.Create(options);
});

builder.Services.AddSingleton<RavenDbAdminClient>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithMessageFilters(filters => filters.AddIncomingFilter(next => async (context, cancellationToken) =>
    {
        if (context.JsonRpcMessage is JsonRpcNotification { Method: NotificationMethods.CancelledNotification, Params: { } parameters })
        {
            var cancelled = parameters.Deserialize<CancelledNotificationParams>();
            var logger = context.Services?.GetService<ILoggerFactory>()?.CreateLogger("RavenDB.Mcp.Cancellation");
            logger?.LogInformation("MCP request {RequestId} cancelled. Reason: {Reason}", cancelled?.RequestId, cancelled?.Reason);
        }

        await next(context, cancellationToken);
    }))
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

static IEnumerable<KeyValuePair<string, string?>> MapRavenEnvironment()
{
    var urls = Environment.GetEnvironmentVariable("RAVENDB_URLS");
    if (!string.IsNullOrWhiteSpace(urls))
    {
        var parts = urls.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++)
            yield return new KeyValuePair<string, string?>($"Urls:{i}", parts[i]);
    }

    var certificatePath = Environment.GetEnvironmentVariable("RAVENDB_CERTIFICATE_PATH");
    if (!string.IsNullOrWhiteSpace(certificatePath))
        yield return new KeyValuePair<string, string?>("CertificatePath", certificatePath);

    var certificatePassword = Environment.GetEnvironmentVariable("RAVENDB_CERTIFICATE_PASSWORD");
    if (!string.IsNullOrWhiteSpace(certificatePassword))
        yield return new KeyValuePair<string, string?>("CertificatePassword", certificatePassword);

    var artifactsPath = Environment.GetEnvironmentVariable("RAVENDB_ARTIFACTS_PATH");
    if (!string.IsNullOrWhiteSpace(artifactsPath))
        yield return new KeyValuePair<string, string?>("ArtifactsPath", artifactsPath);
}

static string? GetConfigPath(string[] args)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] == "--config")
        {
            if (i + 1 == args.Length)
                throw new InvalidOperationException("--config requires a file path.");

            return args[i + 1];
        }

        if (args[i].StartsWith("--config=", StringComparison.Ordinal))
            return args[i]["--config=".Length..];
    }

    return null;
}
