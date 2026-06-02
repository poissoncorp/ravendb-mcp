using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Raven.Client.Documents.Operations.OngoingTasks;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class TaskTools
{
    [McpServerTool(Name = "get_database_tasks", ReadOnly = true, UseStructuredContent = true)]
    [Description("All ongoing tasks for a database in one call: backups, ETL, replication, and subscriptions (from the database record + subscription state).")]
    public static Task<GetDatabaseTasksResult> GetDatabaseTasks(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetDatabaseTasks(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_backup_tasks", ReadOnly = true, UseStructuredContent = true)]
    [Description("Periodic-backup task definitions for a database (schedules, destinations, and settings from the database record).")]
    public static Task<GetBackupTasksResult> GetBackupTasks(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetBackupTasks(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_backup_status", ReadOnly = true, UseStructuredContent = true)]
    [Description("Last/most-recent run status of one periodic-backup task by id: timings, durations, sizes, and any error.")]
    public static Task<GetBackupStatusResult> GetBackupStatus(
        RavenDbAdminClient client,
        string databaseName,
        long taskId,
        CancellationToken cancellationToken)
    {
        return client.GetBackupStatus(databaseName, taskId, cancellationToken);
    }

    [McpServerTool(Name = "get_ongoing_task_info", ReadOnly = true, UseStructuredContent = true)]
    [Description("Detailed runtime info for one ongoing task by id and type (e.g. Backup, Replication, RavenEtl, SqlEtl, Subscription): responsible node, connection status, and task-specific state.")]
    public static Task<GetOngoingTaskInfoResult> GetOngoingTaskInfo(
        RavenDbAdminClient client,
        string databaseName,
        long taskId,
        OngoingTaskType taskType,
        CancellationToken cancellationToken)
    {
        return client.GetOngoingTaskInfo(databaseName, taskId, taskType, cancellationToken);
    }

    [McpServerTool(Name = "get_etl_tasks", ReadOnly = true, UseStructuredContent = true)]
    [Description("ETL task definitions for a database (RavenDB/SQL/OLAP/Elasticsearch/Queue ETL configuration from the database record).")]
    public static Task<GetEtlTasksResult> GetEtlTasks(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetEtlTasks(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_etl_task_info", ReadOnly = true, UseStructuredContent = true)]
    [Description("Detailed runtime info for one ETL task by id and type: responsible node, connection status, and ETL-specific state.")]
    public static Task<GetEtlTaskInfoResult> GetEtlTaskInfo(
        RavenDbAdminClient client,
        string databaseName,
        long taskId,
        OngoingTaskType taskType,
        CancellationToken cancellationToken)
    {
        return client.GetEtlTaskInfo(databaseName, taskId, taskType, cancellationToken);
    }
}

public sealed record GetBackupTasksResult(string DatabaseName, JsonElement Tasks);

public sealed record GetDatabaseTasksResult(
    string DatabaseName,
    JsonElement Tasks,
    JsonElement BackupTasks,
    JsonElement EtlTasks,
    JsonElement ReplicationTasks,
    JsonElement Subscriptions);

public sealed record GetBackupStatusResult(string DatabaseName, long TaskId, JsonElement Status);

public sealed record GetOngoingTaskInfoResult(
    string DatabaseName,
    long TaskId,
    string TaskType,
    JsonElement Task);

public sealed record GetEtlTasksResult(string DatabaseName, JsonElement Tasks);

public sealed record GetEtlTaskInfoResult(
    string DatabaseName,
    long TaskId,
    string TaskType,
    JsonElement Task);
