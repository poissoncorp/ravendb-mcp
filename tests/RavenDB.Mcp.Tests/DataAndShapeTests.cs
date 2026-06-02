using System.Text.Json;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tests;

// Exercises the read-only data tools, the typed control flow, truncation metadata,
// the error-handling convention, and the exact shapes the client parses out of RavenDB.
public sealed class DataAndShapeTests(RavenDbTestFixture fixture)
    : IClassFixture<RavenDbTestFixture>
{
    private RavenDbAdminClient NewClient() => new(fixture.Store);

    [Fact]
    public async Task GetServerInfoReturnsVersion()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var info = await NewClient().GetServerInfo(cts.Token);

        Assert.StartsWith("7.2", info.ProductVersion);
        Assert.True(info.BuildVersion > 0);
        Assert.Equal(JsonValueKind.Object, info.NodeInfo.ValueKind);
    }

    [Fact]
    public async Task RunQueryReturnsRowsAndRejectsMutatingQueries()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var client = NewClient();

        var result = await client.RunQuery(fixture.DatabaseName, "from TestUsers", null, 10, cts.Token);
        Assert.True(result.Result.GetProperty("TotalResults").GetInt32() >= 1);
        var rows = result.Result.GetProperty("Results");
        Assert.Equal(JsonValueKind.Array, rows.ValueKind);
        Assert.NotEmpty(rows.EnumerateArray());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.RunQuery(fixture.DatabaseName, "from TestUsers update { this.Name = 'x' }", null, 10, cts.Token));
    }

    [Fact]
    public async Task GetDocumentReadsExistingAndReportsMissing()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var client = NewClient();

        var query = await client.RunQuery(fixture.DatabaseName, "from TestUsers", null, 1, cts.Token);
        var id = query.Result.GetProperty("Results")[0].GetProperty("@metadata").GetProperty("@id").GetString()!;

        var found = await client.GetDocument(fixture.DatabaseName, id, cts.Token);
        Assert.True(found.Found);
        Assert.Equal(id, found.Id);
        Assert.True(found.Document.TryGetProperty("@metadata", out _));

        var missing = await client.GetDocument(fixture.DatabaseName, "TestUsers/does-not-exist", cts.Token);
        Assert.False(missing.Found);
    }

    [Fact]
    public async Task QueryMetadataOnlyExposesParsedShapeWithoutRows()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var metadata = (await NewClient().QueryMetadataOnly(fixture.DatabaseName, "from TestUsers", 0, cts.Token)).Metadata;

        Assert.Equal(JsonValueKind.Number, metadata.GetProperty("TotalResults").ValueKind);
        Assert.False(metadata.TryGetProperty("Results", out _));
    }

    [Fact]
    public async Task WaitForIndexingCompletesUsingTypedStatistics()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        var result = await NewClient().WaitForIndexing(fixture.DatabaseName, 30, cts.Token);

        Assert.True(result.Completed);
        Assert.Equal(JsonValueKind.Array, result.LastState.GetProperty("Indexes").ValueKind);
    }

    [Fact]
    public async Task TextSampleCarriesTruncationMetadata()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // sample_runtime_events streams over a plain GET (unlike the WebSocket-only *_watch endpoints).
        var sample = await NewClient().SampleRuntimeEvents("gc", 1, cts.Token);

        Assert.Equal(131_072, sample.Limit);
        Assert.False(sample.Truncated);
    }

    [Fact]
    public async Task WatchFeedSamplersConnectOverWebSocket()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var client = NewClient();

        // Both hit WebSocket-only "watch" endpoints; they must connect and return (possibly empty)
        // samples without throwing, and carry the truncation/limit metadata.
        var logs = await client.SampleAdminLogs(2, cts.Token);
        Assert.Equal("admin_logs", logs.Kind);
        Assert.Equal(131_072, logs.Limit);

        var dashboard = await client.SampleClusterDashboard(2, cts.Token);
        Assert.Equal("cluster_dashboard", dashboard.Kind);
        Assert.Equal(131_072, dashboard.Limit);
        // The dashboard pushes widget data within a couple of seconds.
        Assert.False(string.IsNullOrEmpty(dashboard.Sample));
    }

    [Fact]
    public async Task AtomicToolThrowsForMissingDatabase()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            NewClient().GetDatabaseRecord("nonexistent-database-xyz", cts.Token));
    }
}
