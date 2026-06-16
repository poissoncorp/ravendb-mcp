using System.Text.Json;
using Raven.Client.Documents.Operations.AI.Agents;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    // RavenDB AI Agents (conversational agents defined per database) — a distinct feature from
    // GenAI embeddings/connection-string tasks. Read via the typed Client-API operation; no agent
    // id returns all agents, an id returns that one. Availability-wrapped: the feature is
    // licensed/optional, so a database without it reports { available: false } rather than throwing.
    public Task<JsonElement> GetAiAgents(string databaseName, string? name, CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);
        var operation = string.IsNullOrWhiteSpace(name)
            ? new GetAiAgentsOperation()
            : new GetAiAgentsOperation(name);
        return TryReadJson(() => ForDatabase(databaseName).SendAsync(operation, cancellationToken), cancellationToken);
    }
}
