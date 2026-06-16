using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class OperationsTools
{
    [McpServerTool(Name = "get_live_workload", ReadOnly = true)]
    [Description("Live runtime activity: running (and longest-running) operations, currently running queries plus the query cache, and transaction info. Choose sections with include (default: all). Pass operationId to fetch one operation's state instead of the overview. databaseName is required for Queries and (with operationId) Operations; for Operations/Transactions without it, results are server-wide.")]
    public static async Task<Dictionary<string, object?>> GetLiveWorkload(
        RavenDbAdminClient client,
        [Description("Database to scope to. Required for Queries and for an operationId lookup; omit for server-wide Operations/Transactions.")] string? databaseName = null,
        [Description("Sections to return; omit for all.")] WorkloadInclude[]? include = null,
        [Description("Fetch this operation's state instead of the running-operations overview (needs databaseName).")] long? operationId = null,
        CancellationToken cancellationToken = default)
    {
        var sections = Facet.Resolve(include, WorkloadInclude.Operations, WorkloadInclude.Queries, WorkloadInclude.Transactions);
        var result = new Dictionary<string, object?>();

        if (sections.Contains(WorkloadInclude.Operations))
            result["operations"] = operationId is { } id
                ? await client.GetOperationState(Facet.RequireDatabase(databaseName, "operationId lookup"), id, cancellationToken)
                : await client.GetOperationsOverview(databaseName, cancellationToken);

        if (sections.Contains(WorkloadInclude.Queries))
            result["queries"] = await client.GetQueryDiagnostics(Facet.RequireDatabase(databaseName, "Queries"), cancellationToken);

        if (sections.Contains(WorkloadInclude.Transactions))
            result["transactions"] = await client.GetTransactionDiagnostics(databaseName, cancellationToken);

        return result;
    }
}

public sealed record GetOperationStateResult(string DatabaseName, long OperationId, JsonElement State);

public sealed record ListOngoingTasksResult(string DatabaseName, JsonElement Tasks);
