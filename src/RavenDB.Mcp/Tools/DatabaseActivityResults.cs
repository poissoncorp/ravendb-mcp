using System.Text.Json;

namespace RavenDB.Mcp.Tools;

public sealed record GetSubscriptionsResult(string DatabaseName, JsonElement Subscriptions);

public sealed record GetSubscriptionStateResult(
    string DatabaseName,
    string SubscriptionName,
    JsonElement State);

public sealed record GetDatabaseTcpInfoResult(string DatabaseName, string NodeTag, JsonElement Tcp);

public sealed record GetIdentitiesResult(string DatabaseName, JsonElement Identities);
