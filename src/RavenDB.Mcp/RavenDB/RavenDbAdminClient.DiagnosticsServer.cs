using System.Text.Json;
using Raven.Client.Documents.Operations;
using Raven.Client.ServerWide.Commands;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    public async Task<GetServerDiagnosticsOverviewResult> GetServerDiagnosticsOverview(CancellationToken cancellationToken)
    {
        var routesTask = TryGetServerJson("/debug/routes", cancellationToken);
        var settingsTask = TryGetServerJson("/admin/configuration/settings", cancellationToken);
        var metricsTask = TryGetServerJson("/admin/metrics", cancellationToken);
        var cpuCreditsTask = TryGetServerJson("/debug/cpu-credits", cancellationToken);
        var idleTask = TryGetServerJson("/admin/debug/databases/idle", cancellationToken);
        var licenseTask = TryGetServerJson("/license-server/connectivity", cancellationToken);
        var maintenanceTask = TryGetServerJson("/admin/cluster/maintenance-stats", cancellationToken);
        await Task.WhenAll(routesTask, settingsTask, metricsTask, cpuCreditsTask, idleTask, licenseTask, maintenanceTask);

        return new GetServerDiagnosticsOverviewResult(
            await routesTask,
            await settingsTask,
            await metricsTask,
            await cpuCreditsTask,
            await idleTask,
            await licenseTask,
            await maintenanceTask);
    }

    public async Task<GetClusterDiagnosticsOverviewResult> GetClusterDiagnosticsOverview(CancellationToken cancellationToken)
    {
        var decisionsTask = TryGetServerJson("/admin/cluster/observer/decisions", cancellationToken);
        var logTask = TryGetServerJson("/admin/cluster/log", cancellationToken);
        var historyTask = TryGetServerJson("/admin/debug/cluster/history-logs", cancellationToken);
        var remoteTask = TryGetServerJson("/admin/debug/node/remote-connections", cancellationToken);
        var engineTask = TryGetServerJson("/admin/debug/node/engine-logs", cancellationToken);
        var stateTask = TryGetServerJson("/admin/debug/node/state-change-history", cancellationToken);
        await Task.WhenAll(decisionsTask, logTask, historyTask, remoteTask, engineTask, stateTask);

        return new GetClusterDiagnosticsOverviewResult(
            await decisionsTask,
            await logTask,
            await historyTask,
            await remoteTask,
            await engineTask,
            await stateTask);
    }

    public async Task<DiagnosticTextSampleResult> SampleClusterDashboard(int seconds, CancellationToken cancellationToken)
    {
        // The cluster-dashboard watch feed is a WebSocket and requires the target node tag.
        var node = await ExecuteServerCommand(new GetNodeInfoCommand(), cancellationToken);
        var sample = await GetServerWebSocketSample(
            "/cluster-dashboard/watch",
            seconds,
            cancellationToken,
            ("node", node.NodeTag));

        return new DiagnosticTextSampleResult(
            "cluster_dashboard",
            Math.Clamp(seconds, 1, 30),
            sample.Text,
            sample.Truncated,
            sample.Limit);
    }

    public async Task<GetIndexStalenessResult> GetIndexStaleness(
        string databaseName,
        string indexName,
        CancellationToken cancellationToken)
    {
        ValidateName(indexName, "Index name", nameof(indexName));

        return new GetIndexStalenessResult(
            databaseName,
            indexName,
            await GetDatabaseJson(databaseName, "/indexes/staleness", cancellationToken, ("name", indexName)));
    }

    public async Task<GetIndexDebugDetailsResult> GetIndexDebugDetails(
        string databaseName,
        string indexName,
        CancellationToken cancellationToken)
    {
        ValidateName(indexName, "Index name", nameof(indexName));

        return new GetIndexDebugDetailsResult(
            databaseName,
            indexName,
            await TryGetDatabaseJson(databaseName, "/indexes/debug", cancellationToken, ("name", indexName)),
            await TryGetDatabaseJson(databaseName, "/indexes/debug/metadata", cancellationToken, ("name", indexName)),
            await TryGetDatabaseJson(databaseName, "/indexes/history", cancellationToken, ("name", indexName)));
    }

    public async Task<GetQueryDiagnosticsResult> GetQueryDiagnostics(string databaseName, CancellationToken cancellationToken)
    {
        return new GetQueryDiagnosticsResult(
            databaseName,
            await TryGetDatabaseJson(databaseName, "/debug/queries/running", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/debug/queries/cache/list", cancellationToken));
    }

    public async Task<GetOperationsOverviewResult> GetOperationsOverview(
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return new GetOperationsOverviewResult(
            databaseName,
            string.IsNullOrWhiteSpace(databaseName)
                ? ToJson(new { available = false, error = "databaseName was not provided." })
                : await TryGetDatabaseJson(databaseName, "/operations", cancellationToken),
            await TryGetServerJson("/admin/debug/operations/longest-running", cancellationToken));
    }

    public async Task<GetTransactionDiagnosticsResult> GetTransactionDiagnostics(
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return new GetTransactionDiagnosticsResult(
            databaseName,
            await TryGetServerJson("/admin/debug/txinfo", cancellationToken),
            string.IsNullOrWhiteSpace(databaseName)
                ? ToJson(new { available = false, error = "databaseName was not provided." })
                : await TryGetDatabaseJson(databaseName, "/admin/debug/txinfo", cancellationToken),
            string.IsNullOrWhiteSpace(databaseName)
                ? ToJson(new { available = false, error = "databaseName was not provided." })
                : await TryGetDatabaseJson(databaseName, "/admin/debug/cluster/txinfo", cancellationToken));
    }

    public async Task<WaitForConditionResult> WaitForOperation(
        string databaseName,
        long operationId,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(Math.Clamp(timeoutSeconds, 1, 300));
        var polls = 0;
        JsonElement state = default;

        while (DateTime.UtcNow <= deadline)
        {
            polls++;
            var operationState = await ForDatabase(databaseName).SendAsync(
                new GetOperationStateOperation(operationId),
                token: cancellationToken);
            state = ToJson(operationState);

            // A non-existent/expired operation id returns a null state; keep polling until the
            // timeout rather than dereferencing null (get_operation_state returns null gracefully).
            if (operationState is not null
                && operationState.Status is OperationStatus.Completed or OperationStatus.Faulted or OperationStatus.Canceled)
                return new WaitForConditionResult("operation", databaseName, operationId, null, true, polls, state);

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        return new WaitForConditionResult("operation", databaseName, operationId, null, false, polls, state);
    }

    public async Task<WaitForConditionResult> WaitForIndexing(
        string databaseName,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(Math.Clamp(timeoutSeconds, 1, 300));
        var polls = 0;
        JsonElement state = default;

        while (DateTime.UtcNow <= deadline)
        {
            polls++;
            var stats = await ForDatabase(databaseName).SendAsync(
                new GetStatisticsOperation(),
                token: cancellationToken);
            state = ToJson(stats);

            if (stats.Indexes.All(index => !index.IsStale))
                return new WaitForConditionResult("indexing", databaseName, null, null, true, polls, state);

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        return new WaitForConditionResult("indexing", databaseName, null, null, false, polls, state);
    }

    public async Task<GetDocumentConflictsResult> GetDocumentConflicts(
        string databaseName,
        string documentId,
        CancellationToken cancellationToken)
    {
        ValidateName(documentId, "Document id", nameof(documentId));

        return new GetDocumentConflictsResult(
            databaseName,
            documentId,
            await GetDatabaseJson(databaseName, "/replication/conflicts", cancellationToken, ("docId", documentId)));
    }

    public async Task<GetBackupDiagnosticsResult> GetBackupDiagnostics(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var tasks = await GetBackupTasks(databaseName, cancellationToken);

        return new GetBackupDiagnosticsResult(
            databaseName,
            tasks.Tasks,
            await TryGetDatabaseJson(databaseName, "/admin/debug/periodic-backup/timers", cancellationToken),
            await TryGetServerJson("/admin/configuration/server-wide/backup", cancellationToken));
    }

    public async Task<GetEtlDiagnosticsResult> GetEtlDiagnostics(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var tasks = await GetEtlTasks(databaseName, cancellationToken);

        return new GetEtlDiagnosticsResult(
            databaseName,
            tasks.Tasks,
            await TryGetDatabaseJson(databaseName, "/etl/stats", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/etl/performance", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/etl/debug/stats", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/etl/progress", cancellationToken));
    }

    public async Task<GetSubscriptionDiagnosticsResult> GetSubscriptionDiagnostics(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var subscriptions = await GetSubscriptions(databaseName, cancellationToken);

        return new GetSubscriptionDiagnosticsResult(
            databaseName,
            subscriptions.Subscriptions,
            await TryGetDatabaseJson(databaseName, "/subscriptions/state", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/subscriptions/connection-details", cancellationToken));
    }

    public async Task<GetTrafficWatchConfigurationResult> GetTrafficWatchConfiguration(CancellationToken cancellationToken)
    {
        return new GetTrafficWatchConfigurationResult(
            await GetServerJson("/admin/traffic-watch/configuration", cancellationToken));
    }

    // Studio configuration is optional; the route 404s until it's been set. Availability-wrapped
    // so an unconfigured server reports { available: false } instead of throwing.
    public Task<JsonElement> GetServerStudioConfiguration(CancellationToken cancellationToken)
    {
        return TryGetServerJson("/configuration/studio", cancellationToken);
    }

    public Task<DiagnosticArtifactResult> ExportLogs(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        return SaveServerArtifact(
            "logs",
            "/admin/logs/download",
            cancellationToken,
            ("from", from?.ToString("O")),
            ("to", to?.ToString("O")));
    }

    public async Task<DiagnosticTextSampleResult> SampleTrafficWatch(
        int seconds,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        // Traffic-watch live feed is a WebSocket endpoint; listen for the sample window.
        var sample = await GetServerWebSocketSample(
            "/admin/traffic-watch",
            seconds,
            cancellationToken,
            ("database", databaseName));

        return new DiagnosticTextSampleResult(
            "traffic_watch",
            Math.Clamp(seconds, 1, 30),
            sample.Text,
            sample.Truncated,
            sample.Limit);
    }

    public async Task<GetNotificationsResult> GetNotifications(
        string? databaseName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            return new GetNotificationsResult(null, await GetServerJson("/admin/server/notifications", cancellationToken));

        return new GetNotificationsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/notifications", cancellationToken));
    }

    public async Task<DiagnosticTextSampleResult> SampleAdminLogs(int seconds, CancellationToken cancellationToken)
    {
        // The admin logs watch feed is a WebSocket endpoint.
        var sample = await GetServerWebSocketSample("/admin/logs/watch", seconds, cancellationToken);
        return new DiagnosticTextSampleResult(
            "admin_logs",
            Math.Clamp(seconds, 1, 30),
            sample.Text,
            sample.Truncated,
            sample.Limit);
    }
}
