using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;
using System.ComponentModel;
using System.Text.Json;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class ServerTools
{
    [McpServerTool(Name = "get_cluster_overview", ReadOnly = true)]
    [Description("Cluster and server overview. Sections: Nodes (topology, leader, per-node tag/type/url/health), ServerInfo (build/version + contacted node), ServerDiagnostics (routes/settings/metrics/license/idle DBs), ClusterDiagnostics (observer decisions, cluster log, engine logs). Choose with include; default is Nodes + ServerInfo. For alerts/hints use get_notifications.")]
    public static async Task<Dictionary<string, object?>> GetClusterOverview(
        RavenDbAdminClient client,
        [Description("Sections to return; omit for Nodes + ServerInfo.")] ClusterInclude[]? include = null,
        CancellationToken cancellationToken = default)
    {
        var sections = Facet.Resolve(include, ClusterInclude.Nodes, ClusterInclude.ServerInfo);
        var result = new Dictionary<string, object?>();

        if (sections.Contains(ClusterInclude.Nodes)) result["nodes"] = await client.GetClusterNodes(cancellationToken);
        if (sections.Contains(ClusterInclude.ServerInfo)) result["serverInfo"] = await client.GetServerInfo(cancellationToken);
        if (sections.Contains(ClusterInclude.ServerDiagnostics)) result["serverDiagnostics"] = await client.GetServerDiagnosticsOverview(cancellationToken);
        if (sections.Contains(ClusterInclude.ClusterDiagnostics)) result["clusterDiagnostics"] = await client.GetClusterDiagnosticsOverview(cancellationToken);

        return result;
    }

    [McpServerTool(Name = "get_notifications", ReadOnly = true)]
    [Description("Active RavenDB notifications — alerts, performance hints, and operation/error notices. Omit databaseName for server-wide notifications; pass it to scope to one database. Returns the raw notification list (group/categorize client-side as needed).")]
    public static Task<GetNotificationsResult> GetNotifications(
        RavenDbAdminClient client,
        [Description("Database to scope to; omit for server-wide notifications.")] string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        return client.GetNotifications(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_server_config", ReadOnly = true)]
    [Description("Server-scoped configuration. Sections: Logs (mode/levels/paths/retention), ClientConfig (server-wide client config pushed to all clients), TrafficWatch (capture configuration), Studio (environment banner, disabled UI features). Choose with include; default is all. For per-database configuration use get_database_config.")]
    public static async Task<Dictionary<string, object?>> GetServerConfig(
        RavenDbAdminClient client,
        [Description("Sections to return; omit for all.")] ServerConfigSection[]? include = null,
        CancellationToken cancellationToken = default)
    {
        var sections = Facet.Resolve(include,
            ServerConfigSection.Logs, ServerConfigSection.ClientConfig,
            ServerConfigSection.TrafficWatch, ServerConfigSection.Studio);
        var result = new Dictionary<string, object?>();

        if (sections.Contains(ServerConfigSection.Logs)) result["logs"] = await client.GetLogsConfiguration(cancellationToken);
        if (sections.Contains(ServerConfigSection.ClientConfig)) result["clientConfig"] = await client.GetServerWideClientConfiguration(cancellationToken);
        if (sections.Contains(ServerConfigSection.TrafficWatch)) result["trafficWatch"] = await client.GetTrafficWatchConfiguration(cancellationToken);
        if (sections.Contains(ServerConfigSection.Studio)) result["studio"] = await client.GetServerStudioConfiguration(cancellationToken);

        return result;
    }
}

public sealed record ServerBuildResult(
    string ProductVersion,
    int BuildVersion,
    string? AssemblyVersion,
    string CommitHash,
    string FullVersion);

public sealed record GetServerInfoResult(
    string ProductVersion,
    int BuildVersion,
    string CommitHash,
    string FullVersion,
    JsonElement NodeInfo);

public sealed record CurrentNodeResult(
    string? NodeTag,
    Guid ServerId,
    string? TopologyId,
    string? ClusterStatus,
    string? CurrentState,
    string? ServerRole,
    int ServerSchemaVersion,
    bool HasFixedPort,
    int NumberOfCores,
    double InstalledMemoryInGb,
    double UsableMemoryInGb,
    bool CertificatePresent,
    OsInfoResult? Os);

public sealed record OsInfoResult(
    string? Type,
    string? FullName,
    string? Version,
    string? BuildVersion,
    bool Is64Bit);

public sealed record ClusterNodeStatusResult(
    string? Name,
    bool Connected,
    DateTime LastSent,
    DateTime LastReply,
    string? LastSentMessage,
    long LastMatchingIndex,
    string? ErrorDetails);

public sealed record ClusterNodeResult(
    string Tag,
    string Type,
    string? Url,
    ClusterNodeStatusResult? Status,
    ServerBuildResult? Server,
    CurrentNodeResult? Self,
    string? Error);

public sealed record ClusterResult(
    string? TopologyId,
    long Etag,
    string? Leader,
    string? RespondingNodeTag,
    string? RespondingNodeRole,
    string? LastNodeId,
    ClusterNodeResult[] Nodes);

public sealed record GetClusterNodesResult(
    ServerBuildResult Server,
    CurrentNodeResult CurrentNode,
    ClusterResult Cluster);

public sealed record GetLogsConfigurationToolResult(JsonElement Configuration);

public sealed record GetServerWideClientConfigurationResult(JsonElement Configuration);
