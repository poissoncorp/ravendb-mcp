namespace RavenDB.Mcp.Tools;

// Selector enums for parameterized facet tools (ADR-0010). The MCP SDK maps each enum to a
// JSON-Schema `enum`, so the agent sees the exact allowed values. One enum per facet tool;
// added alongside the tool that uses it (no unused selectors).

/// <summary>Which live server feed <c>sample_live_feed</c> listens to.</summary>
public enum FeedKind
{
    /// <summary>Operational server logs (live).</summary>
    AdminLogs,

    /// <summary>Cluster dashboard feed (throughput, requests, indexing, storage).</summary>
    ClusterDashboard,

    /// <summary>Traffic-watch feed (HTTP/TCP requests as they happen). Optional databaseName filter.</summary>
    TrafficWatch,

    /// <summary>GC events.</summary>
    GcEvents,

    /// <summary>Allocation events.</summary>
    Allocations,

    /// <summary>Lock-contention events.</summary>
    ThreadContention,

    /// <summary>Runaway-threads snapshot.</summary>
    ThreadRunaway
}

/// <summary>What <c>wait_for_completion</c> blocks on.</summary>
public enum WaitCondition
{
    /// <summary>Wait for a server operation (by operationId) to reach a terminal state.</summary>
    Operation,

    /// <summary>Wait until the database has no stale indexes.</summary>
    Indexing
}

/// <summary>Live runtime activity returned by <c>get_live_workload</c>.</summary>
public enum WorkloadInclude
{
    /// <summary>Running operations (or the longest-running, server-wide).</summary>
    Operations,

    /// <summary>Currently running queries and the query cache.</summary>
    Queries,

    /// <summary>Server, database, and cluster transaction info.</summary>
    Transactions
}

/// <summary>What <c>get_network_details</c> returns.</summary>
public enum NetworkFacet
{
    /// <summary>Server TCP statistics (connection counts, bytes in/out).</summary>
    Stats,

    /// <summary>Active TCP connections (server-wide, or per database).</summary>
    Connections,

    /// <summary>TCP endpoint info to reach a database on a node (needs databaseName + nodeTag).</summary>
    DatabaseInfo
}

/// <summary>Host/runtime sections returned by <c>get_server_resources</c>.</summary>
public enum ResourceInclude
{
    Metrics,
    Cpu,
    Io,
    Gc,
    Memory,
    Process,
    LowMemoryLog,
    EncryptionBufferPool,
    StackTraces,
    ScriptRunners
}

/// <summary>Per-database statistics/state sections returned by <c>get_database_stats</c>.</summary>
public enum DatabaseStatsInclude
{
    Summary,
    Detailed,
    Collections,
    Indexing,
    IndexErrors,
    IndexPerformance,
    Storage,
    Tombstones,
    Metrics,
    Identities,
    Revisions,
    Sharding,
    HugeDocuments,
    Io
}

/// <summary>Database-scoped configuration sections returned by <c>get_database_config</c>.</summary>
public enum DatabaseConfigSection
{
    /// <summary>Effective database configuration keys/values.</summary>
    Settings,

    /// <summary>Per-database client configuration pushed to clients.</summary>
    ClientConfig,

    /// <summary>Studio configuration (environment banner, disabled UI features).</summary>
    Studio,

    /// <summary>Document-expiration policy (from the database record).</summary>
    Expiration,

    /// <summary>Document-refresh policy (from the database record).</summary>
    Refresh,

    /// <summary>Data-archival policy (from the database record).</summary>
    DataArchival,

    /// <summary>Revisions configuration (from the database record).</summary>
    Revisions,

    /// <summary>Documents-compression configuration (from the database record).</summary>
    DocumentsCompression,

    /// <summary>Time-series configuration (from the database record).</summary>
    TimeSeries,

    /// <summary>Schema-validation configuration (from the database record).</summary>
    SchemaValidation
}

/// <summary>Per-document sections returned by <c>get_document_data</c>.</summary>
public enum DocumentInclude
{
    /// <summary>The document body and its @metadata.</summary>
    Document,

    /// <summary>The document's counters.</summary>
    Counters,

    /// <summary>The document's attachments (names, sizes, hashes from @metadata).</summary>
    Attachments,

    /// <summary>A time series on the document (requires timeSeriesName).</summary>
    TimeSeries,

    /// <summary>The document's revision history.</summary>
    Revisions,

    /// <summary>Replication conflicts for the document.</summary>
    Conflicts
}

/// <summary>Per-index views returned by <c>get_index</c>.</summary>
public enum IndexInclude
{
    /// <summary>Index definition (maps/reduce, fields, configuration, lock/deployment mode).</summary>
    Definition,

    /// <summary>Whether the index is stale and why.</summary>
    Staleness,

    /// <summary>Internal debug view, metadata, and definition history.</summary>
    Debug,

    /// <summary>Distinct indexed terms for a field (requires fieldName).</summary>
    Terms,

    /// <summary>This index's indexing errors.</summary>
    Errors,

    /// <summary>This index's performance statistics.</summary>
    Performance
}

/// <summary>Storage deep-dive sections returned by <c>inspect_storage</c>.</summary>
public enum StorageFacet
{
    /// <summary>Voron storage tree listing (names, types, sizes).</summary>
    Trees,

    /// <summary>One environment's report + scratch buffers + free space (defaults to Documents).</summary>
    Environment,

    /// <summary>Document-compression dictionaries (ids and sizes).</summary>
    CompressionDictionaries
}

/// <summary>Server-scoped configuration sections returned by <c>get_server_config</c>.</summary>
public enum ServerConfigSection
{
    /// <summary>Log mode/levels, paths, retention.</summary>
    Logs,

    /// <summary>Server-wide client configuration (read balance, load balancing, max requests).</summary>
    ClientConfig,

    /// <summary>Traffic-watch capture configuration.</summary>
    TrafficWatch,

    /// <summary>Studio configuration (environment banner, disabled UI features).</summary>
    Studio
}

/// <summary>Sections returned by <c>get_cluster_overview</c>.</summary>
public enum ClusterInclude
{
    /// <summary>Topology, leader, and per-node tag/type/url/health.</summary>
    Nodes,

    /// <summary>Build/version and the contacted node's info.</summary>
    ServerInfo,

    /// <summary>Server-level diagnostics (routes, settings, metrics, license, idle DBs, ...).</summary>
    ServerDiagnostics,

    /// <summary>Cluster-level diagnostics (observer decisions, cluster log, engine logs, ...).</summary>
    ClusterDiagnostics
}

/// <summary>Which debug package <c>collect_debug_package</c> downloads.</summary>
public enum PackageScope
{
    /// <summary>This node's full server debug package.</summary>
    Server,

    /// <summary>Cluster-wide debug package.</summary>
    Cluster,

    /// <summary>One database's debug package (needs databaseName).</summary>
    Database
}

/// <summary>Shared helpers for parameterized facet tools.</summary>
internal static class Facet
{
    /// <summary>The requested selectors, or <paramref name="defaults"/> when none were supplied.</summary>
    public static HashSet<T> Resolve<T>(T[]? requested, params T[] defaults) where T : struct, Enum
        => new(requested is { Length: > 0 } ? requested : defaults);

    /// <summary>Guard a required argument for a facet section.</summary>
    public static string Require(string? value, string argName, string forSection)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{argName} is required for the '{forSection}' section.", argName)
            : value;

    /// <summary>Guard a facet that requires a database name.</summary>
    public static string RequireDatabase(string? databaseName, string forSection)
        => Require(databaseName, "databaseName", forSection);
}
