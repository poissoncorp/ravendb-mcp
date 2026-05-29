using System.Diagnostics;
using System.Text.Json;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    public async Task<GetServerDiagnosticsOverviewResult> GetServerDiagnosticsOverview(CancellationToken cancellationToken)
    {
        return new GetServerDiagnosticsOverviewResult(
            await TryGetServerJson("/debug/routes", cancellationToken),
            await TryGetServerJson("/admin/configuration/settings", cancellationToken),
            await TryGetServerJson("/admin/metrics", cancellationToken),
            await TryGetServerJson("/debug/cpu-credits", cancellationToken),
            await TryGetServerJson("/admin/debug/databases/idle", cancellationToken),
            await TryGetServerJson("/license-server/connectivity", cancellationToken),
            await TryGetServerJson("/admin/cluster/maintenance-stats", cancellationToken));
    }

    public async Task<GetClusterDiagnosticsOverviewResult> GetClusterDiagnosticsOverview(CancellationToken cancellationToken)
    {
        return new GetClusterDiagnosticsOverviewResult(
            await TryGetServerJson("/admin/cluster/observer/decisions", cancellationToken),
            await TryGetServerJson("/admin/cluster/log", cancellationToken),
            await TryGetServerJson("/admin/debug/cluster/history-logs", cancellationToken),
            await TryGetServerJson("/admin/debug/node/remote-connections", cancellationToken),
            await TryGetServerJson("/admin/debug/node/engine-logs", cancellationToken),
            await TryGetServerJson("/admin/debug/node/state-change-history", cancellationToken));
    }

    public async Task<PingClusterNodeResult> PingClusterNode(string url, CancellationToken cancellationToken)
    {
        ValidateName(url, "Node URL", nameof(url));

        var target = url.TrimEnd('/') + "/admin/debug/node/ping";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await http.GetAsync(target, cancellationToken);
            stopwatch.Stop();

            return new PingClusterNodeResult(
                url,
                (int)response.StatusCode,
                response.IsSuccessStatusCode,
                stopwatch.ElapsedMilliseconds,
                null);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return new PingClusterNodeResult(url, null, false, stopwatch.ElapsedMilliseconds, exception.Message);
        }
    }

    public async Task<DiagnosticTextSampleResult> SampleClusterDashboard(int seconds, CancellationToken cancellationToken)
    {
        return new DiagnosticTextSampleResult(
            "cluster_dashboard",
            Math.Clamp(seconds, 1, 30),
            await GetServerTextSample("/cluster-dashboard/watch", seconds, cancellationToken));
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
            state = (await GetOperationState(databaseName, operationId, cancellationToken)).State;

            if (LooksComplete(state))
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
            state = (await GetIndexingStatus(databaseName, cancellationToken)).Status;

            if (!state.GetRawText().Contains("\"Stale\":true", StringComparison.OrdinalIgnoreCase))
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

    public Task<DiagnosticArtifactResult> ExportTrafficWatch(
        DateTime? from,
        DateTime? to,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return SaveServerArtifact(
            "traffic-watch",
            "/admin/traffic-watch",
            cancellationToken,
            ("from", from?.ToString("O")),
            ("to", to?.ToString("O")),
            ("database", databaseName));
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
        return new DiagnosticTextSampleResult(
            "admin_logs",
            Math.Clamp(seconds, 1, 30),
            await GetServerTextSample("/admin/logs/watch", seconds, cancellationToken));
    }
}
