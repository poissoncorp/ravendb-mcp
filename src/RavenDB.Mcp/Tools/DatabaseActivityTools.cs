using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class DatabaseActivityTools
{
    [McpServerTool(Name = "get_subscriptions", ReadOnly = true, UseStructuredContent = true)]
    [Description("All data subscriptions defined on a database: name, query, and configuration per subscription.")]
    public static Task<GetSubscriptionsResult> GetSubscriptions(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetSubscriptions(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_subscription_state", ReadOnly = true, UseStructuredContent = true)]
    [Description("Runtime state of one subscription: change vector / processing position and last-batch info.")]
    public static Task<GetSubscriptionStateResult> GetSubscriptionState(
        RavenDbAdminClient client,
        string databaseName,
        string subscriptionName,
        CancellationToken cancellationToken)
    {
        return client.GetSubscriptionState(databaseName, subscriptionName, cancellationToken);
    }

    [McpServerTool(Name = "get_database_tcp_info", ReadOnly = true, UseStructuredContent = true)]
    [Description("TCP connection info a client/node would use to reach this database on a given node tag (URLs, port, certificate requirement).")]
    public static Task<GetDatabaseTcpInfoResult> GetDatabaseTcpInfo(
        RavenDbAdminClient client,
        string databaseName,
        string nodeTag,
        CancellationToken cancellationToken)
    {
        return client.GetDatabaseTcpInfo(databaseName, nodeTag, cancellationToken);
    }

    [McpServerTool(Name = "get_identities", ReadOnly = true, UseStructuredContent = true)]
    [Description("Identity counters for a database (the server-side sequence values behind identity document ids).")]
    public static Task<GetIdentitiesResult> GetIdentities(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetIdentities(databaseName, cancellationToken);
    }
}

public sealed record GetSubscriptionsResult(string DatabaseName, JsonElement Subscriptions);

public sealed record GetSubscriptionStateResult(
    string DatabaseName,
    string SubscriptionName,
    JsonElement State);

public sealed record GetDatabaseTcpInfoResult(string DatabaseName, string NodeTag, JsonElement Tcp);

public sealed record GetIdentitiesResult(string DatabaseName, JsonElement Identities);
