using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class IndexTools
{
    [McpServerTool(Name = "get_indexing_overview", ReadOnly = true, UseStructuredContent = true)]
    [Description("One-call indexing snapshot for a database: index summaries, stats, errors, indexing status, performance, progress, suggested merges, and total indexing time.")]
    public static Task<GetIndexingOverviewResult> GetIndexingOverview(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetIndexingOverview(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_index", ReadOnly = true, UseStructuredContent = true)]
    [Description("Full definition of one index: maps/reduce, fields, configuration, lock mode, and deployment mode.")]
    public static Task<GetIndexResult> GetIndex(
        RavenDbAdminClient client,
        string databaseName,
        string indexName,
        CancellationToken cancellationToken)
    {
        return client.GetIndex(databaseName, indexName, cancellationToken);
    }

    [McpServerTool(Name = "get_index_terms", ReadOnly = true, UseStructuredContent = true)]
    [Description("Distinct indexed terms for one field of an index (paged from fromValue). Use to inspect how a field is tokenized/indexed.")]
    public static Task<GetIndexTermsResult> GetIndexTerms(
        RavenDbAdminClient client,
        string databaseName,
        string indexName,
        string fieldName,
        string? fromValue,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        return client.GetIndexTerms(databaseName, indexName, fieldName, fromValue, pageSize, cancellationToken);
    }
}

public sealed record ListIndexesResult(string DatabaseName, JsonElement Indexes);

public sealed record GetIndexStatsResult(string DatabaseName, JsonElement Stats);

public sealed record GetIndexErrorsResult(string DatabaseName, JsonElement Errors);

public sealed record GetIndexPerformanceResult(string DatabaseName, JsonElement Performance);

public sealed record GetIndexingStatusResult(string DatabaseName, JsonElement Status);

public sealed record GetIndexingOverviewResult(
    string DatabaseName,
    JsonElement Indexes,
    JsonElement Stats,
    JsonElement Errors,
    JsonElement Status,
    JsonElement Performance,
    JsonElement Progress,
    JsonElement SuggestedMerges,
    JsonElement TotalTime);

public sealed record GetIndexResult(string DatabaseName, string IndexName, JsonElement Index);

public sealed record GetIndexTermsResult(
    string DatabaseName,
    string IndexName,
    string FieldName,
    JsonElement Terms);
