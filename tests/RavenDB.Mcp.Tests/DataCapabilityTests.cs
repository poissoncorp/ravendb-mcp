using System.Text.Json;
using Raven.Client.Documents.Session;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tests;

// Read-path coverage for capabilities the suite didn't exercise: counters, time-series,
// compare-exchange, revisions/conflicts, AI-agents envelope, the remaining sample feeds.
public sealed class DataCapabilityTests(RavenDbTestFixture fixture)
    : IClassFixture<RavenDbTestFixture>
{
    private RavenDbAdminClient NewClient() => new(fixture.Store);

    private async Task<string> SeededDocumentIdAsync(RavenDbAdminClient client, CancellationToken cancellationToken)
    {
        var query = await client.RunQuery(fixture.DatabaseName, "from TestUsers", null, 1, cancellationToken);
        return query.Result.GetProperty("Results")[0].GetProperty("@metadata").GetProperty("@id").GetString()!;
    }

    [Fact]
    public async Task GetAiAgentsReportsAvailabilityEnvelope()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var result = await NewClient().GetAiAgents(fixture.DatabaseName, null, cts.Token);

        var available = result.GetProperty("available");
        Assert.True(available.ValueKind is JsonValueKind.True or JsonValueKind.False);
        if (available.GetBoolean())
            Assert.Equal(JsonValueKind.Object, result.GetProperty("value").ValueKind);
        else
            Assert.False(string.IsNullOrEmpty(result.GetProperty("error").GetString()));
    }

    [Fact]
    public async Task CountersAndCompareExchangeReturnWellFormedShapeWhenUnseeded()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var client = NewClient();
        var id = await SeededDocumentIdAsync(client, cts.Token);

        var counters = await client.GetDocumentCounters(fixture.DatabaseName, id, cts.Token);
        Assert.Equal(JsonValueKind.Object, counters.ValueKind);

        var compareExchange = await client.GetCompareExchange(fixture.DatabaseName, null, null, cts.Token);
        Assert.Equal(JsonValueKind.Object, compareExchange.ValueKind);
    }

    [Fact]
    public async Task RevisionsAndConflictsReturnSafeShapeForPlainDocument()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var client = NewClient();
        var id = await SeededDocumentIdAsync(client, cts.Token);

        var revisions = await client.GetDocumentRevisions(fixture.DatabaseName, id, null, null, cts.Token);
        Assert.Equal(JsonValueKind.Object, revisions.Revisions.ValueKind); // revisions disabled -> empty, no throw

        var conflicts = await client.GetDocumentConflicts(fixture.DatabaseName, id, cts.Token);
        Assert.Equal(JsonValueKind.Object, conflicts.Conflicts.ValueKind); // no conflicts -> empty, no throw
    }

    [Fact]
    public async Task SampleAllocationsAndThreadContentionCarryTruncationMetadata()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var client = NewClient();

        var allocations = await client.SampleAllocations(1, cts.Token);
        Assert.Equal("allocations", allocations.Kind);
        Assert.Equal(131_072, allocations.Limit);

        var contention = await client.SampleThreadContention(1, cts.Token);
        Assert.Equal("thread_contention", contention.Kind);
        Assert.Equal(131_072, contention.Limit);
    }

    [Fact]
    public async Task CountersAndTimeSeriesRoundTripOnIsolatedDatabase()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var databaseName = $"RavenDB_Mcp_Data_{Guid.NewGuid():N}";
        await fixture.Store.Maintenance.Server.SendAsync(
            new CreateDatabaseOperation(new DatabaseRecord(databaseName)), cts.Token);

        try
        {
            using (var session = fixture.Store.OpenAsyncSession(databaseName))
            {
                await session.StoreAsync(new Sensor("rack-1"), "sensors/1", cts.Token);
                session.CountersFor("sensors/1").Increment("Likes", 5);
                session.TimeSeriesFor("sensors/1", "Heartrate").Append(DateTime.UtcNow, 99, "watch");
                await session.SaveChangesAsync(cts.Token);
            }

            var client = new RavenDbAdminClient(fixture.Store);

            var counters = await client.GetDocumentCounters(databaseName, "sensors/1", cts.Token);
            Assert.Equal(JsonValueKind.Object, counters.ValueKind);
            Assert.Contains("Likes", counters.GetRawText()); // the seeded counter surfaces

            var timeSeries = await client.GetDocumentTimeSeries(databaseName, "sensors/1", "Heartrate", null, null, cts.Token);
            Assert.Equal(JsonValueKind.Object, timeSeries.ValueKind);
            Assert.Contains("watch", timeSeries.GetRawText()); // the appended entry (its tag) surfaces
        }
        finally
        {
            await fixture.Store.Maintenance.Server.SendAsync(
                new DeleteDatabasesOperation(databaseName, hardDelete: true), cts.Token);
        }
    }

    [Fact]
    public async Task CompareExchangeStartsWithFilterReturnsOnlyMatchingKeys()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var databaseName = $"RavenDB_Mcp_Cmpxchg_{Guid.NewGuid():N}";
        await fixture.Store.Maintenance.Server.SendAsync(
            new CreateDatabaseOperation(new DatabaseRecord(databaseName)), cts.Token);

        try
        {
            using (var session = fixture.Store.OpenAsyncSession(new SessionOptions
            {
                Database = databaseName,
                TransactionMode = TransactionMode.ClusterWide
            }))
            {
                session.Advanced.ClusterTransaction.CreateCompareExchangeValue("users/1", "owner-a");
                session.Advanced.ClusterTransaction.CreateCompareExchangeValue("orders/9", "owner-b");
                await session.SaveChangesAsync(cts.Token);
            }

            var client = new RavenDbAdminClient(fixture.Store);

            var all = await client.GetCompareExchange(databaseName, null, null, cts.Token);
            Assert.True(all.TryGetProperty("users/1", out _));
            Assert.True(all.TryGetProperty("orders/9", out _));

            var usersOnly = await client.GetCompareExchange(databaseName, "users", null, cts.Token);
            Assert.True(usersOnly.TryGetProperty("users/1", out _));
            Assert.False(usersOnly.TryGetProperty("orders/9", out _)); // startsWith prefix is honoured
        }
        finally
        {
            await fixture.Store.Maintenance.Server.SendAsync(
                new DeleteDatabasesOperation(databaseName, hardDelete: true), cts.Token);
        }
    }

    private sealed record Sensor(string Location);
}
