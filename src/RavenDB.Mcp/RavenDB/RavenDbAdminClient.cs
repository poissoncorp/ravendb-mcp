using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Operations;
using Raven.Client.Http;
using Raven.Client.ServerWide.Operations;
using RavenDB.Mcp.Configuration;
using Sparrow.Json;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient(
    IDocumentStore store,
    IOptions<RavenDbOptions>? options = null)
{
    private static readonly JsonSerializerOptions RavenDbJsonOptions = new()
    {
        IncludeFields = true
    };

    private readonly X509Certificate2? clientCertificate = LoadClientCertificate(options?.Value);
    private readonly HttpClient http = CreateHttpClient(options?.Value);

    // Raw HTTP diagnostic routes target the configured primary node by design for v1.
    // Typed Client-API calls still fail over across the cluster; raw debug/admin routes do not.
    // TODO: topology-aware raw scans (per-node fan-out) are deferred — see ADR-0006 / the distribution+hardening plan.
    private readonly string serverUrl = (options?.Value.Urls.FirstOrDefault() ?? store.Urls.First()).TrimEnd('/');
    private readonly string artifactsPath = string.IsNullOrWhiteSpace(options?.Value.ArtifactsPath)
        ? Path.Combine(Path.GetTempPath(), "ravendb-mcp-artifacts")
        : options.Value.ArtifactsPath;

    private async Task<JsonElement> GetDatabaseRecordJson(string databaseName, CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);

        var record = await store.Maintenance.Server.SendAsync(
            new GetDatabaseRecordOperation(databaseName),
            cancellationToken);

        if (record is null)
            throw new InvalidOperationException($"Database '{databaseName}' was not found.");

        // DatabaseRecord keeps most payload data in fields; redact connection-string secrets (ADR-0011).
        return RedactSecrets(ToJson(record));
    }

    private MaintenanceOperationExecutor ForDatabase(string databaseName)
    {
        ValidateDatabaseName(databaseName);
        return store.Maintenance.ForDatabase(databaseName);
    }

    // Connection-string sections embed secrets (SQL passwords, S3/Azure/GCP keys, SAS tokens,
    // Elasticsearch/AI API keys). Mask any value whose key is a known secret field before it leaves
    // the server (ADR-0011). Exact key match (case-insensitive) to avoid over-redacting unrelated fields.
    private static readonly HashSet<string> SecretKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "ApiKey", "Secret", "SecretKey", "AccessKey", "AccountKey",
        "AwsSecretKey", "AwsAccessKey", "SasToken", "ConnectionString", "GoogleCredentialsJson"
    };

    private const string RedactedValue = "***redacted***";

    private static JsonElement RedactSecrets(JsonElement element)
    {
        var node = JsonNode.Parse(element.GetRawText());
        RedactNode(node);
        return JsonSerializer.SerializeToElement(node);
    }

    private static void RedactNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj.ToArray())
                {
                    if (property.Value is JsonValue && SecretKeys.Contains(property.Key))
                        obj[property.Key] = RedactedValue;
                    else
                        RedactNode(property.Value);
                }
                break;
            case JsonArray array:
                foreach (var item in array)
                    RedactNode(item);
                break;
        }
    }

    private Task<T> ExecuteServerCommand<T>(RavenCommand<T> command, CancellationToken cancellationToken)
    {
        return ExecuteServerCommand(store, command, cancellationToken);
    }

    private static Task<T> ExecuteServerCommand<T>(
        IDocumentStore targetStore,
        RavenCommand<T> command,
        CancellationToken cancellationToken)
    {
        return targetStore.Maintenance.Server.SendAsync(
            new ServerCommandOperation<T>(command),
            cancellationToken);
    }

    private Task<JsonElement> GetServerJson(
        string path,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        return GetJson(BuildServerUrl(path, query), cancellationToken);
    }

    private Task<JsonElement> GetDatabaseJson(
        string databaseName,
        string path,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        ValidateDatabaseName(databaseName);
        return GetJson(BuildDatabaseUrl(databaseName, path, query), cancellationToken);
    }

    private Task<string> GetServerText(string path, CancellationToken cancellationToken)
    {
        return GetText(BuildServerUrl(path), cancellationToken);
    }

    private Task<string> GetDatabaseText(
        string databaseName,
        string path,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        ValidateDatabaseName(databaseName);
        return GetText(BuildDatabaseUrl(databaseName, path, query), cancellationToken);
    }

    private async Task<JsonElement> GetJson(string url, CancellationToken cancellationToken)
    {
        var content = await GetText(url, cancellationToken);
        return JsonSerializer.Deserialize<JsonElement>(content);
    }

    private async Task<string> GetText(string url, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(url, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GET {url} failed with {(int)response.StatusCode}: {content}");

        return content;
    }

    private const int SampleCharLimit = 131_072;

    private async Task<TextSample> GetServerTextSample(
        string path,
        int seconds,
        CancellationToken cancellationToken)
    {
        var sampleSeconds = Math.Clamp(seconds, 1, 30);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(sampleSeconds));

        var result = new StringBuilder();
        var truncated = false;

        try
        {
            using var response = await http.GetAsync(
                BuildServerUrl(path),
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);

            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var reader = new StreamReader(stream);
            var buffer = new char[4096];

            while (!timeout.Token.IsCancellationRequested)
            {
                if (result.Length >= SampleCharLimit)
                {
                    truncated = true;
                    break;
                }

                var read = await reader.ReadAsync(buffer, timeout.Token);
                if (read == 0)
                    break;

                result.Append(buffer, 0, read);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }

        return new TextSample(result.ToString(), truncated, SampleCharLimit);
    }

    private readonly record struct TextSample(string Text, bool Truncated, int Limit);

    private static JsonElement ToJson<T>(T value)
    {
        return JsonSerializer.SerializeToElement(value, RavenDbJsonOptions);
    }

    private static X509Certificate2? LoadClientCertificate(RavenDbOptions? options)
    {
        return options is null || string.IsNullOrWhiteSpace(options.CertificatePath)
            ? null
            : DocumentStoreFactory.LoadCertificate(options);
    }

    private static HttpClient CreateHttpClient(RavenDbOptions? options)
    {
        var certificate = LoadClientCertificate(options);
        if (certificate is null)
            return new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(certificate);
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
    }

    private static string ToWebSocketUrl(string httpUrl)
    {
        return httpUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? string.Concat("wss://", httpUrl.AsSpan("https://".Length))
            : string.Concat("ws://", httpUrl.AsSpan("http://".Length));
    }

    // RavenDB's live "watch" feeds (admin logs, cluster dashboard) are WebSocket endpoints, not
    // plain GETs. Connect, collect frames for the sample window (or until the size cap), report
    // truncation. Mirrors GetServerTextSample's bounds and TextSample shape.
    private async Task<TextSample> GetServerWebSocketSample(
        string path,
        int seconds,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        var sampleSeconds = Math.Clamp(seconds, 1, 30);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(sampleSeconds));

        var url = ToWebSocketUrl(BuildServerUrl(path, query));
        using var socket = new ClientWebSocket();
        if (clientCertificate is not null)
            socket.Options.ClientCertificates.Add(clientCertificate);

        var result = new StringBuilder();
        var truncated = false;
        var buffer = new byte[8192];

        try
        {
            await socket.ConnectAsync(new Uri(url), timeout.Token);

            while (!timeout.Token.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                if (result.Length >= SampleCharLimit)
                {
                    truncated = true;
                    break;
                }

                var received = await socket.ReceiveAsync(buffer, timeout.Token);
                if (received.MessageType == WebSocketMessageType.Close)
                    break;

                result.Append(Encoding.UTF8.GetString(buffer, 0, received.Count));
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }
        catch (WebSocketException)
        {
            // Connection closed/aborted mid-sample; return whatever we collected.
        }
        finally
        {
            socket.Abort();
        }

        return new TextSample(result.ToString(), truncated, SampleCharLimit);
    }

    private string BuildServerUrl(
        string path,
        params (string Name, string? Value)[] query)
    {
        return WithQuery($"{serverUrl}{path}", query);
    }

    private string BuildDatabaseUrl(
        string databaseName,
        string path,
        params (string Name, string? Value)[] query)
    {
        return WithQuery($"{serverUrl}/databases/{Uri.EscapeDataString(databaseName)}{path}", query);
    }

    private static string WithQuery(string url, params (string Name, string? Value)[] query)
    {
        var values = query
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .Select(item => $"{Uri.EscapeDataString(item.Name)}={Uri.EscapeDataString(item.Value!)}")
            .ToArray();

        return values.Length == 0 ? url : $"{url}?{string.Join('&', values)}";
    }

    private static JsonElement SelectRecordProperties(JsonElement record, params string[] nameFragments)
    {
        var values = new Dictionary<string, JsonElement>();

        foreach (var property in record.EnumerateObject())
        {
            if (nameFragments.Any(fragment => property.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                values[property.Name] = property.Value.Clone();
        }

        return ToJson(values);
    }

    private static Dictionary<string, JsonElement> SelectProperties(JsonElement value, params string[] names)
    {
        var selected = new Dictionary<string, JsonElement>();

        foreach (var name in names)
        {
            if (value.TryGetProperty(name, out var property))
                selected[name] = property.Clone();
        }

        return selected;
    }

    private static void ValidateDatabaseName(string databaseName)
    {
        ValidateName(databaseName, "Database name", nameof(databaseName));
    }

    private static void ValidateName(string value, string label, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{label} is required.", parameterName);
    }

    private sealed class ServerCommandOperation<T>(RavenCommand<T> command) : IServerOperation<T>
    {
        public RavenCommand<T> GetCommand(DocumentConventions conventions, JsonOperationContext context)
        {
            return command;
        }
    }
}
