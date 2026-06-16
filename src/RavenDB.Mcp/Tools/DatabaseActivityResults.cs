using System.Text.Json;

namespace RavenDB.Mcp.Tools;

// Database-activity result records. These reads are surfaced through facet tools:
// subscriptions via get_tasks, TCP endpoint via get_network_details, identities via get_database_stats.

public sealed record GetSubscriptionsResult(string DatabaseName, JsonElement Subscriptions);

public sealed record GetSubscriptionStateResult(
    string DatabaseName,
    string SubscriptionName,
    JsonElement State);

public sealed record GetDatabaseTcpInfoResult(string DatabaseName, string NodeTag, JsonElement Tcp);

public sealed record GetIdentitiesResult(string DatabaseName, JsonElement Identities);
