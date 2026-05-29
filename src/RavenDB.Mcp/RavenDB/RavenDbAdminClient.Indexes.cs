using System.Text.Json;
using Raven.Client.Documents.Operations.Indexes;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    public async Task<ListIndexesResult> ListIndexes(string databaseName, CancellationToken cancellationToken)
    {
        var indexes = await ForDatabase(databaseName).SendAsync(
            new GetIndexesOperation(0, int.MaxValue),
            token: cancellationToken);

        return new ListIndexesResult(databaseName, ToJson(indexes));
    }

    public async Task<GetIndexStatsResult> GetIndexStats(string databaseName, CancellationToken cancellationToken)
    {
        var stats = await ForDatabase(databaseName).SendAsync(
            new GetIndexesStatisticsOperation(),
            token: cancellationToken);

        return new GetIndexStatsResult(databaseName, ToJson(stats));
    }

    public async Task<GetIndexErrorsResult> GetIndexErrors(string databaseName, CancellationToken cancellationToken)
    {
        var errors = await ForDatabase(databaseName).SendAsync(
            new GetIndexErrorsOperation(),
            token: cancellationToken);

        return new GetIndexErrorsResult(databaseName, ToJson(errors));
    }

    public async Task<GetIndexPerformanceResult> GetIndexPerformance(string databaseName, CancellationToken cancellationToken)
    {
        var performance = await ForDatabase(databaseName).SendAsync(
            new GetIndexPerformanceStatisticsOperation(),
            token: cancellationToken);

        return new GetIndexPerformanceResult(databaseName, ToJson(performance));
    }

    public async Task<GetIndexingStatusResult> GetIndexingStatus(string databaseName, CancellationToken cancellationToken)
    {
        var status = await ForDatabase(databaseName).SendAsync(
            new GetIndexingStatusOperation(),
            token: cancellationToken);

        return new GetIndexingStatusResult(databaseName, ToJson(status));
    }

    public async Task<GetIndexingOverviewResult> GetIndexingOverview(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var indexes = await ListIndexes(databaseName, cancellationToken);
        var stats = await GetIndexStats(databaseName, cancellationToken);
        var errors = await GetIndexErrors(databaseName, cancellationToken);
        var status = await GetIndexingStatus(databaseName, cancellationToken);
        var performance = await GetIndexPerformance(databaseName, cancellationToken);

        return new GetIndexingOverviewResult(
            databaseName,
            SummarizeIndexes(indexes.Indexes),
            stats.Stats,
            errors.Errors,
            status.Status,
            performance.Performance,
            await TryGetDatabaseJson(databaseName, "/indexes/progress", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/indexes/suggest-index-merge", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/indexes/total-time", cancellationToken));
    }

    public async Task<GetIndexResult> GetIndex(
        string databaseName,
        string indexName,
        CancellationToken cancellationToken)
    {
        ValidateName(indexName, "Index name", nameof(indexName));

        var index = await ForDatabase(databaseName).SendAsync(
            new GetIndexOperation(indexName),
            token: cancellationToken);

        return new GetIndexResult(databaseName, indexName, ToJson(index));
    }

    public async Task<GetIndexTermsResult> GetIndexTerms(
        string databaseName,
        string indexName,
        string fieldName,
        string? fromValue,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        ValidateName(indexName, "Index name", nameof(indexName));
        ValidateName(fieldName, "Field name", nameof(fieldName));

        var terms = await ForDatabase(databaseName).SendAsync(
            new GetTermsOperation(indexName, fieldName, fromValue, pageSize),
            token: cancellationToken);

        return new GetIndexTermsResult(databaseName, indexName, fieldName, ToJson(terms));
    }

    private static JsonElement SummarizeIndexes(JsonElement indexes)
    {
        var values = new List<Dictionary<string, JsonElement>>();

        foreach (var index in indexes.EnumerateArray())
            values.Add(SelectProperties(index, "Name", "Type", "SourceType", "State", "Priority", "LockMode", "DeploymentMode"));

        return ToJson(values);
    }
}
