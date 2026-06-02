using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class DocumentDataTools
{
    [McpServerTool(Name = "get_document", ReadOnly = true, UseStructuredContent = true, Idempotent = true, Destructive = false, OpenWorld = true)]
    [Description("Read one document by id, including its @metadata. Returns Found=false (not an error) when the id does not exist. Returns real document content.")]
    public static Task<GetDocumentResult> GetDocument(
        RavenDbAdminClient client,
        string databaseName,
        [Description("Exact document id, e.g. 'users/1-A'.")] string id,
        CancellationToken cancellationToken)
    {
        return client.GetDocument(databaseName, id, cancellationToken);
    }

    [McpServerTool(Name = "run_query", ReadOnly = true, UseStructuredContent = true, Idempotent = true, Destructive = false, OpenWorld = true)]
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
}

public sealed record GetDocumentResult(string DatabaseName, string Id, bool Found, JsonElement Document);

public sealed record RunQueryResult(string DatabaseName, string Query, JsonElement Result);
