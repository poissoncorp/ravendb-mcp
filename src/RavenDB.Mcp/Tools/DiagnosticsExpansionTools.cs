using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class DiagnosticsExpansionTools
{
    [McpServerTool(Name = "get_server_diagnostics_overview", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetServerDiagnosticsOverviewResult> GetServerDiagnosticsOverview(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetServerDiagnosticsOverview(cancellationToken);
    }

    [McpServerTool(Name = "get_cluster_diagnostics_overview", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetClusterDiagnosticsOverviewResult> GetClusterDiagnosticsOverview(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetClusterDiagnosticsOverview(cancellationToken);
    }

    [McpServerTool(Name = "ping_cluster_node", ReadOnly = true, UseStructuredContent = true)]
    public static Task<PingClusterNodeResult> PingClusterNode(
        RavenDbAdminClient client,
        string url,
        CancellationToken cancellationToken)
    {
        return client.PingClusterNode(url, cancellationToken);
    }

    [McpServerTool(Name = "sample_cluster_dashboard", ReadOnly = true, UseStructuredContent = true)]
    public static Task<DiagnosticTextSampleResult> SampleClusterDashboard(
        RavenDbAdminClient client,
        int seconds,
        CancellationToken cancellationToken)
    {
        return client.SampleClusterDashboard(seconds, cancellationToken);
    }

    [McpServerTool(Name = "get_index_staleness", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetIndexStalenessResult> GetIndexStaleness(
        RavenDbAdminClient client,
        string databaseName,
        string indexName,
        CancellationToken cancellationToken)
    {
        return client.GetIndexStaleness(databaseName, indexName, cancellationToken);
    }

    [McpServerTool(Name = "get_index_debug_details", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetIndexDebugDetailsResult> GetIndexDebugDetails(
        RavenDbAdminClient client,
        string databaseName,
        string indexName,
        CancellationToken cancellationToken)
    {
        return client.GetIndexDebugDetails(databaseName, indexName, cancellationToken);
    }

    [McpServerTool(Name = "get_query_diagnostics", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetQueryDiagnosticsResult> GetQueryDiagnostics(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetQueryDiagnostics(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_operations_overview", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetOperationsOverviewResult> GetOperationsOverview(
        RavenDbAdminClient client,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetOperationsOverview(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_transaction_diagnostics", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetTransactionDiagnosticsResult> GetTransactionDiagnostics(
        RavenDbAdminClient client,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetTransactionDiagnostics(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "wait_for_operation", ReadOnly = true, UseStructuredContent = true)]
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
    public static Task<WaitForConditionResult> WaitForIndexing(
        RavenDbAdminClient client,
        string databaseName,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        return client.WaitForIndexing(databaseName, timeoutSeconds, cancellationToken);
    }

    [McpServerTool(Name = "get_document_conflicts", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetDocumentConflictsResult> GetDocumentConflicts(
        RavenDbAdminClient client,
        string databaseName,
        string documentId,
        CancellationToken cancellationToken)
    {
        return client.GetDocumentConflicts(databaseName, documentId, cancellationToken);
    }

    [McpServerTool(Name = "get_backup_diagnostics", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetBackupDiagnosticsResult> GetBackupDiagnostics(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetBackupDiagnostics(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_etl_diagnostics", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetEtlDiagnosticsResult> GetEtlDiagnostics(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetEtlDiagnostics(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_subscription_diagnostics", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetSubscriptionDiagnosticsResult> GetSubscriptionDiagnostics(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetSubscriptionDiagnostics(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_traffic_watch_configuration", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetTrafficWatchConfigurationResult> GetTrafficWatchConfiguration(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetTrafficWatchConfiguration(cancellationToken);
    }

    [McpServerTool(Name = "export_logs", ReadOnly = true, UseStructuredContent = true)]
    public static Task<DiagnosticArtifactResult> ExportLogs(
        RavenDbAdminClient client,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        return client.ExportLogs(from, to, cancellationToken);
    }

    [McpServerTool(Name = "export_traffic_watch", ReadOnly = true, UseStructuredContent = true)]
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
    public static Task<GetNotificationsResult> GetNotifications(
        RavenDbAdminClient client,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetNotifications(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "sample_admin_logs", ReadOnly = true, UseStructuredContent = true)]
    public static Task<DiagnosticTextSampleResult> SampleAdminLogs(
        RavenDbAdminClient client,
        int seconds,
        CancellationToken cancellationToken)
    {
        return client.SampleAdminLogs(seconds, cancellationToken);
    }

    [McpServerTool(Name = "get_collection_sample_shape", ReadOnly = true, UseStructuredContent = true)]
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
    public static Task<GetHugeDocumentsReportResult> GetHugeDocumentsReport(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetHugeDocumentsReport(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "scan_corrupted_document_ids", ReadOnly = true, UseStructuredContent = true)]
    public static Task<DiagnosticArtifactResult> ScanCorruptedDocumentIds(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.ScanCorruptedDocumentIds(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_document_revisions", ReadOnly = true, UseStructuredContent = true)]
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
    public static Task<DiagnosticArtifactResult> FindMissingAttachments(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.FindMissingAttachments(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_revisions_collection_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetRevisionsCollectionStatsResult> GetRevisionsCollectionStats(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetRevisionsCollectionStats(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "query_metadata_only", ReadOnly = true, UseStructuredContent = true)]
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
    public static Task<DiagnosticArtifactResult> CollectServerInfoPackage(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.CollectServerInfoPackage(cancellationToken);
    }

    [McpServerTool(Name = "collect_cluster_info_package", ReadOnly = true, UseStructuredContent = true)]
    public static Task<DiagnosticArtifactResult> CollectClusterInfoPackage(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.CollectClusterInfoPackage(cancellationToken);
    }

    [McpServerTool(Name = "collect_database_info_package", ReadOnly = true, UseStructuredContent = true)]
    public static Task<DiagnosticArtifactResult> CollectDatabaseInfoPackage(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.CollectDatabaseInfoPackage(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "collect_diagnostic_snapshot", ReadOnly = true, UseStructuredContent = true)]
    public static Task<CollectDiagnosticSnapshotResult> CollectDiagnosticSnapshot(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.CollectDiagnosticSnapshot(databaseName, cancellationToken);
    }
}
