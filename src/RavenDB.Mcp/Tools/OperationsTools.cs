using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class OperationsTools
{
    [McpServerTool(Name = "get_operation_state", ReadOnly = true, UseStructuredContent = true)]
    [Description("State of a long-running server operation by id: status (InProgress/Completed/Faulted/Canceled), progress, and result.")]
    public static Task<GetOperationStateResult> GetOperationState(
        RavenDbAdminClient client,
        string databaseName,
        long operationId,
        CancellationToken cancellationToken)
    {
        return client.GetOperationState(databaseName, operationId, cancellationToken);
    }

}

public sealed record GetOperationStateResult(string DatabaseName, long OperationId, JsonElement State);

public sealed record ListOngoingTasksResult(string DatabaseName, JsonElement Tasks);
