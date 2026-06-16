using System.Text.Json;

namespace RavenDB.Mcp.Tests;

public sealed class McpToolContractTests
{
    [Fact]
    public async Task ExposesExpectedReadOnlyToolsWithoutConnectingToRavenDb()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var client = McpStdioClient.Start("http://127.0.0.1:1/");

        await client.Initialize(timeout.Token);

        using var tools = await client.Request("tools/list", null, timeout.Token);
        var toolsArray = tools.RootElement
            .GetProperty("result")
            .GetProperty("tools")
            .EnumerateArray()
            .ToArray();
        var toolNames = toolsArray
            .Select(tool => tool.GetProperty("name").GetString())
            .ToArray();

        Assert.Equal(ToolCatalog.ExpectedToolNames.Order(), toolNames.Order());
        Assert.All(toolNames, name => Assert.Matches("^[a-z][a-z0-9]*(?:_[a-z0-9]+)*$", name!));
        Assert.All(toolsArray, tool => Assert.True(
            tool.GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean()));
    }

    // Missing-required-argument guards must reach the agent with the SPECIFIC message (thrown as
    // McpException), not the SDK's generic wrapper — else the agent can't self-correct. Runs against a
    // dead URL since each guard fires before any RavenDB call.
    [Theory]
    [InlineData("get_index", /*args*/ "{\"databaseName\":\"x\",\"indexName\":\"i\",\"include\":[\"Terms\"]}", "fieldName is required")]
    [InlineData("get_network_details", "{\"databaseName\":\"x\",\"include\":[\"DatabaseInfo\"]}", "nodeTag is required")]
    [InlineData("wait_for_completion", "{\"databaseName\":\"x\",\"condition\":\"Operation\",\"timeoutSeconds\":5}", "operationId is required")]
    [InlineData("collect_debug_package", "{\"scope\":\"Database\"}", "databaseName is required")]
    [InlineData("get_tasks", "{\"databaseName\":\"x\",\"taskId\":5}", "taskType is required")]
    [InlineData("get_document_data", "{\"databaseName\":\"x\",\"id\":\"y\",\"include\":[\"TimeSeries\"]}", "timeSeriesName is required")]
    [InlineData("get_live_workload", "{\"include\":[\"Queries\"]}", "databaseName is required")]
    [InlineData("get_live_workload", "{\"include\":[\"Operations\"],\"operationId\":5}", "databaseName is required")]
    [InlineData("get_tasks", "{\"databaseName\":\"x\",\"includeDiagnostics\":true}", "taskType is required when includeDiagnostics is set")]
    [InlineData("get_tasks", "{\"databaseName\":\"x\",\"taskType\":\"QueueSink\",\"includeDiagnostics\":true}", "No deep diagnostics available for task type")]
    [InlineData("get_network_details", "{\"include\":[\"DatabaseInfo\"]}", "databaseName is required")]
    public async Task MissingRequiredArgumentSurfacesActionableMessage(string tool, string argsJson, string expectedMessage)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var client = McpStdioClient.Start("http://127.0.0.1:1/");
        await client.Initialize(timeout.Token);

        var args = JsonSerializer.Deserialize<JsonElement>(argsJson);
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.CallTool(tool, args, timeout.Token));

        // CallTool surfaces an isError result's text verbatim; the actionable guard message must be there.
        Assert.Contains(expectedMessage, failure.Message);
    }
}
