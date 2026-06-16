using System.Text.Json;
using Raven.Client.Documents.Operations.AI.Agents;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    public Task<JsonElement> GetAiAgents(string databaseName, string? name, CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);
        var operation = string.IsNullOrWhiteSpace(name)
            ? new GetAiAgentsOperation()
            : new GetAiAgentsOperation(name);
        return TryReadJson(() => ForDatabase(databaseName).SendAsync(operation, cancellationToken), cancellationToken);
    }
}
