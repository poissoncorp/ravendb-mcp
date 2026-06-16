using System.Text.Json;
using Raven.Client.Documents.Operations.ConnectionStrings;
using Raven.Client.Documents.Operations.ETL.SQL;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tests;

public sealed class RavenDbAdminClientTests(RavenDbTestFixture fixture)
    : IClassFixture<RavenDbTestFixture>
{
    [Fact]
    public async Task ReadsClusterNodesAndDatabaseRecord()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var client = new RavenDbAdminClient(fixture.Store);

        var clusterNodes = await client.GetClusterNodes(timeout.Token);
        Assert.NotEmpty(clusterNodes.Server.ProductVersion);
        Assert.True(clusterNodes.Server.BuildVersion > 0);
        Assert.NotEmpty(clusterNodes.Cluster.Nodes);

        var databases = await client.ListDatabases(timeout.Token);
        Assert.Contains(fixture.DatabaseName, databases.Databases);

        var databaseRecord = await client.GetDatabaseRecord(
            fixture.DatabaseName,
            timeout.Token);

        Assert.Equal(fixture.DatabaseName, databaseRecord.DatabaseName);
        Assert.Equal(fixture.DatabaseName, databaseRecord.Record.GetProperty("DatabaseName").GetString());
    }

    [Fact]
    public async Task RedactsConnectionStringSecretsInDatabaseRecord()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        const string secret = "Sup3rSecretEtlPassword!";

        // Isolated database so the secret never lingers in the shared fixture state.
        var databaseName = $"RavenDB_Mcp_Redaction_{Guid.NewGuid():N}";
        await fixture.Store.Maintenance.Server.SendAsync(
            new CreateDatabaseOperation(new DatabaseRecord(databaseName)), timeout.Token);

        try
        {
            await fixture.Store.Maintenance.ForDatabase(databaseName).SendAsync(
                new PutConnectionStringOperation<SqlConnectionString>(new SqlConnectionString
                {
                    Name = "reporting-sql",
                    FactoryName = "System.Data.SqlClient",
                    ConnectionString = $"Server=db;User Id=sa;Password={secret}"
                }),
                timeout.Token);

            var record = await new RavenDbAdminClient(fixture.Store).GetDatabaseRecord(databaseName, timeout.Token);
            var raw = record.Record.GetRawText();

            var connectionString = record.Record
                .GetProperty("SqlConnectionStrings")
                .GetProperty("reporting-sql")
                .GetProperty("ConnectionString")
                .GetString()!;

            Assert.DoesNotContain(secret, raw);
            // Hybrid redaction tokenizes the connection string: secret masked in place, Server/User Id kept.
            Assert.Contains("Server=db", connectionString);
            Assert.Contains("Password=***redacted***", connectionString);
            Assert.DoesNotContain(secret, connectionString);
        }
        finally
        {
            await fixture.Store.Maintenance.Server.SendAsync(
                new DeleteDatabasesOperation(databaseName, hardDelete: true), timeout.Token);
        }
    }

    [Fact]
    public async Task ReadsBetaDiagnosticCategories()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var client = new RavenDbAdminClient(fixture.Store);

        var clusterNodes = await client.GetClusterNodes(timeout.Token);
        var nodeTag = clusterNodes.Cluster.RespondingNodeTag!;

        Assert.Equal(JsonValueKind.Object, (await client.GetLogsConfiguration(timeout.Token)).Configuration.ValueKind);

        var collections = await client.GetCollectionOverview(fixture.DatabaseName, timeout.Token);
        Assert.Equal(JsonValueKind.Object, collections.Stats.ValueKind);
        Assert.Equal(JsonValueKind.Object, collections.DetailedStats.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetDatabaseConfiguration(fixture.DatabaseName, timeout.Token)).Configuration.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetClientConfiguration(fixture.DatabaseName, timeout.Token)).Configuration.ValueKind);
        var serverWideClientConfiguration = await client.GetServerWideClientConfiguration(timeout.Token);
        Assert.True(serverWideClientConfiguration.Configuration.ValueKind is JsonValueKind.Object or JsonValueKind.Null);

        var indexing = await client.GetIndexingOverview(fixture.DatabaseName, timeout.Token);
        Assert.Equal(JsonValueKind.Array, indexing.Indexes.ValueKind);
        Assert.Equal(JsonValueKind.Array, indexing.Stats.ValueKind);
        Assert.Equal(JsonValueKind.Array, indexing.Errors.ValueKind);
        Assert.Equal(JsonValueKind.Array, indexing.Performance.ValueKind);
        Assert.Equal(JsonValueKind.Object, indexing.Status.ValueKind);

        var index = await client.GetIndex(fixture.DatabaseName, fixture.IndexName, timeout.Token);
        Assert.Equal(fixture.IndexName, index.IndexName);
        Assert.Equal(JsonValueKind.Object, index.Index.ValueKind);

        var terms = await client.GetIndexTerms(
            fixture.DatabaseName,
            fixture.IndexName,
            fixture.IndexFieldName,
            null,
            16,
            timeout.Token);
        Assert.Equal(JsonValueKind.Array, terms.Terms.ValueKind);

        Assert.Equal(JsonValueKind.Object, (await client.GetReplicationTasksDetails(fixture.DatabaseName, timeout.Token)).Tasks.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetBackupTasks(fixture.DatabaseName, timeout.Token)).Tasks.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetEtlTasks(fixture.DatabaseName, timeout.Token)).Tasks.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetDatabaseTasks(fixture.DatabaseName, timeout.Token)).Tasks.ValueKind);

        Assert.Equal(JsonValueKind.Array, (await client.GetSubscriptions(fixture.DatabaseName, timeout.Token)).Subscriptions.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetDatabaseTcpInfo(fixture.DatabaseName, nodeTag, timeout.Token)).Tcp.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetIdentities(fixture.DatabaseName, timeout.Token)).Identities.ValueKind);
    }

    [Fact]
    public async Task ReadsStorageDiagnostics()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var client = new RavenDbAdminClient(fixture.Store);

        var overview = await client.GetStorageOverview(fixture.DatabaseName, timeout.Token);
        Assert.Equal(fixture.DatabaseName, overview.DatabaseName);
        Assert.Equal(JsonValueKind.Object, overview.Report.ValueKind);
        Assert.Equal(JsonValueKind.Object, overview.Environments.ValueKind);

        Assert.Equal(JsonValueKind.Object, (await client.GetStorageTrees(fixture.DatabaseName, timeout.Token)).Trees.ValueKind);
        var environment = await client.GetStorageEnvironmentDetails(fixture.DatabaseName, null, null, timeout.Token);
        Assert.Equal(JsonValueKind.Object, environment.Report.ValueKind);
        Assert.Equal(JsonValueKind.Object, environment.ScratchBuffers.ValueKind);
        Assert.Equal(JsonValueKind.Object, environment.FreeSpace.ValueKind);

        Assert.Equal(JsonValueKind.Object, (await client.GetStorageCompressionDictionaries(fixture.DatabaseName, timeout.Token)).Dictionaries.ValueKind);
    }

    [Fact]
    public async Task ReadsPerformanceDiagnostics()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var client = new RavenDbAdminClient(fixture.Store);

        Assert.Equal(JsonValueKind.Object, (await client.GetPerformanceOverview(timeout.Token)).Metrics.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetCpuStats(timeout.Token)).Cpu.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetGcMemoryStats(timeout.Token)).Gc.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetOsMemoryStats(timeout.Token)).Memory.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetProcessStats(timeout.Token)).Process.ValueKind);

        Assert.Equal(JsonValueKind.Object, (await client.GetIoStats(null, timeout.Token)).Io.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetIoStats(fixture.DatabaseName, timeout.Token)).Io.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetLowMemoryLog(timeout.Token)).Log.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetEncryptionBufferPoolStats(timeout.Token)).BufferPool.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetStackTraces(timeout.Token)).StackTraces.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetScriptRunners(null, timeout.Token)).ScriptRunners.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetScriptRunners(fixture.DatabaseName, timeout.Token)).ScriptRunners.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetTcpStats(timeout.Token)).Tcp.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.ListTcpConnections(null, timeout.Token)).Connections.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.ListTcpConnections(fixture.DatabaseName, timeout.Token)).Connections.ValueKind);

        var runtimeEvents = await client.SampleGcEvents(1, timeout.Token);
        Assert.Equal("gc_events", runtimeEvents.Kind);
        Assert.Equal(1, runtimeEvents.Seconds);

        var threadDiagnostics = await client.SampleThreadRunaway(timeout.Token);
        Assert.Equal("thread_runaway", threadDiagnostics.Kind);
        Assert.NotEmpty(threadDiagnostics.Sample);
    }
}
