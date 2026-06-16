using System.ComponentModel;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class DiagnosticsExpansionTools
{
    [McpServerTool(Name = "sample_live_feed", ReadOnly = true, UseStructuredContent = true)]
    [Description("Pull a live server feed for a few seconds and return what streamed in. feed selects the source: AdminLogs (operational logs), ClusterDashboard (throughput/requests/indexing/storage), TrafficWatch (HTTP/TCP requests as they happen — optional databaseName filter), GcEvents, Allocations, ThreadContention, or ThreadRunaway (a one-shot snapshot, ignores seconds). seconds is the capture window, 1-30. Returns the captured text with Truncated/Limit flags when capped.")]
    public static Task<DiagnosticTextSampleResult> SampleLiveFeed(
        RavenDbAdminClient client,
        [Description("Which live feed to pull.")] FeedKind feed,
        [Description("Capture window in seconds, 1-30 (ignored for ThreadRunaway).")] int seconds,
        [Description("For TrafficWatch: optionally scope the capture to one database.")] string? databaseName,
        CancellationToken cancellationToken)
    {
        return feed switch
        {
            FeedKind.AdminLogs => client.SampleAdminLogs(seconds, cancellationToken),
            FeedKind.ClusterDashboard => client.SampleClusterDashboard(seconds, cancellationToken),
            FeedKind.TrafficWatch => client.SampleTrafficWatch(seconds, databaseName, cancellationToken),
            FeedKind.GcEvents => client.SampleRuntimeEvents("gc", seconds, cancellationToken),
            FeedKind.Allocations => client.SampleRuntimeEvents("allocations", seconds, cancellationToken),
            FeedKind.ThreadContention => client.SampleThreadDiagnostics("contention", seconds, cancellationToken),
            FeedKind.ThreadRunaway => client.SampleThreadDiagnostics("runaway", seconds, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(feed))
        };
    }

    [McpServerTool(Name = "wait_for_completion", ReadOnly = true)]
    [Description("Block until a condition is met or the timeout elapses. condition=Operation polls a server operation (by operationId) until a terminal state (Completed/Faulted/Canceled); condition=Indexing waits until the database has no stale indexes. Returns completion flag, poll count, and last state.")]
    public static Task<WaitForConditionResult> WaitForCompletion(
        RavenDbAdminClient client,
        [Description("Database to target.")] string databaseName,
        [Description("What to wait for: Operation or Indexing.")] WaitCondition condition,
        [Description("Max seconds to wait, 1-300.")] int timeoutSeconds,
        [Description("Server operation id — required when condition is Operation.")] long? operationId,
        CancellationToken cancellationToken)
    {
        return condition switch
        {
            WaitCondition.Operation => operationId is { } id
                ? client.WaitForOperation(databaseName, id, timeoutSeconds, cancellationToken)
                : throw new ArgumentException("operationId is required when condition is Operation.", nameof(operationId)),
            WaitCondition.Indexing => client.WaitForIndexing(databaseName, timeoutSeconds, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(condition))
        };
    }

    [McpServerTool(Name = "export_server_logs", ReadOnly = true, UseStructuredContent = true)]
    [Description("Download the operational server logs (the regular runtime log stream, not the audit log) for an optional time range to a local artifact file. Returns the artifact path, content type, and byte size — not the log contents inline.")]
    public static Task<DiagnosticArtifactResult> ExportServerLogs(
        RavenDbAdminClient client,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        return client.ExportLogs(from, to, cancellationToken);
    }

    [McpServerTool(Name = "get_collection_sample_shape", ReadOnly = true)]
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

    [McpServerTool(Name = "scan_corrupted_document_ids", ReadOnly = true, UseStructuredContent = true)]
    [Description("Scan a database for corrupted document ids and write the result to a local artifact file. Returns the artifact path, content type, and byte size.")]
    public static Task<DiagnosticArtifactResult> ScanCorruptedDocumentIds(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.ScanCorruptedDocumentIds(databaseName, cancellationToken);
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
    [Description("Scan one collection for attachments referenced by documents but missing from storage, writing results to a local artifact file. 'collectionName' is required (e.g. 'Users'). Returns the artifact path, content type, and byte size.")]
    public static Task<DiagnosticArtifactResult> FindMissingAttachments(
        RavenDbAdminClient client,
        string databaseName,
        string collectionName,
        CancellationToken cancellationToken)
    {
        return client.FindMissingAttachments(databaseName, collectionName, cancellationToken);
    }

    [McpServerTool(Name = "query_metadata_only", ReadOnly = true)]
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

    [McpServerTool(Name = "collect_debug_package", ReadOnly = true, UseStructuredContent = true)]
    [Description("Download a RavenDB debug/support package (zip) to a local artifact file. scope=Server (this node), Cluster (cluster-wide), or Database (one database — needs databaseName). Returns the artifact path, content type, and byte size. Heavy; for deep support investigations.")]
    public static Task<DiagnosticArtifactResult> CollectDebugPackage(
        RavenDbAdminClient client,
        [Description("Which package to collect: Server, Cluster, or Database.")] PackageScope scope,
        [Description("Database to package — required when scope is Database.")] string? databaseName,
        CancellationToken cancellationToken)
    {
        return scope switch
        {
            PackageScope.Server => client.CollectServerInfoPackage(cancellationToken),
            PackageScope.Cluster => client.CollectClusterInfoPackage(cancellationToken),
            PackageScope.Database => client.CollectDatabaseInfoPackage(Facet.RequireDatabase(databaseName, "Database"), cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(scope))
        };
    }

    [McpServerTool(Name = "collect_diagnostic_snapshot", ReadOnly = true)]
    [Description("One-call broad snapshot for a database: cluster nodes, database overview, indexing overview, ongoing tasks, notifications, and a database info-package artifact. Good starting point for triage.")]
    public static Task<CollectDiagnosticSnapshotResult> CollectDiagnosticSnapshot(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.CollectDiagnosticSnapshot(databaseName, cancellationToken);
    }
}
