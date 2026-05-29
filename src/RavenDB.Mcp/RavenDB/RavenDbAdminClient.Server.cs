using Raven.Client.ServerWide.Commands;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Raven.Client.ServerWide.Operations.Configuration;
using Raven.Client.ServerWide.Operations.Logs;
using Raven.Client.Http;
using RavenDB.Mcp.Configuration;
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

    public async Task<GetClusterTopologyResult> GetClusterTopology(CancellationToken cancellationToken)
    {
        var topology = await ExecuteServerCommand(new GetClusterTopologyCommand(), cancellationToken);
        return new GetClusterTopologyResult(ToJson(topology));
    }

    public async Task<GetClusterNodesResult> GetClusterNodes(CancellationToken cancellationToken)
    {
        var server = await store.Maintenance.Server.SendAsync(
            new GetBuildNumberOperation(),
            cancellationToken);
        var currentNode = await ExecuteServerCommand(new GetNodeInfoCommand(), cancellationToken);
        var topology = await ExecuteServerCommand(new GetClusterTopologyCommand(), cancellationToken);

        var nodes = new List<ClusterNodeResult>();

        foreach (var (tag, url) in topology.Topology.AllNodes.OrderBy(node => node.Key, StringComparer.OrdinalIgnoreCase))
        {
            NodeStatus? status = null;
            topology.Status?.TryGetValue(tag, out status);
            nodes.Add(await GetClusterNode(tag, GetNodeType(tag, topology), url, status, cancellationToken));
        }

        return new GetClusterNodesResult(
            ToServerBuild(server),
            ToCurrentNode(currentNode),
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

    private async Task<ClusterNodeResult> GetClusterNode(
        string tag,
        string type,
        string url,
        NodeStatus? status,
        CancellationToken cancellationToken)
    {
        try
        {
            using var nodeStore = DocumentStoreFactory.Create(NodeOptions(url));
            var build = await nodeStore.Maintenance.Server.SendAsync(new GetBuildNumberOperation(), cancellationToken);
            var self = await ExecuteServerCommand(nodeStore, new GetNodeInfoCommand(), cancellationToken);

            return new ClusterNodeResult(
                tag,
                type,
                url,
                status is null ? null : ToClusterNodeStatus(status),
                ToServerBuild(build),
                ToCurrentNode(self),
                null);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new ClusterNodeResult(
                tag,
                type,
                url,
                status is null ? null : ToClusterNodeStatus(status),
                null,
                null,
                exception.Message);
        }
    }

    private RavenDbOptions NodeOptions(string url)
    {
        return configuredOptions is null
            ? new RavenDbOptions { Urls = [url] }
            : configuredOptions with { Urls = [url] };
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
