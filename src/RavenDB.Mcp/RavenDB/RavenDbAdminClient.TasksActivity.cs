using Raven.Client.Documents.Operations.Identities;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Operations.OngoingTasks;
using Raven.Client.Documents.Operations.Replication;
using Raven.Client.ServerWide.Commands;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    public async Task<GetOperationStateResult> GetOperationState(
        string databaseName,
        long operationId,
        CancellationToken cancellationToken)
    {
        var state = await ForDatabase(databaseName).SendAsync(
            new GetOperationStateOperation(operationId),
            token: cancellationToken);

        return new GetOperationStateResult(
            databaseName,
            operationId,
            ToJson(state));
    }

    public async Task<ListOngoingTasksResult> ListOngoingTasks(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var record = await GetDatabaseRecordJson(databaseName, cancellationToken);

        return new ListOngoingTasksResult(
            databaseName,
            SelectRecordProperties(record, "backup", "replication", "etl", "subscription", "sink", "expiration", "refresh", "archival"));
    }

    public async Task<GetBackupTasksResult> GetBackupTasks(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var record = await GetDatabaseRecordJson(databaseName, cancellationToken);
        return new GetBackupTasksResult(databaseName, SelectRecordProperties(record, "backup"));
    }

    public async Task<GetDatabaseTasksResult> GetDatabaseTasks(
        string databaseName,
        CancellationToken cancellationToken)
    {
        // Fetch the database record once (the per-category methods would each refetch it)
        // and pull subscriptions concurrently.
        var recordTask = GetDatabaseRecordJson(databaseName, cancellationToken);
        var subscriptionsTask = GetSubscriptions(databaseName, cancellationToken);
        await Task.WhenAll(recordTask, subscriptionsTask);

        var record = await recordTask;
        var subscriptions = await subscriptionsTask;

        return new GetDatabaseTasksResult(
            databaseName,
            SelectRecordProperties(record, "backup", "replication", "etl", "subscription", "sink", "expiration", "refresh", "archival"),
            SelectRecordProperties(record, "backup"),
            SelectRecordProperties(record, "etl"),
            SelectRecordProperties(record, "replication"),
            subscriptions.Subscriptions);
    }

    public async Task<GetReplicationTasksResult> GetReplicationTasks(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var record = await GetDatabaseRecordJson(databaseName, cancellationToken);
        return new GetReplicationTasksResult(databaseName, SelectRecordProperties(record, "replication"));
    }

    public async Task<GetReplicationPerformanceResult> GetReplicationPerformance(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var performance = await ForDatabase(databaseName).SendAsync(
            new GetReplicationPerformanceStatisticsOperation(),
            token: cancellationToken);

        return new GetReplicationPerformanceResult(
            databaseName,
            ToJson(performance));
    }

    public async Task<GetReplicationTasksDetailsResult> GetReplicationTasksDetails(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var tasks = await GetReplicationTasks(databaseName, cancellationToken);
        var performance = await GetReplicationPerformance(databaseName, cancellationToken);

        return new GetReplicationTasksDetailsResult(
            databaseName,
            tasks.Tasks,
            performance.Performance,
            await TryGetDatabaseJson(databaseName, "/replication/active-connections", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/replication/conflicts", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/replication/debug/outgoing-failures", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/replication/debug/incoming-last-activity-time", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/replication/debug/incoming-rejection-info", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/replication/debug/outgoing-reconnect-queue", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/replication/progress", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/replication/internal/outgoing/progress", cancellationToken));
    }

    public async Task<GetEtlTasksResult> GetEtlTasks(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var record = await GetDatabaseRecordJson(databaseName, cancellationToken);
        return new GetEtlTasksResult(databaseName, SelectRecordProperties(record, "etl"));
    }

    public async Task<GetOngoingTaskInfoResult> GetOngoingTaskInfo(
        string databaseName,
        long taskId,
        OngoingTaskType taskType,
        CancellationToken cancellationToken)
    {
        var task = await ForDatabase(databaseName).SendAsync(
            new GetOngoingTaskInfoOperation(taskId, taskType),
            token: cancellationToken);

        return new GetOngoingTaskInfoResult(
            databaseName,
            taskId,
            taskType.ToString(),
            ToJson(task));
    }

    public async Task<GetSubscriptionsResult> GetSubscriptions(
        string databaseName,
        CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);

        var subscriptions = await store.Subscriptions.GetSubscriptionsAsync(
            0,
            int.MaxValue,
            databaseName,
            cancellationToken);

        return new GetSubscriptionsResult(databaseName, ToJson(subscriptions));
    }

    public async Task<GetSubscriptionStateResult> GetSubscriptionState(
        string databaseName,
        string subscriptionName,
        CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);

        if (string.IsNullOrWhiteSpace(subscriptionName))
            throw new ArgumentException("Subscription name is required.", nameof(subscriptionName));

        var state = await store.Subscriptions.GetSubscriptionStateAsync(
            subscriptionName,
            databaseName,
            cancellationToken);

        return new GetSubscriptionStateResult(databaseName, subscriptionName, ToJson(state));
    }

    public async Task<GetDatabaseTcpInfoResult> GetDatabaseTcpInfo(
        string databaseName,
        string nodeTag,
        CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);

        if (string.IsNullOrWhiteSpace(nodeTag))
            throw new ArgumentException("Node tag is required.", nameof(nodeTag));

        var tcp = await ExecuteServerCommand(new GetTcpInfoCommand(nodeTag, databaseName), cancellationToken);
        return new GetDatabaseTcpInfoResult(databaseName, nodeTag, ToJson(tcp));
    }

    public async Task<GetIdentitiesResult> GetIdentities(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var identities = await ForDatabase(databaseName).SendAsync(
            new GetIdentitiesOperation(),
            token: cancellationToken);

        return new GetIdentitiesResult(databaseName, ToJson(identities));
    }
}
