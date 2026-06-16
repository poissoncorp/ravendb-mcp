using System.Diagnostics;
using System.Text.Json;
using RavenDB.Mcp.Configuration;

namespace RavenDB.Mcp.Tests;

public sealed class McpServerE2ETests(RavenDbTestFixture fixture)
    : IClassFixture<RavenDbTestFixture>
{
    [Fact]
    public async Task ExposesAndCallsFacetToolsOverStdio()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        await using var client = McpStdioClient.Start(fixture.Options);

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

        // The hybrid output contract (ADR-0010): facet tools carry no outputSchema so strict
        // clients accept the list. Guard against a regression that reintroduces `true` schemas.
        Assert.All(toolsArray, tool => Assert.False(
            tool.TryGetProperty("outputSchema", out var schema) && schema.ValueKind == JsonValueKind.True));

        // get_cluster_overview — all four sections; also our source for productVersion + nodeTag.
        using var cluster = await client.CallTool(
            "get_cluster_overview",
            new { include = new[] { "Nodes", "ServerInfo", "ServerDiagnostics", "ClusterDiagnostics" } },
            timeout.Token);
        AssertSections(cluster, "nodes", "serverInfo", "serverDiagnostics", "clusterDiagnostics");
        Assert.Equal("7.2", cluster.RootElement.GetProperty("nodes").GetProperty("server").GetProperty("productVersion").GetString());
        var nodeTag = cluster.RootElement.GetProperty("nodes").GetProperty("cluster").GetProperty("respondingNodeTag").GetString()!;

        using var notifications = await client.CallTool("get_notifications", new { databaseName = (string?)null }, timeout.Token);
        Assert.True(notifications.RootElement.TryGetProperty("notifications", out _));

        using var serverConfig = await client.CallTool(
            "get_server_config",
            new { include = new[] { "Logs", "ClientConfig", "TrafficWatch", "Studio" } },
            timeout.Token);
        AssertSections(serverConfig, "logs", "clientConfig", "trafficWatch", "studio");

        using var serverResources = await client.CallTool(
            "get_server_resources",
            new { include = new[] { "Metrics", "Cpu", "Process" } },
            timeout.Token);
        AssertSections(serverResources, "metrics", "cpu", "process");

        using var network = await client.CallTool(
            "get_network_details",
            new { include = new[] { "Stats", "Connections", "DatabaseInfo" }, databaseName = fixture.DatabaseName, nodeTag },
            timeout.Token);
        AssertSections(network, "stats", "connections", "databaseInfo");

        using var databases = await client.CallTool("list_databases", null, timeout.Token);
        Assert.Contains(
            fixture.DatabaseName,
            databases.RootElement.GetProperty("databases").EnumerateArray().Select(database => database.GetString()));

        using var databaseRecord = await client.CallTool(
            "get_database_record",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(fixture.DatabaseName, databaseRecord.RootElement.GetProperty("databaseName").GetString());

        using var stats = await client.CallTool(
            "get_database_stats",
            new { databaseName = fixture.DatabaseName, include = new[] { "Summary", "Detailed", "Collections", "Indexing", "Identities", "Storage" } },
            timeout.Token);
        AssertSections(stats, "summary", "detailed", "collections", "indexing", "identities", "storage");

        using var databaseConfig = await client.CallTool(
            "get_database_config",
            new { databaseName = fixture.DatabaseName, include = new[] { "Settings", "ClientConfig" } },
            timeout.Token);
        AssertSections(databaseConfig, "settings", "clientConfig");

        using var index = await client.CallTool(
            "get_index",
            new
            {
                databaseName = fixture.DatabaseName,
                indexName = fixture.IndexName,
                include = new[] { "Definition", "Staleness", "Terms" },
                fieldName = fixture.IndexFieldName,
                pageSize = 16
            },
            timeout.Token);
        AssertSections(index, "definition", "staleness", "terms");
        Assert.Equal(fixture.IndexName, index.RootElement.GetProperty("definition").GetProperty("indexName").GetString());

        using var tasks = await client.CallTool("get_tasks", new { databaseName = fixture.DatabaseName }, timeout.Token);
        Assert.Equal(JsonValueKind.Object, tasks.RootElement.GetProperty("tasks").ValueKind);
        AssertNoSections(tasks, "info", "subscriptionState", "diagnostics"); // default mode emits only `tasks`

        using var taskDiagnostics = await client.CallTool(
            "get_tasks",
            new { databaseName = fixture.DatabaseName, taskType = "Replication", includeDiagnostics = true },
            timeout.Token);
        Assert.True(taskDiagnostics.RootElement.TryGetProperty("diagnostics", out _));

        using var workload = await client.CallTool(
            "get_live_workload",
            new { databaseName = fixture.DatabaseName, include = new[] { "Operations", "Queries", "Transactions" } },
            timeout.Token);
        AssertSections(workload, "operations", "queries", "transactions");

        using var storage = await client.CallTool(
            "inspect_storage",
            new { databaseName = fixture.DatabaseName, include = new[] { "Trees", "Environment" } },
            timeout.Token);
        AssertSections(storage, "trees", "environment");

        // run_query gives us a live document id to drive get_document_data.
        using var query = await client.CallTool(
            "run_query",
            new { databaseName = fixture.DatabaseName, query = "from TestUsers" },
            timeout.Token);
        var rows = query.RootElement.GetProperty("result").GetProperty("Results");
        Assert.NotEmpty(rows.EnumerateArray());
        var documentId = rows[0].GetProperty("@metadata").GetProperty("@id").GetString()!;

        using var document = await client.CallTool(
            "get_document_data",
            new { databaseName = fixture.DatabaseName, id = documentId, include = new[] { "Document", "Counters" } },
            timeout.Token);
        AssertSections(document, "document", "counters");
        Assert.True(document.RootElement.GetProperty("document").GetProperty("found").GetBoolean());

        using var compareExchange = await client.CallTool(
            "list_compare_exchange",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, compareExchange.RootElement.ValueKind);

        // AI agents are availability-wrapped; on a non-AI/unlicensed server this reports unavailable.
        using var aiAgents = await client.CallTool("get_ai_agents", new { databaseName = fixture.DatabaseName }, timeout.Token);
        Assert.True(aiAgents.RootElement.TryGetProperty("available", out _));

        using var feed = await client.CallTool("sample_live_feed", new { feed = "GcEvents", seconds = 1 }, timeout.Token);
        Assert.Equal("gc_events", feed.RootElement.GetProperty("kind").GetString());
        Assert.Equal(131_072, feed.RootElement.GetProperty("limit").GetInt32());

        using var wait = await client.CallTool(
            "wait_for_completion",
            new { databaseName = fixture.DatabaseName, condition = "Indexing", timeoutSeconds = 30 },
            timeout.Token);
        Assert.True(wait.RootElement.GetProperty("completed").GetBoolean());

        using var exportedLogs = await client.CallTool(
            "export_server_logs",
            new { from = (string?)null, to = (string?)null },
            timeout.Token);
        Assert.NotEmpty(exportedLogs.RootElement.GetProperty("path").GetString()!);

        // Dispatch correctness: default resolution + single-section, asserting ABSENCE of unselected sections.
        using var indexDefault = await client.CallTool(
            "get_index",
            new { databaseName = fixture.DatabaseName, indexName = fixture.IndexName },
            timeout.Token);
        AssertSections(indexDefault, "definition", "staleness"); // the default include set
        AssertNoSections(indexDefault, "terms", "debug", "errors", "performance");

        using var statsOne = await client.CallTool(
            "get_database_stats",
            new { databaseName = fixture.DatabaseName, include = new[] { "Tombstones" } },
            timeout.Token);
        AssertSections(statsOne, "tombstones");
        AssertNoSections(statsOne, "summary", "collections", "indexing");

        // The attachments projection branch runs (the seeded doc has none, so null is acceptable).
        using var attachments = await client.CallTool(
            "get_document_data",
            new { databaseName = fixture.DatabaseName, id = documentId, include = new[] { "Attachments" } },
            timeout.Token);
        Assert.True(attachments.RootElement.TryGetProperty("attachments", out _));
    }

    private static void AssertSections(JsonDocument document, params string[] sections)
    {
        foreach (var section in sections)
            Assert.True(document.RootElement.TryGetProperty(section, out _), $"missing facet section '{section}'");
    }

    private static void AssertNoSections(JsonDocument document, params string[] sections)
    {
        foreach (var section in sections)
            Assert.False(document.RootElement.TryGetProperty(section, out _), $"unexpected facet section '{section}'");
    }

    [Fact]
    public async Task LoadsRavenDbUrlFromConfigFile()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var configPath = Path.Combine(Path.GetTempPath(), $"ravendb-mcp-{Guid.NewGuid():N}.json");

        try
        {
            await File.WriteAllTextAsync(
                configPath,
                JsonSerializer.Serialize(fixture.Options),
                timeout.Token);

            await using var client = McpStdioClient.StartWithConfigFile(configPath);
            await client.Initialize(timeout.Token);

            using var databases = await client.CallTool("list_databases", null, timeout.Token);

            Assert.Contains(
                fixture.DatabaseName,
                databases.RootElement.GetProperty("databases").EnumerateArray().Select(database => database.GetString()));
        }
        finally
        {
            File.Delete(configPath);
        }
    }
}

internal sealed class McpStdioClient : IAsyncDisposable
{
    private readonly Process _process;
    private int _nextId;

    private McpStdioClient(Process process)
    {
        _process = process;
    }

    public static McpStdioClient Start(string ravenDbUrl)
    {
        return Start(new RavenDbOptions { Urls = [ravenDbUrl] });
    }

    public static McpStdioClient StartWithConfigFile(string configPath)
    {
        var startInfo = CreateStartInfo();
        startInfo.ArgumentList.Add("--config");
        startInfo.ArgumentList.Add(configPath);

        return StartProcess(startInfo);
    }

    public static McpStdioClient Start(RavenDbOptions options)
    {
        var startInfo = CreateStartInfo();
        startInfo.Environment["Urls__0"] = options.Urls[0];

        if (!string.IsNullOrWhiteSpace(options.CertificatePath))
            startInfo.Environment["CertificatePath"] = options.CertificatePath;

        if (options.CertificatePassword is not null)
            startInfo.Environment["CertificatePassword"] = options.CertificatePassword;

        if (options.ArtifactsPath is not null)
            startInfo.Environment["ArtifactsPath"] = options.ArtifactsPath;

        return StartProcess(startInfo);
    }

    private static ProcessStartInfo CreateStartInfo()
    {
        var serverAssembly = typeof(global::RavenDB.Mcp.RavenDB.RavenDbAdminClient).Assembly.Location;

        return new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { serverAssembly },
            WorkingDirectory = Path.GetDirectoryName(serverAssembly)!,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
    }

    public readonly System.Text.StringBuilder Stderr = new();

    private static McpStdioClient StartProcess(ProcessStartInfo startInfo)
    {
        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start RavenDB.Mcp process.");

        var client = new McpStdioClient(process);
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (client.Stderr) client.Stderr.AppendLine(e.Data); };
        process.BeginErrorReadLine();

        return client;
    }

    public async Task Initialize(CancellationToken cancellationToken)
    {
        using var response = await Request("initialize", new
        {
            protocolVersion = "2025-06-18",
            capabilities = new { },
            clientInfo = new
            {
                name = "ravendb-mcp-tests",
                version = "0.1.0"
            }
        }, cancellationToken);

        await Send(new
        {
            jsonrpc = "2.0",
            method = "notifications/initialized",
            @params = new { }
        }, cancellationToken);
    }

    public async Task<JsonDocument> CallTool(string name, object? arguments, CancellationToken cancellationToken)
    {
        using var response = await Request("tools/call", new
        {
            name,
            arguments = arguments ?? new { }
        }, cancellationToken);

        if (response.RootElement.TryGetProperty("error", out var error))
            throw new InvalidOperationException(error.ToString());

        var result = response.RootElement.GetProperty("result");

        var firstText = result.TryGetProperty("content", out var content)
            && content.ValueKind == JsonValueKind.Array
            && content.GetArrayLength() > 0
            && content[0].TryGetProperty("text", out var textElement)
                ? textElement.GetString()
                : null;

        // A tool that throws server-side comes back as an isError result with the message as text.
        if (result.TryGetProperty("isError", out var isError) && isError.GetBoolean())
        {
            string stderr; lock (Stderr) stderr = Stderr.ToString();
            throw new InvalidOperationException($"Tool '{name}' failed: {firstText}\nSTDERR:\n{stderr}");
        }

        // Typed-result tools return structuredContent; opaque-JsonElement/Dictionary tools return the
        // JSON payload as a text content block (UseStructuredContent is hybrid — see ADR-0010).
        if (result.TryGetProperty("structuredContent", out var structuredContent))
            return JsonDocument.Parse(structuredContent.GetRawText());

        if (firstText is not null)
            return JsonDocument.Parse(firstText);

        throw new InvalidOperationException(response.RootElement.GetRawText());
    }

    public async Task<JsonDocument> Request(string method, object? parameters, CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref _nextId);
        await Send(new
        {
            jsonrpc = "2.0",
            id,
            method,
            @params = parameters ?? new { }
        }, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await _process.StandardOutput.ReadLineAsync(cancellationToken);
            if (line is null)
                throw new InvalidOperationException("MCP server closed stdout.");

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var document = JsonDocument.Parse(line);
            if (document.RootElement.TryGetProperty("id", out var responseId) && responseId.GetInt32() == id)
                return document;

            document.Dispose();
        }

        throw new OperationCanceledException(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            await _process.StandardInput.DisposeAsync();
            _process.Kill(entireProcessTree: true);
        }

        _process.Dispose();
    }

    private async Task Send(object message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message);
        await _process.StandardInput.WriteLineAsync(json.AsMemory(), cancellationToken);
        await _process.StandardInput.FlushAsync(cancellationToken);
    }
}
