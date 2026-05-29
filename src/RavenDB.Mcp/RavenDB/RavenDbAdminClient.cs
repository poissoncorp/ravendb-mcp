using System.Text;
using System.Text.Json;
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

    private readonly RavenDbOptions? configuredOptions = options?.Value;
    private readonly HttpClient http = CreateHttpClient(options?.Value);
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

        // DatabaseRecord keeps most payload data in fields.
        return ToJson(record);
    }

    private MaintenanceOperationExecutor ForDatabase(string databaseName)
    {
        ValidateDatabaseName(databaseName);
        return store.Maintenance.ForDatabase(databaseName);
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

    private async Task<string> GetServerTextSample(
        string path,
        int seconds,
        CancellationToken cancellationToken)
    {
        var sampleSeconds = Math.Clamp(seconds, 1, 30);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(sampleSeconds));

        var result = new StringBuilder();

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

            while (!timeout.Token.IsCancellationRequested && result.Length < 131_072)
            {
                var read = await reader.ReadAsync(buffer, timeout.Token);
                if (read == 0)
                    break;

                result.Append(buffer, 0, read);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }

        return result.ToString();
    }

    private static JsonElement ToJson<T>(T value)
    {
        return JsonSerializer.SerializeToElement(value, RavenDbJsonOptions);
    }

    private static HttpClient CreateHttpClient(RavenDbOptions? options)
    {
        if (options is null || string.IsNullOrWhiteSpace(options.CertificatePath))
            return new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(DocumentStoreFactory.LoadCertificate(options)!);
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
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
