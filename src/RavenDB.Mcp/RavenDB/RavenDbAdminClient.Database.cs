using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Operations.Configuration;
using Raven.Client.ServerWide.Operations;
using Raven.Client.ServerWide.Operations.Configuration;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    public async Task<ListDatabasesResult> ListDatabases(CancellationToken cancellationToken)
    {
        var databaseNames = await store.Maintenance.Server.SendAsync(
            new GetDatabaseNamesOperation(0, int.MaxValue),
            cancellationToken);

        return new ListDatabasesResult(databaseNames);
    }

    public async Task<GetDatabaseRecordResult> GetDatabaseRecord(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetDatabaseRecordResult(
            databaseName,
            await GetDatabaseRecordJson(databaseName, cancellationToken));
    }

    public async Task<GetDatabaseStatsResult> GetDatabaseStats(string databaseName, CancellationToken cancellationToken)
    {
        var stats = await ForDatabase(databaseName).SendAsync(
            new GetStatisticsOperation(),
            token: cancellationToken);

        return new GetDatabaseStatsResult(databaseName, ToJson(stats));
    }

    public async Task<GetDetailedDatabaseStatsResult> GetDetailedDatabaseStats(string databaseName, CancellationToken cancellationToken)
    {
        var stats = await ForDatabase(databaseName).SendAsync(
            new GetDetailedStatisticsOperation(),
            token: cancellationToken);

        return new GetDetailedDatabaseStatsResult(databaseName, ToJson(stats));
    }

    public async Task<GetCollectionStatsResult> GetCollectionStats(string databaseName, CancellationToken cancellationToken)
    {
        var stats = await ForDatabase(databaseName).SendAsync(
            new GetCollectionStatisticsOperation(),
            token: cancellationToken);

        return new GetCollectionStatsResult(databaseName, ToJson(stats));
    }

    public async Task<GetDetailedCollectionStatsResult> GetDetailedCollectionStats(string databaseName, CancellationToken cancellationToken)
    {
        var stats = await ForDatabase(databaseName).SendAsync(
            new GetDetailedCollectionStatisticsOperation(),
            token: cancellationToken);

        return new GetDetailedCollectionStatsResult(databaseName, ToJson(stats));
    }

    public async Task<GetCollectionOverviewResult> GetCollectionOverview(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var statsTask = GetCollectionStats(databaseName, cancellationToken);
        var detailedStatsTask = GetDetailedCollectionStats(databaseName, cancellationToken);
        await Task.WhenAll(statsTask, detailedStatsTask);

        return new GetCollectionOverviewResult(
            databaseName,
            (await statsTask).Stats,
            (await detailedStatsTask).Stats);
    }

    public async Task<GetDatabaseOverviewResult> GetDatabaseOverview(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var statsTask = GetDatabaseStats(databaseName, cancellationToken);
        var detailedStatsTask = GetDetailedDatabaseStats(databaseName, cancellationToken);
        var indexingStatusTask = GetIndexingStatus(databaseName, cancellationToken);
        var indexStatsTask = GetIndexStats(databaseName, cancellationToken);
        var indexErrorsTask = GetIndexErrors(databaseName, cancellationToken);
        var tasksTask = GetDatabaseTasks(databaseName, cancellationToken);
        await Task.WhenAll(statsTask, detailedStatsTask, indexingStatusTask, indexStatsTask, indexErrorsTask, tasksTask);

        return new GetDatabaseOverviewResult(
            databaseName,
            (await statsTask).Stats,
            (await detailedStatsTask).Stats,
            (await indexingStatusTask).Status,
            (await indexStatsTask).Stats,
            (await indexErrorsTask).Errors,
            (await tasksTask).Tasks);
    }

    public async Task<GetDatabaseConfigurationResult> GetDatabaseConfiguration(string databaseName, CancellationToken cancellationToken)
    {
        var configuration = await ForDatabase(databaseName).SendAsync(
            new GetDatabaseSettingsOperation(databaseName),
            token: cancellationToken);

        return new GetDatabaseConfigurationResult(databaseName, ToJson(configuration));
    }

    public async Task<GetClientConfigurationResult> GetClientConfiguration(string databaseName, CancellationToken cancellationToken)
    {
        var configuration = await ForDatabase(databaseName).SendAsync(
            new GetClientConfigurationOperation(),
            token: cancellationToken);

        return new GetClientConfigurationResult(databaseName, ToJson(configuration));
    }
}
