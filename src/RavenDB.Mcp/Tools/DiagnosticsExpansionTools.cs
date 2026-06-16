using System.ComponentModel;
using ModelContextProtocol;
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
        [Description("For TrafficWatch: optionally scope the capture to one database.")] string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        return feed switch
        {
            FeedKind.AdminLogs => client.SampleAdminLogs(seconds, cancellationToken),
            FeedKind.ClusterDashboard => client.SampleClusterDashboard(seconds, cancellationToken),
            FeedKind.TrafficWatch => client.SampleTrafficWatch(seconds, databaseName, cancellationToken),
            FeedKind.GcEvents => client.SampleGcEvents(seconds, cancellationToken),
            FeedKind.Allocations => client.SampleAllocations(seconds, cancellationToken),
            FeedKind.ThreadContention => client.SampleThreadContention(seconds, cancellationToken),
            FeedKind.ThreadRunaway => client.SampleThreadRunaway(cancellationToken),
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
        [Description("Server operation id — required when condition is Operation.")] long? operationId = null,
        CancellationToken cancellationToken = default)
    {
        return condition switch
        {
            WaitCondition.Operation => operationId is { } id
                ? client.WaitForOperation(databaseName, id, timeoutSeconds, cancellationToken)
                : throw new McpException("operationId is required when condition is Operation."),
            WaitCondition.Indexing => client.WaitForIndexing(databaseName, timeoutSeconds, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(condition))
        };
    }

    [McpServerTool(Name = "export_server_logs", ReadOnly = true, UseStructuredContent = true)]
    [Description("Download the operational server logs (the regular runtime log stream, not the audit log) for an optional time range to a local artifact file. Returns the artifact path, content type, and byte size — not the log contents inline.")]
    public static Task<DiagnosticArtifactResult> ExportServerLogs(
        RavenDbAdminClient client,
        [Description("Start of the time range (ISO-8601); omit for no lower bound.")] DateTime? from = null,
        [Description("End of the time range (ISO-8601); omit for no upper bound.")] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        return client.ExportLogs(from, to, cancellationToken);
    }

    [McpServerTool(Name = "collect_debug_package", ReadOnly = true, UseStructuredContent = true)]
    [Description("Download a RavenDB debug/support package (zip) to a local artifact file. scope=Server (this node), Cluster (cluster-wide), or Database (one database — needs databaseName). Returns the artifact path, content type, and byte size. Heavy; for deep support investigations.")]
    public static Task<DiagnosticArtifactResult> CollectDebugPackage(
        RavenDbAdminClient client,
        [Description("Which package to collect: Server, Cluster, or Database.")] PackageScope scope,
        [Description("Database to package — required when scope is Database.")] string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        return scope switch
        {
            PackageScope.Server => client.CollectServerInfoPackage(cancellationToken),
            PackageScope.Cluster => client.CollectClusterInfoPackage(cancellationToken),
            PackageScope.Database => client.CollectDatabaseInfoPackage(Facet.RequireDatabase(databaseName, "Database"), cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(scope))
        };
    }
}
