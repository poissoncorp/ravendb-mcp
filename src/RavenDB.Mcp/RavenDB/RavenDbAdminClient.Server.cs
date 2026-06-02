using Raven.Client.ServerWide.Commands;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Raven.Client.ServerWide.Operations.Configuration;
using Raven.Client.ServerWide.Operations.Logs;
using Raven.Client.Http;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    public async Task<GetServerInfoResult> GetServerInfo(CancellationToken cancellationToken)
    {
        var buildNumber = await store.Maintenance.Server.SendAsync(
            new GetBuildNumberOperation(),
            cancellationToken);
        var nodeInfo = await ExecuteServerCommand(new GetNodeInfoCommand(), cancellationToken);

        return new GetServerInfoResult(
            buildNumber.ProductVersion,
            buildNumber.BuildVersion,
            buildNumber.CommitHash,
            buildNumber.FullVersion,
            ToJson(nodeInfo));
    }

    public async Task<GetClusterNodesResult> GetClusterNodes(CancellationToken cancellationToken)
    {
        var server = await store.Maintenance.Server.SendAsync(
            new GetBuildNumberOperation(),
            cancellationToken);
        var currentNode = await ExecuteServerCommand(new GetNodeInfoCommand(), cancellationToken);
        var topology = await ExecuteServerCommand(new GetClusterTopologyCommand(), cancellationToken);

        var serverBuild = ToServerBuild(server);
        var self = ToCurrentNode(currentNode);

        var nodes = new List<ClusterNodeResult>();

        foreach (var (tag, url) in topology.Topology.AllNodes.OrderBy(node => node.Key, StringComparer.OrdinalIgnoreCase))
        {
            NodeStatus? status = null;
            topology.Status?.TryGetValue(tag, out status);

            // Build/self info is only populated for the contacted node. Probing every node
            // would require a DocumentStore per URL; the server targets one cluster, and
            // per-node reachability is covered by topology Status and ping_cluster_node.
            var isContacted = string.Equals(tag, currentNode.NodeTag, StringComparison.OrdinalIgnoreCase);

            nodes.Add(new ClusterNodeResult(
                tag,
                GetNodeType(tag, topology),
                url,
                status is null ? null : ToClusterNodeStatus(status),
                isContacted ? serverBuild : null,
                isContacted ? self : null,
                null));
        }

        return new GetClusterNodesResult(
            serverBuild,
            self,
            new ClusterResult(
                topology.Topology.TopologyId,
                topology.Topology.Etag,
                topology.Leader,
                topology.NodeTag,
                topology.ServerRole.ToString(),
                topology.Topology.LastNodeId,
                [.. nodes]));
    }

    public async Task<GetLogsConfigurationToolResult> GetLogsConfiguration(CancellationToken cancellationToken)
    {
        var configuration = await store.Maintenance.Server.SendAsync(
            new GetLogsConfigurationOperation(),
            cancellationToken);

        return new GetLogsConfigurationToolResult(ToJson(configuration));
    }

    public async Task<GetServerWideClientConfigurationResult> GetServerWideClientConfiguration(CancellationToken cancellationToken)
    {
        var configuration = await store.Maintenance.Server.SendAsync(
            new GetServerWideClientConfigurationOperation(),
            cancellationToken);

        return new GetServerWideClientConfigurationResult(ToJson(configuration));
    }

    private static string GetNodeType(string tag, ClusterTopologyResponse topology)
    {
        if (topology.Topology.Members.ContainsKey(tag))
            return "member";

        if (topology.Topology.Promotables.ContainsKey(tag))
            return "promotable";

        if (topology.Topology.Watchers.ContainsKey(tag))
            return "watcher";

        return "unknown";
    }

    private static ServerBuildResult ToServerBuild(BuildNumber build)
    {
        return new ServerBuildResult(
            build.ProductVersion,
            build.BuildVersion,
            build.AssemblyVersion,
            build.CommitHash,
            build.FullVersion);
    }

    private static CurrentNodeResult ToCurrentNode(NodeInfo node)
    {
        return new CurrentNodeResult(
            node.NodeTag,
            node.ServerId,
            node.TopologyId,
            node.ClusterStatus,
            node.CurrentState.ToString(),
            node.ServerRole.ToString(),
            node.ServerSchemaVersion,
            node.HasFixedPort,
            node.NumberOfCores,
            node.InstalledMemoryInGb,
            node.UsableMemoryInGb,
            !string.IsNullOrWhiteSpace(node.Certificate),
            node.OsInfo is null ? null : new OsInfoResult(
                node.OsInfo.Type.ToString(),
                node.OsInfo.FullName,
                node.OsInfo.Version,
                node.OsInfo.BuildVersion,
                node.OsInfo.Is64Bit));
    }

    private static ClusterNodeStatusResult ToClusterNodeStatus(NodeStatus status)
    {
        return new ClusterNodeStatusResult(
            status.Name,
            status.Connected,
            status.LastSent,
            status.LastReply,
            status.LastSentMessage,
            status.LastMatchingIndex,
            status.ErrorDetails);
    }
}
