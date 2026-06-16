using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Raven.Client.Documents.Operations.OngoingTasks;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class TaskTools
{
    [McpServerTool(Name = "get_tasks", ReadOnly = true)]
    [Description("Ongoing tasks for a database. Default (no taskId/subscriptionName/diagnostics) lists all tasks: backups, ETL, replication, subscriptions. Provide taskId (+taskType) for one task's runtime info; subscriptionName for a subscription's processing state; set includeDiagnostics (+taskType) for deep diagnostics of that family (Backup / *Etl / Subscription / Replication).")]
    public static async Task<Dictionary<string, object?>> GetTasks(
        RavenDbAdminClient client,
        [Description("Database to read tasks for.")] string databaseName,
        [Description("Task type for info or diagnostics: Backup, RavenEtl, SqlEtl, OlapEtl, ElasticSearchEtl, QueueEtl, QueueSink, Replication, Subscription, PullReplicationAsHub, PullReplicationAsSink.")] OngoingTaskType? taskType = null,
        [Description("Task id — returns this task's runtime info (requires taskType).")] long? taskId = null,
        [Description("Subscription name — returns this subscription's processing state.")] string? subscriptionName = null,
        [Description("Add deep diagnostics for the taskType family (requires taskType).")] bool includeDiagnostics = false,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, object?>();

        if (taskId is { } id)
            result["info"] = await client.GetOngoingTaskInfo(
                databaseName, id,
                taskType ?? throw new McpException("taskType is required when taskId is given."),
                cancellationToken);

        if (!string.IsNullOrWhiteSpace(subscriptionName))
            result["subscriptionState"] = await client.GetSubscriptionState(databaseName, subscriptionName, cancellationToken);

        if (includeDiagnostics)
            result["diagnostics"] = await TaskDiagnostics(
                client, databaseName,
                taskType ?? throw new McpException("taskType is required when includeDiagnostics is set."),
                cancellationToken);

        if (result.Count == 0)
            result["tasks"] = await client.GetDatabaseTasks(databaseName, cancellationToken);

        return result;
    }

    private static async Task<object?> TaskDiagnostics(
        RavenDbAdminClient client,
        string databaseName,
        OngoingTaskType taskType,
        CancellationToken cancellationToken)
        => taskType switch
        {
            OngoingTaskType.Backup => await client.GetBackupDiagnostics(databaseName, cancellationToken),
            OngoingTaskType.RavenEtl or OngoingTaskType.SqlEtl or OngoingTaskType.OlapEtl
                or OngoingTaskType.ElasticSearchEtl or OngoingTaskType.QueueEtl
                => await client.GetEtlDiagnostics(databaseName, cancellationToken),
            OngoingTaskType.Subscription => await client.GetSubscriptionDiagnostics(databaseName, cancellationToken),
            OngoingTaskType.Replication or OngoingTaskType.PullReplicationAsHub or OngoingTaskType.PullReplicationAsSink
                => await client.GetReplicationTasksDetails(databaseName, cancellationToken),
            _ => throw new McpException($"No deep diagnostics available for task type '{taskType}'.")
        };
}

public sealed record GetBackupTasksResult(string DatabaseName, JsonElement Tasks);

public sealed record GetDatabaseTasksResult(
    string DatabaseName,
    JsonElement Tasks,
    JsonElement BackupTasks,
    JsonElement EtlTasks,
    JsonElement ReplicationTasks,
    JsonElement Subscriptions);

public sealed record GetOngoingTaskInfoResult(
    string DatabaseName,
    long TaskId,
    string TaskType,
    JsonElement Task);

public sealed record GetEtlTasksResult(string DatabaseName, JsonElement Tasks);
