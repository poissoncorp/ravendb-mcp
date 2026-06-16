using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class StoragePerformanceTools
{
    [McpServerTool(Name = "inspect_storage", ReadOnly = true)]
    [Description("Storage internals deep-dive for a database. Sections: Trees (Voron tree listing), Environment (one environment's report + scratch buffers + free space; defaults to the Documents environment — set environmentName/environmentType for Index/Configuration/System), CompressionDictionaries. Choose with include; default is Trees + Environment. For high-level sizes use get_database_stats with the storage section.")]
    public static async Task<Dictionary<string, object?>> InspectStorage(
        RavenDbAdminClient client,
        [Description("Database to inspect.")] string databaseName,
        [Description("Sections to return; omit for Trees + Environment.")] StorageFacet[]? include = null,
        [Description("Environment name for the Environment section (defaults to the database's Documents environment).")] string? environmentName = null,
        [Description("Environment type for Environment: Documents (default), Index, Configuration, or System.")] string? environmentType = null,
        CancellationToken cancellationToken = default)
    {
        var sections = Facet.Resolve(include, StorageFacet.Trees, StorageFacet.Environment);
        var result = new Dictionary<string, object?>();

        if (sections.Contains(StorageFacet.Trees))
            result["trees"] = await client.GetStorageTrees(databaseName, cancellationToken);
        if (sections.Contains(StorageFacet.Environment))
            result["environment"] = await client.GetStorageEnvironmentDetails(databaseName, environmentName, environmentType, cancellationToken);
        if (sections.Contains(StorageFacet.CompressionDictionaries))
            result["compressionDictionaries"] = await client.GetStorageCompressionDictionaries(databaseName, cancellationToken);

        return result;
    }

    [McpServerTool(Name = "get_server_resources", ReadOnly = true)]
    [Description("Host/runtime resource snapshot. Sections: Metrics, Cpu, Io, Gc, Memory (includes threads), Process, LowMemoryLog, EncryptionBufferPool, StackTraces, ScriptRunners. Choose with include; default is the core set (Metrics, Cpu, Io, Gc, Memory, Process). Use for host-health and runtime investigation.")]
    public static async Task<Dictionary<string, object?>> GetServerResources(
        RavenDbAdminClient client,
        [Description("Sections to return; omit for the core set (metrics, cpu, io, gc, memory, process).")] ResourceInclude[]? include = null,
        CancellationToken cancellationToken = default)
    {
        var sections = Facet.Resolve(include,
            ResourceInclude.Metrics, ResourceInclude.Cpu, ResourceInclude.Io,
            ResourceInclude.Gc, ResourceInclude.Memory, ResourceInclude.Process);
        var result = new Dictionary<string, object?>();

        if (sections.Contains(ResourceInclude.Metrics)) result["metrics"] = await client.GetPerformanceOverview(cancellationToken);
        if (sections.Contains(ResourceInclude.Cpu)) result["cpu"] = await client.GetCpuStats(cancellationToken);
        if (sections.Contains(ResourceInclude.Io)) result["io"] = await client.GetIoStats(null, cancellationToken);
        if (sections.Contains(ResourceInclude.Gc)) result["gc"] = await client.GetGcMemoryStats(cancellationToken);
        if (sections.Contains(ResourceInclude.Memory)) result["memory"] = await client.GetOsMemoryStats(cancellationToken);
        if (sections.Contains(ResourceInclude.Process)) result["process"] = await client.GetProcessStats(cancellationToken);
        if (sections.Contains(ResourceInclude.LowMemoryLog)) result["lowMemoryLog"] = await client.GetLowMemoryLog(cancellationToken);
        if (sections.Contains(ResourceInclude.EncryptionBufferPool)) result["encryptionBufferPool"] = await client.GetEncryptionBufferPoolStats(cancellationToken);
        if (sections.Contains(ResourceInclude.StackTraces)) result["stackTraces"] = await client.GetStackTraces(cancellationToken);
        if (sections.Contains(ResourceInclude.ScriptRunners)) result["scriptRunners"] = await client.GetScriptRunners(null, cancellationToken);

        return result;
    }

    [McpServerTool(Name = "get_network_details", ReadOnly = true)]
    [Description("TCP/network details: Stats (server connection counts + bytes), Connections (active TCP connections — server-wide, or per database when databaseName is given), DatabaseInfo (endpoint a client/node uses to reach a database on a node — needs databaseName + nodeTag). Choose sections with include; default is Stats + Connections.")]
    public static async Task<Dictionary<string, object?>> GetNetworkDetails(
        RavenDbAdminClient client,
        [Description("Sections to return; omit for Stats + Connections.")] NetworkFacet[]? include = null,
        [Description("Database to scope Connections to, or required for DatabaseInfo. Omit for server-wide.")] string? databaseName = null,
        [Description("Node tag — required for DatabaseInfo (e.g. 'A').")] string? nodeTag = null,
        CancellationToken cancellationToken = default)
    {
        var sections = Facet.Resolve(include, NetworkFacet.Stats, NetworkFacet.Connections);
        var result = new Dictionary<string, object?>();

        if (sections.Contains(NetworkFacet.Stats))
            result["stats"] = await client.GetTcpStats(cancellationToken);

        if (sections.Contains(NetworkFacet.Connections))
            result["connections"] = await client.ListTcpConnections(databaseName, cancellationToken);

        if (sections.Contains(NetworkFacet.DatabaseInfo))
            result["databaseInfo"] = await client.GetDatabaseTcpInfo(
                Facet.RequireDatabase(databaseName, "DatabaseInfo"),
                Facet.Require(nodeTag, "nodeTag", "DatabaseInfo"),
                cancellationToken);

        return result;
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

public sealed record GetCpuStatsResult(JsonElement Cpu);

public sealed record GetIoStatsResult(string? DatabaseName, JsonElement Io);

public sealed record GetGcMemoryStatsResult(JsonElement Gc);

public sealed record GetOsMemoryStatsResult(JsonElement Memory);

public sealed record GetProcessStatsResult(JsonElement Process);

public sealed record GetLowMemoryLogResult(JsonElement Log);

public sealed record GetEncryptionBufferPoolStatsResult(JsonElement BufferPool);

public sealed record GetStackTracesResult(JsonElement StackTraces);

public sealed record GetScriptRunnersResult(string? DatabaseName, JsonElement ScriptRunners);

public sealed record GetTcpStatsResult(JsonElement Tcp);

public sealed record ListTcpConnectionsResult(string? DatabaseName, JsonElement Connections);
