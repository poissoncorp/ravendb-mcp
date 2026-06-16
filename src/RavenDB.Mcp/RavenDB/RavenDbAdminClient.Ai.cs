using System.Text.Json;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    // RavenDB Gen-AI agents. No typed Client-API operation in 7.2; raw admin route.
    // Availability-wrapped: the feature is licensed/optional, so an unconfigured database
    // reports { available: false } rather than throwing.
    public Task<JsonElement> GetAiAgents(string databaseName, string? name, CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);
        return TryGetDatabaseJson(databaseName, "/admin/ai/agent", cancellationToken, ("name", name));
    }
}
