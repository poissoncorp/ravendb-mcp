using System.Text.Json;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using RavenDB.Mcp.Configuration;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tests;

public sealed class RawHttpDiagnosticsTests
{
    [Fact]
    public async Task CompoundDiagnosticsExposeUnavailableSections()
    {
        await using var server = new FakeRavenHttpServer();
        server.Json("/admin/metrics", """{"requestsPerSecond":7}""");

        using var store = new DocumentStore { Urls = [server.Url] };
        var client = new RavenDbAdminClient(store, Options.Create(new RavenDbOptions { Urls = [server.Url] }));

        var result = await client.GetServerDiagnosticsOverview(CancellationToken.None);

        Assert.True(result.Metrics.GetProperty("available").GetBoolean());
        Assert.Equal(7, result.Metrics.GetProperty("value").GetProperty("requestsPerSecond").GetInt32());
        Assert.False(result.Routes.GetProperty("available").GetBoolean());
        Assert.Contains("/debug/routes", server.Requests);
        Assert.Contains("/admin/metrics", server.Requests);
    }

    [Fact]
    public async Task ArtifactToolsWriteFileReferenceOnly()
    {
        var artifactsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        await using var server = new FakeRavenHttpServer();
        server.Text("/admin/logs/download", "admin log line");

        using var store = new DocumentStore { Urls = [server.Url] };
        var client = new RavenDbAdminClient(
            store,
            Options.Create(new RavenDbOptions
            {
                Urls = [server.Url],
                ArtifactsPath = artifactsPath
            }));

        var result = await client.ExportLogs(null, null, CancellationToken.None);

        Assert.StartsWith(artifactsPath, result.Path);
        Assert.Equal("text/plain", result.ContentType);
        Assert.Equal("admin log line".Length, result.Bytes);
        Assert.Equal("admin log line", await File.ReadAllTextAsync(result.Path));
    }

    [Fact]
    public async Task QueryMetadataOnlyDoesNotReturnRows()
    {
        await using var server = new FakeRavenHttpServer();
        server.Json(
            "/databases/Test/queries",
            """
            {
              "Results": [{"Name":"Ada"}],
              "TotalResults": 1,
              "IndexName": "Auto/Test/ByName",
              "IsStale": false,
              "DurationInMs": 3
            }
            """);

        using var store = new DocumentStore { Urls = [server.Url] };
        var client = new RavenDbAdminClient(store, Options.Create(new RavenDbOptions { Urls = [server.Url] }));

        var result = await client.QueryMetadataOnly("Test", "from Test", 1, CancellationToken.None);

        Assert.Equal(JsonValueKind.Number, result.Metadata.GetProperty("TotalResults").ValueKind);
        Assert.Equal(JsonValueKind.String, result.Metadata.GetProperty("IndexName").ValueKind);
        Assert.False(result.Metadata.TryGetProperty("Results", out _));
    }
}
