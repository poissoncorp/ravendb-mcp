using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class DocumentDataTools
{
    [McpServerTool(Name = "get_document_data", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = true)]
    [Description("Everything about ONE document by id. Sections: Document (body + @metadata; Found=false when absent), Counters, Attachments (names/sizes/hashes), TimeSeries (requires timeSeriesName, optional from/to), Revisions, Conflicts. Choose with include; default is Document. Returns real document data.")]
    public static async Task<Dictionary<string, object?>> GetDocumentData(
        RavenDbAdminClient client,
        [Description("Database the document is in.")] string databaseName,
        [Description("Exact document id, e.g. 'users/1-A'.")] string id,
        [Description("Sections to return; omit for Document.")] DocumentInclude[]? include,
        [Description("Time series name — required for the TimeSeries section.")] string? timeSeriesName,
        [Description("TimeSeries range start (ISO-8601).")] DateTime? from,
        [Description("TimeSeries range end (ISO-8601).")] DateTime? to,
        CancellationToken cancellationToken)
    {
        var sections = Facet.Resolve(include, DocumentInclude.Document);
        var result = new Dictionary<string, object?>();

        // The document carries its attachments under @metadata.@attachments — fetch once, reuse.
        GetDocumentResult? document = null;
        if (sections.Contains(DocumentInclude.Document) || sections.Contains(DocumentInclude.Attachments))
            document = await client.GetDocument(databaseName, id, cancellationToken);

        if (sections.Contains(DocumentInclude.Document))
            result["document"] = document;

        if (sections.Contains(DocumentInclude.Attachments))
            result["attachments"] =
                document!.Found
                && document.Document.TryGetProperty("@metadata", out var metadata)
                && metadata.TryGetProperty("@attachments", out var attachments)
                    ? attachments.Clone()
                    : null;

        if (sections.Contains(DocumentInclude.Counters))
            result["counters"] = await client.GetDocumentCounters(databaseName, id, cancellationToken);

        if (sections.Contains(DocumentInclude.TimeSeries))
            result["timeSeries"] = await client.GetDocumentTimeSeries(
                databaseName, id, Facet.Require(timeSeriesName, "timeSeriesName", "TimeSeries"), from, to, cancellationToken);

        if (sections.Contains(DocumentInclude.Revisions))
            result["revisions"] = await client.GetDocumentRevisions(databaseName, id, null, null, cancellationToken);

        if (sections.Contains(DocumentInclude.Conflicts))
            result["conflicts"] = await client.GetDocumentConflicts(databaseName, id, cancellationToken);

        return result;
    }

    [McpServerTool(Name = "run_query", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = true)]
    [Description("Run a read-only RQL query and return result rows plus query metadata (TotalResults, IndexName, IsStale, DurationInMs). Paged via start/pageSize (1-128). Mutating UPDATE/patch queries are rejected. Returns real document data.")]
    public static Task<RunQueryResult> RunQuery(
        RavenDbAdminClient client,
        string databaseName,
        [Description("RQL query, e.g. 'from Users where Age > 30'. Read-only; UPDATE/patch is rejected.")] string query,
        [Description("Zero-based offset of the first row to return (default 0).")] int? start,
        [Description("Max rows to return, 1-128 (default 25).")] int? pageSize,
        CancellationToken cancellationToken)
    {
        return client.RunQuery(databaseName, query, start, pageSize, cancellationToken);
    }

    [McpServerTool(Name = "list_compare_exchange", ReadOnly = true)]
    [Description("List cluster-wide compare-exchange (cmpxchg) key/value entries — used for atomic guards, unique constraints, and cluster-transaction state. Optional startsWith key prefix; paged (1-1024, default 100). Not document-scoped.")]
    public static Task<JsonElement> ListCompareExchange(
        RavenDbAdminClient client,
        string databaseName,
        [Description("Only return keys starting with this prefix.")] string? startsWith,
        [Description("Max entries to return, 1-1024 (default 100).")] int? pageSize,
        CancellationToken cancellationToken)
    {
        return client.GetCompareExchange(databaseName, startsWith, pageSize, cancellationToken);
    }
}

public sealed record GetDocumentResult(string DatabaseName, string Id, bool Found, JsonElement Document);

public sealed record RunQueryResult(string DatabaseName, string Query, JsonElement Result);
