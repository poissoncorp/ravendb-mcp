using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class AiTools
{
    [McpServerTool(Name = "get_ai_agents", ReadOnly = true)]
    [Description("RavenDB AI Agents configured on a database — conversational agents (their model connection, system prompt, tools, parameters). This is the AI Agents feature, NOT GenAI embeddings/connection-string tasks. Omit name to list all agents; pass name for one agent's configuration. Availability-wrapped: reports unavailable when AI Agents isn't enabled or licensed.")]
    public static Task<JsonElement> GetAiAgents(
        RavenDbAdminClient client,
        string databaseName,
        [Description("Agent name — omit to list all agents.")] string? name = null,
        CancellationToken cancellationToken = default)
    {
        return client.GetAiAgents(databaseName, name, cancellationToken);
    }
}
