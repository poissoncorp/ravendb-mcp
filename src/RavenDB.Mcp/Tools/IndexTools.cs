using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class IndexTools
{
    [McpServerTool(Name = "get_index", ReadOnly = true)]
    [Description("One index, multiple views. Sections: Definition (maps/reduce, fields, config, lock/deployment mode), Staleness (is it stale and why), Debug (internal debug view + metadata + history), Terms (distinct indexed terms for fieldName, paged), Errors (this index's indexing errors), Performance (this index's performance stats). Choose with include; default is Definition + Staleness. Terms requires fieldName. For an all-indexes view use get_database_stats with the indexing section.")]
    public static async Task<Dictionary<string, object?>> GetIndex(
        RavenDbAdminClient client,
        [Description("Database the index belongs to.")] string databaseName,
        [Description("Index name.")] string indexName,
        [Description("Sections to return; omit for Definition + Staleness.")] IndexInclude[]? include = null,
        [Description("Field name — required for the Terms section.")] string? fieldName = null,
        [Description("Terms paging: return terms after this value.")] string? fromValue = null,
        [Description("Terms paging: max terms to return.")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var sections = Facet.Resolve(include, IndexInclude.Definition, IndexInclude.Staleness);
        var result = new Dictionary<string, object?>();

        if (sections.Contains(IndexInclude.Definition))
            result["definition"] = await client.GetIndex(databaseName, indexName, cancellationToken);
        if (sections.Contains(IndexInclude.Staleness))
            result["staleness"] = await client.GetIndexStaleness(databaseName, indexName, cancellationToken);
        if (sections.Contains(IndexInclude.Debug))
            result["debug"] = await client.GetIndexDebugDetails(databaseName, indexName, cancellationToken);
        if (sections.Contains(IndexInclude.Terms))
            result["terms"] = await client.GetIndexTerms(databaseName, indexName, Facet.Require(fieldName, "fieldName", "Terms"), fromValue, pageSize, cancellationToken);
        if (sections.Contains(IndexInclude.Errors))
            result["errors"] = await client.GetIndexErrors(databaseName, indexName, cancellationToken);
        if (sections.Contains(IndexInclude.Performance))
            result["performance"] = await client.GetIndexPerformance(databaseName, indexName, cancellationToken);

        return result;
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
