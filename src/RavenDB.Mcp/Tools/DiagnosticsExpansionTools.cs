using System.ComponentModel;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class DiagnosticsExpansionTools
{
    [McpServerTool(Name = "get_server_diagnostics_overview", ReadOnly = true, UseStructuredContent = true)]
    [Description("Server-level diagnostics in one call: routes, configuration settings, metrics, CPU credits, idle databases, license connectivity, and cluster maintenance stats. Each section is availability-wrapped.")]
    public static Task<GetServerDiagnosticsOverviewResult> GetServerDiagnosticsOverview(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetServerDiagnosticsOverview(cancellationToken);
    }

    [McpServerTool(Name = "get_cluster_diagnostics_overview", ReadOnly = true, UseStructuredContent = true)]
    [Description("Cluster-level diagnostics in one call: observer decisions, cluster log, history logs, remote connections, engine logs, and state-change history. Each section is availability-wrapped.")]
    public static Task<GetClusterDiagnosticsOverviewResult> GetClusterDiagnosticsOverview(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetClusterDiagnosticsOverview(cancellationToken);
    }

    [McpServerTool(Name = "ping_cluster_node", ReadOnly = true, UseStructuredContent = true)]
    [Description("Ping a node URL's debug endpoint and measure latency. Returns status code, success, elapsed ms, and any error. Use to check a specific node's reachability.")]
    public static Task<PingClusterNodeResult> PingClusterNode(
        RavenDbAdminClient client,
        string url,
        CancellationToken cancellationToken)
    {
        return client.PingClusterNode(url, cancellationToken);
    }

    [McpServerTool(Name = "sample_cluster_dashboard", ReadOnly = true, UseStructuredContent = true)]
    [Description("Stream a few seconds (1-30) of the live cluster dashboard feed (throughput, requests, indexing, storage). Returns raw text with Truncated/Limit flags when capped.")]
    public static Task<DiagnosticTextSampleResult> SampleClusterDashboard(
        RavenDbAdminClient client,
        int seconds,
        CancellationToken cancellationToken)
    {
        return client.SampleClusterDashboard(seconds, cancellationToken);
    }

    [McpServerTool(Name = "get_index_staleness", ReadOnly = true, UseStructuredContent = true)]
    [Description("Whether one index is stale and why: the staleness reasons RavenDB reports (pending documents/tombstones to index).")]
    public static Task<GetIndexStalenessResult> GetIndexStaleness(
        RavenDbAdminClient client,
        string databaseName,
        string indexName,
        CancellationToken cancellationToken)
    {
        return client.GetIndexStaleness(databaseName, indexName, cancellationToken);
    }

    [McpServerTool(Name = "get_index_debug_details", ReadOnly = true, UseStructuredContent = true)]
    [Description("Low-level debug detail for one index: internal debug view, metadata, and definition history. Each section is availability-wrapped.")]
    public static Task<GetIndexDebugDetailsResult> GetIndexDebugDetails(
        RavenDbAdminClient client,
        string databaseName,
        string indexName,
        CancellationToken cancellationToken)
    {
        return client.GetIndexDebugDetails(databaseName, indexName, cancellationToken);
    }

    [McpServerTool(Name = "get_query_diagnostics", ReadOnly = true, UseStructuredContent = true)]
    [Description("Query diagnostics for a database: currently running queries and the query-cache contents. Each section is availability-wrapped.")]
    public static Task<GetQueryDiagnosticsResult> GetQueryDiagnostics(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetQueryDiagnostics(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_operations_overview", ReadOnly = true, UseStructuredContent = true)]
    [Description("Running operations overview: per-database running operations (when databaseName is given) plus server-wide longest-running operations.")]
    public static Task<GetOperationsOverviewResult> GetOperationsOverview(
        RavenDbAdminClient client,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetOperationsOverview(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_transaction_diagnostics", ReadOnly = true, UseStructuredContent = true)]
    [Description("Transaction diagnostics: server-wide tx info plus (when databaseName is given) the database's tx info and cluster-transaction info. Sections are availability-wrapped.")]
    public static Task<GetTransactionDiagnosticsResult> GetTransactionDiagnostics(
        RavenDbAdminClient client,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetTransactionDiagnostics(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "wait_for_operation", ReadOnly = true, UseStructuredContent = true)]
    [Description("Poll a server operation until it reaches a terminal state (Completed/Faulted/Canceled) or the timeout (1-300s) elapses. Returns completion flag, poll count, and last operation state.")]
    public static Task<WaitForConditionResult> WaitForOperation(
        RavenDbAdminClient client,
        string databaseName,
        long operationId,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        return client.WaitForOperation(databaseName, operationId, timeoutSeconds, cancellationToken);
    }

    [McpServerTool(Name = "wait_for_indexing", ReadOnly = true, UseStructuredContent = true)]
    [Description("Block until the database has no stale indexes or the timeout (1-300s) elapses. Returns completion flag, poll count, and the last DatabaseStatistics.")]
    public static Task<WaitForConditionResult> WaitForIndexing(
        RavenDbAdminClient client,
        string databaseName,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        return client.WaitForIndexing(databaseName, timeoutSeconds, cancellationToken);
    }

    [McpServerTool(Name = "get_document_conflicts", ReadOnly = true, UseStructuredContent = true)]
    [Description("Replication conflicts for one document id: conflicting change vectors/versions, if any exist.")]
    public static Task<GetDocumentConflictsResult> GetDocumentConflicts(
        RavenDbAdminClient client,
        string databaseName,
        string documentId,
        CancellationToken cancellationToken)
    {
        return client.GetDocumentConflicts(databaseName, documentId, cancellationToken);
    }

    [McpServerTool(Name = "get_backup_diagnostics", ReadOnly = true, UseStructuredContent = true)]
    [Description("Backup diagnostics for a database: backup tasks, next-occurrence timers, and server-wide backup configurations. Sections are availability-wrapped.")]
    public static Task<GetBackupDiagnosticsResult> GetBackupDiagnostics(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetBackupDiagnostics(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_etl_diagnostics", ReadOnly = true, UseStructuredContent = true)]
    [Description("ETL diagnostics for a database: ETL tasks, stats, performance, debug stats, and progress. Sections are availability-wrapped.")]
    public static Task<GetEtlDiagnosticsResult> GetEtlDiagnostics(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetEtlDiagnostics(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_subscription_diagnostics", ReadOnly = true, UseStructuredContent = true)]
    [Description("Subscription diagnostics for a database: subscriptions, their state, and active connection details. Sections are availability-wrapped.")]
    public static Task<GetSubscriptionDiagnosticsResult> GetSubscriptionDiagnostics(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetSubscriptionDiagnostics(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_traffic_watch_configuration", ReadOnly = true, UseStructuredContent = true)]
    [Description("Current traffic-watch configuration: capture mode, filtered databases, and HTTP/TCP change types being recorded.")]
    public static Task<GetTrafficWatchConfigurationResult> GetTrafficWatchConfiguration(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetTrafficWatchConfiguration(cancellationToken);
    }

    [McpServerTool(Name = "export_logs", ReadOnly = true, UseStructuredContent = true)]
    [Description("Download server logs for an optional time range to a local artifact file. Returns the artifact path, content type, and byte size (not the log contents inline).")]
    public static Task<DiagnosticArtifactResult> ExportLogs(
        RavenDbAdminClient client,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        return client.ExportLogs(from, to, cancellationToken);
    }

    [McpServerTool(Name = "export_traffic_watch", ReadOnly = true, UseStructuredContent = true)]
    [Description("Download captured traffic-watch data (optional time range and database) to a local artifact file. Returns the artifact path, content type, and byte size.")]
    public static Task<DiagnosticArtifactResult> ExportTrafficWatch(
        RavenDbAdminClient client,
        DateTime? from,
        DateTime? to,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return client.ExportTrafficWatch(from, to, databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_notifications", ReadOnly = true, UseStructuredContent = true)]
    [Description("Active notifications/alerts. Omit databaseName for server-wide notifications; pass it for that database's notifications (alerts, performance hints, errors).")]
    public static Task<GetNotificationsResult> GetNotifications(
        RavenDbAdminClient client,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetNotifications(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "sample_admin_logs", ReadOnly = true, UseStructuredContent = true)]
    [Description("Stream a few seconds (1-30) of the live server log feed. Returns raw text with Truncated/Limit flags when capped. Use for a quick look at current logging.")]
    public static Task<DiagnosticTextSampleResult> SampleAdminLogs(
        RavenDbAdminClient client,
        int seconds,
        CancellationToken cancellationToken)
    {
        return client.SampleAdminLogs(seconds, cancellationToken);
    }

    [McpServerTool(Name = "get_collection_sample_shape", ReadOnly = true, UseStructuredContent = true)]
    [Description("Infer the field shape of a collection by sampling a few documents (sampleSize 1-25). Returns field name -> observed JSON value kinds (no document values). Use to learn a collection's structure cheaply.")]
    public static Task<GetCollectionSampleShapeResult> GetCollectionSampleShape(
        RavenDbAdminClient client,
        string databaseName,
        string collectionName,
        int? sampleSize,
        CancellationToken cancellationToken)
    {
        return client.GetCollectionSampleShape(databaseName, collectionName, sampleSize, cancellationToken);
    }

    [McpServerTool(Name = "get_huge_documents_report", ReadOnly = true, UseStructuredContent = true)]
    [Description("Documents RavenDB flagged as unusually large for a database: ids and sizes. Use to find bloated documents.")]
    public static Task<GetHugeDocumentsReportResult> GetHugeDocumentsReport(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetHugeDocumentsReport(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "scan_corrupted_document_ids", ReadOnly = true, UseStructuredContent = true)]
    [Description("Scan a database for corrupted document ids and write the result to a local artifact file. Returns the artifact path, content type, and byte size.")]
    public static Task<DiagnosticArtifactResult> ScanCorruptedDocumentIds(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.ScanCorruptedDocumentIds(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_document_revisions", ReadOnly = true, UseStructuredContent = true)]
    [Description("Paged revision history for one document id (start/pageSize, 1-1024). Returns the stored revisions including their contents.")]
    public static Task<GetDocumentRevisionsResult> GetDocumentRevisions(
        RavenDbAdminClient client,
        string databaseName,
        string documentId,
        int? start,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        return client.GetDocumentRevisions(databaseName, documentId, start, pageSize, cancellationToken);
    }

    [McpServerTool(Name = "export_document_ids", ReadOnly = true, UseStructuredContent = true)]
    [Description("Export document ids (metadata-only, optional startsWith prefix, pageSize 1-10000) to a local artifact file. Returns the artifact path, content type, and byte size (ids only, no document bodies).")]
    public static Task<DiagnosticArtifactResult> ExportDocumentIds(
        RavenDbAdminClient client,
        string databaseName,
        string? startsWith,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        return client.ExportDocumentIds(databaseName, startsWith, pageSize, cancellationToken);
    }

    [McpServerTool(Name = "find_missing_attachments", ReadOnly = true, UseStructuredContent = true)]
    [Description("Scan a database for attachments referenced by documents but missing from storage, writing results to a local artifact file. Returns the artifact path, content type, and byte size.")]
    public static Task<DiagnosticArtifactResult> FindMissingAttachments(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.FindMissingAttachments(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_revisions_collection_stats", ReadOnly = true, UseStructuredContent = true)]
    [Description("Revision storage stats per collection for a database: how many revisions and how much space revisions consume.")]
    public static Task<GetRevisionsCollectionStatsResult> GetRevisionsCollectionStats(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetRevisionsCollectionStats(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "query_metadata_only", ReadOnly = true, UseStructuredContent = true)]
    [Description("Run an RQL query but return ONLY metadata (duration, total/skipped/scanned results, index, staleness, last query time, result etag) — no rows. Cheap query probe. Use run_query when you need the rows.")]
    public static Task<QueryMetadataOnlyResult> QueryMetadataOnly(
        RavenDbAdminClient client,
        string databaseName,
        string query,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        return client.QueryMetadataOnly(databaseName, query, pageSize, cancellationToken);
    }

    [McpServerTool(Name = "collect_server_info_package", ReadOnly = true, UseStructuredContent = true)]
    [Description("Download RavenDB's full server info/debug package (zip) to a local artifact file. Returns the artifact path, content type, and byte size. Heavy; for deep support investigations.")]
    public static Task<DiagnosticArtifactResult> CollectServerInfoPackage(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.CollectServerInfoPackage(cancellationToken);
    }

    [McpServerTool(Name = "collect_cluster_info_package", ReadOnly = true, UseStructuredContent = true)]
    [Description("Download RavenDB's cluster-wide info/debug package (zip) to a local artifact file. Returns the artifact path, content type, and byte size. Heavy; for deep support investigations.")]
    public static Task<DiagnosticArtifactResult> CollectClusterInfoPackage(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.CollectClusterInfoPackage(cancellationToken);
    }

    [McpServerTool(Name = "collect_database_info_package", ReadOnly = true, UseStructuredContent = true)]
    [Description("Download one database's info/debug package (zip) to a local artifact file. Returns the artifact path, content type, and byte size.")]
    public static Task<DiagnosticArtifactResult> CollectDatabaseInfoPackage(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.CollectDatabaseInfoPackage(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "collect_diagnostic_snapshot", ReadOnly = true, UseStructuredContent = true)]
    [Description("One-call broad snapshot for a database: cluster nodes, database overview, indexing overview, ongoing tasks, notifications, and a database info-package artifact. Good starting point for triage.")]
    public static Task<CollectDiagnosticSnapshotResult> CollectDiagnosticSnapshot(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.CollectDiagnosticSnapshot(databaseName, cancellationToken);
    }
}
