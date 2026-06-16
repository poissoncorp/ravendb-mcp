using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class AiTools
{
    [McpServerTool(Name = "get_ai_agents", ReadOnly = true)]
    [Description("AI agents configured on a database (RavenDB Gen-AI). Omit name to list all agents; pass name for one agent's definition. Availability-wrapped: reports unavailable when the AI feature isn't enabled or licensed.")]
    public static Task<JsonElement> GetAiAgents(
        RavenDbAdminClient client,
        string databaseName,
        [Description("Agent name — omit to list all agents.")] string? name,
        CancellationToken cancellationToken)
    {
        return client.GetAiAgents(databaseName, name, cancellationToken);
    }
}
