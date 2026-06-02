using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class ReplicationTools
{
    [McpServerTool(Name = "get_replication_tasks_details", ReadOnly = true, UseStructuredContent = true)]
    [Description("Replication diagnostics for a database: configured replication tasks, performance, active connections, conflicts, outgoing/incoming failures, rejections, reconnect queue, and progress.")]
    public static Task<GetReplicationTasksDetailsResult> GetReplicationTasksDetails(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetReplicationTasksDetails(databaseName, cancellationToken);
    }
}

public sealed record GetReplicationTasksResult(string DatabaseName, JsonElement Tasks);

public sealed record GetReplicationPerformanceResult(string DatabaseName, JsonElement Performance);

public sealed record GetReplicationTasksDetailsResult(
    string DatabaseName,
    JsonElement Tasks,
    JsonElement Performance,
    JsonElement ActiveConnections,
    JsonElement Conflicts,
    JsonElement OutgoingFailures,
    JsonElement IncomingLastActivity,
    JsonElement IncomingRejections,
    JsonElement OutgoingReconnectQueue,
    JsonElement Progress,
    JsonElement InternalOutgoingProgress);
