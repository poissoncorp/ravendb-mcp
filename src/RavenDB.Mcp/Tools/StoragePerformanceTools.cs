using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class StoragePerformanceTools
{
    [McpServerTool(Name = "get_storage_overview", ReadOnly = true, UseStructuredContent = true)]
    [Description("Per-database storage report plus all-environments report: tree sizes, allocated vs used bytes, and per-environment breakdown.")]
    public static Task<GetStorageOverviewResult> GetStorageOverview(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetStorageOverview(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_storage_trees", ReadOnly = true, UseStructuredContent = true)]
    [Description("Low-level storage tree listing for a database's Voron environment: tree names, types, and page/size details.")]
    public static Task<GetStorageTreesResult> GetStorageTrees(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetStorageTrees(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_storage_environment_details", ReadOnly = true, UseStructuredContent = true)]
    [Description("Deep storage detail for one environment (Documents/Index/Configuration/System): report, scratch-buffer info, and free-space snapshot. Defaults to the Documents environment of the database.")]
    public static Task<GetStorageEnvironmentDetailsResult> GetStorageEnvironmentDetails(
        RavenDbAdminClient client,
        string databaseName,
        string? environmentName,
        string? environmentType,
        CancellationToken cancellationToken)
    {
        return client.GetStorageEnvironmentDetails(databaseName, environmentName, environmentType, cancellationToken);
    }

    [McpServerTool(Name = "get_storage_tree_structure", ReadOnly = true, UseStructuredContent = true)]
    [Description("Dump the internal structure of one storage tree by name. treeKind selects btree (default) or fixed_size/fst. Returns RavenDB's raw structure text.")]
    public static Task<GetStorageTreeStructureResult> GetStorageTreeStructure(
        RavenDbAdminClient client,
        string databaseName,
        string treeName,
        string? treeKind,
        CancellationToken cancellationToken)
    {
        return client.GetStorageTreeStructure(databaseName, treeName, treeKind, cancellationToken);
    }

    [McpServerTool(Name = "get_storage_compression_dictionaries", ReadOnly = true, UseStructuredContent = true)]
    [Description("Document-compression dictionaries for a database: dictionary ids and sizes used by RavenDB's storage compression.")]
    public static Task<GetStorageCompressionDictionariesResult> GetStorageCompressionDictionaries(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetStorageCompressionDictionaries(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_server_resources", ReadOnly = true, UseStructuredContent = true)]
    [Description("Server resource snapshot: metrics, CPU, IO, GC, OS memory, process, and thread stats. Use for a quick host-health read.")]
    public static Task<GetServerResourcesResult> GetServerResources(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetServerResources(cancellationToken);
    }

    [McpServerTool(Name = "get_io_stats", ReadOnly = true, UseStructuredContent = true)]
    [Description("Disk IO metrics. Omit databaseName for server-wide IO; provide it for that database's IO. Returns read/write rates and latencies per environment/file.")]
    public static Task<GetIoStatsResult> GetIoStats(
        RavenDbAdminClient client,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetIoStats(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_low_memory_log", ReadOnly = true, UseStructuredContent = true)]
    [Description("Server low-memory event log: recent low-memory triggers and the actions RavenDB took. Use to diagnose memory pressure.")]
    public static Task<GetLowMemoryLogResult> GetLowMemoryLog(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetLowMemoryLog(cancellationToken);
    }

    [McpServerTool(Name = "get_encryption_buffer_pool_stats", ReadOnly = true, UseStructuredContent = true)]
    [Description("Encryption buffer-pool stats (relevant for encrypted databases): allocated/used secure buffers.")]
    public static Task<GetEncryptionBufferPoolStatsResult> GetEncryptionBufferPoolStats(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetEncryptionBufferPoolStats(cancellationToken);
    }

    [McpServerTool(Name = "sample_runtime_events", ReadOnly = true, UseStructuredContent = true)]
    [Description("Stream a few seconds (1-30) of runtime events. kind 'gc' samples GC events, otherwise allocations. Returns raw text with Truncated/Limit flags when capped.")]
    public static Task<SampleRuntimeEventsResult> SampleRuntimeEvents(
        RavenDbAdminClient client,
        string kind,
        int seconds,
        CancellationToken cancellationToken)
    {
        return client.SampleRuntimeEvents(kind, seconds, cancellationToken);
    }

    [McpServerTool(Name = "sample_thread_diagnostics", ReadOnly = true, UseStructuredContent = true)]
    [Description("Thread diagnostics. kind 'contention' streams a few seconds (1-30) of lock-contention events; otherwise returns the current runaway-threads snapshot. Returns raw text with Truncated/Limit flags.")]
    public static Task<SampleThreadDiagnosticsResult> SampleThreadDiagnostics(
        RavenDbAdminClient client,
        string kind,
        int seconds,
        CancellationToken cancellationToken)
    {
        return client.SampleThreadDiagnostics(kind, seconds, cancellationToken);
    }

    [McpServerTool(Name = "get_stack_traces", ReadOnly = true, UseStructuredContent = true)]
    [Description("Managed stack traces of the server's threads. Use to investigate hangs or busy threads.")]
    public static Task<GetStackTracesResult> GetStackTraces(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetStackTraces(cancellationToken);
    }

    [McpServerTool(Name = "get_script_runners", ReadOnly = true, UseStructuredContent = true)]
    [Description("JavaScript script-runner pool stats (patches, ETL/subscription scripts). Omit databaseName for server-wide, or pass it for one database.")]
    public static Task<GetScriptRunnersResult> GetScriptRunners(
        RavenDbAdminClient client,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetScriptRunners(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_tcp_stats", ReadOnly = true, UseStructuredContent = true)]
    [Description("Server TCP statistics: connection counts and bytes in/out across the server's TCP endpoints.")]
    public static Task<GetTcpStatsResult> GetTcpStats(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetTcpStats(cancellationToken);
    }

    [McpServerTool(Name = "list_tcp_connections", ReadOnly = true, UseStructuredContent = true)]
    [Description("Active TCP connections. Omit databaseName for server-wide active connections; pass it for that database's TCP connection info.")]
    public static Task<ListTcpConnectionsResult> ListTcpConnections(
        RavenDbAdminClient client,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return client.ListTcpConnections(databaseName, cancellationToken);
    }
}

public sealed record GetStorageOverviewResult(string DatabaseName, JsonElement Report, JsonElement Environments);

public sealed record GetStorageTreesResult(string DatabaseName, JsonElement Trees);

public sealed record GetStorageEnvironmentReportResult(
    string DatabaseName,
    string EnvironmentName,
    string EnvironmentType,
    JsonElement Report);

public sealed record GetStorageEnvironmentDetailsResult(
    string DatabaseName,
    string EnvironmentName,
    string EnvironmentType,
    JsonElement Report,
    JsonElement ScratchBuffers,
    JsonElement FreeSpace);

public sealed record GetStorageTreeStructureResult(
    string DatabaseName,
    string TreeName,
    string TreeKind,
    string Structure);

public sealed record GetStorageCompressionDictionariesResult(string DatabaseName, JsonElement Dictionaries);

public sealed record GetStorageScratchBufferInfoResult(
    string DatabaseName,
    string EnvironmentName,
    string EnvironmentType,
    JsonElement ScratchBuffers);

public sealed record GetStorageFreeSpaceSnapshotResult(
    string DatabaseName,
    string EnvironmentName,
    string EnvironmentType,
    JsonElement FreeSpace);

public sealed record GetPerformanceOverviewResult(JsonElement Metrics);

public sealed record GetServerResourcesResult(
    JsonElement Metrics,
    JsonElement Cpu,
    JsonElement Io,
    JsonElement Gc,
    JsonElement Memory,
    JsonElement Process,
    JsonElement Threads);

public sealed record GetCpuStatsResult(JsonElement Cpu);

public sealed record GetIoStatsResult(string? DatabaseName, JsonElement Io);

public sealed record GetGcMemoryStatsResult(JsonElement Gc);

public sealed record GetOsMemoryStatsResult(JsonElement Memory);

public sealed record GetProcessStatsResult(JsonElement Process);

public sealed record GetLowMemoryLogResult(JsonElement Log);

public sealed record GetEncryptionBufferPoolStatsResult(JsonElement BufferPool);

public sealed record SampleRuntimeEventsResult(string Kind, int Seconds, string Sample, bool Truncated, int Limit);

public sealed record SampleThreadDiagnosticsResult(string Kind, int Seconds, string Sample, bool Truncated, int Limit);

public sealed record GetStackTracesResult(JsonElement StackTraces);

public sealed record GetScriptRunnersResult(string? DatabaseName, JsonElement ScriptRunners);

public sealed record GetTcpStatsResult(JsonElement Tcp);

public sealed record ListTcpConnectionsResult(string? DatabaseName, JsonElement Connections);
