using System.Diagnostics;
using System.Text.Json;
using RavenDB.Mcp.Configuration;

namespace RavenDB.Mcp.Tests;

public sealed class McpServerE2ETests(RavenDbTestFixture fixture)
    : IClassFixture<RavenDbTestFixture>
{
    [Fact]
    public async Task ExposesAndCallsV1ToolsOverStdio()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
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

        using var clusterNodes = await client.CallTool("get_cluster_nodes", null, timeout.Token);
        Assert.Equal("7.2", clusterNodes.RootElement.GetProperty("server").GetProperty("productVersion").GetString());
        Assert.NotEmpty(clusterNodes.RootElement.GetProperty("cluster").GetProperty("nodes").EnumerateArray());

        using var logsConfiguration = await client.CallTool("get_logs_configuration", null, timeout.Token);
        Assert.Equal(JsonValueKind.Object, logsConfiguration.RootElement.GetProperty("configuration").ValueKind);

        using var serverDiagnostics = await client.CallTool("get_server_diagnostics_overview", null, timeout.Token);
        Assert.Equal(JsonValueKind.Object, serverDiagnostics.RootElement.GetProperty("metrics").ValueKind);
        AssertAvailable(serverDiagnostics, "routes");
        AssertAvailable(serverDiagnostics, "configuration");
        AssertAvailable(serverDiagnostics, "metrics");
        AssertAvailable(serverDiagnostics, "cpuCredits");
        AssertAvailable(serverDiagnostics, "idleDatabases");
        AssertAvailable(serverDiagnostics, "clusterMaintenance");

        using var clusterDiagnostics = await client.CallTool("get_cluster_diagnostics_overview", null, timeout.Token);
        Assert.Equal(JsonValueKind.Object, clusterDiagnostics.RootElement.GetProperty("observerDecisions").ValueKind);
        AssertAvailable(clusterDiagnostics, "observerDecisions");
        AssertAvailable(clusterDiagnostics, "clusterLog");
        AssertAvailable(clusterDiagnostics, "history");
        AssertAvailable(clusterDiagnostics, "remoteConnections");
        AssertAvailable(clusterDiagnostics, "engineLogs");
        AssertAvailable(clusterDiagnostics, "stateChanges");

        using var serverWideClientConfiguration = await client.CallTool("get_server_wide_client_configuration", null, timeout.Token);
        Assert.True(serverWideClientConfiguration.RootElement.GetProperty("configuration").ValueKind is JsonValueKind.Object or JsonValueKind.Null);

        using var databases = await client.CallTool("list_databases", null, timeout.Token);
        Assert.Contains(
            fixture.DatabaseName,
            databases.RootElement.GetProperty("databases").EnumerateArray().Select(database => database.GetString()));

        using var databaseRecord = await client.CallTool(
            "get_database_record",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);

        Assert.Equal(
            fixture.DatabaseName,
            databaseRecord.RootElement.GetProperty("databaseName").GetString());

        var nodeTag = clusterNodes.RootElement
            .GetProperty("cluster")
            .GetProperty("respondingNodeTag")
            .GetString()!;

        using var databaseOverview = await client.CallTool(
            "get_database_overview",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(fixture.DatabaseName, databaseOverview.RootElement.GetProperty("databaseName").GetString());
        Assert.Equal(JsonValueKind.Object, databaseOverview.RootElement.GetProperty("stats").ValueKind);
        Assert.Equal(JsonValueKind.Object, databaseOverview.RootElement.GetProperty("detailedStats").ValueKind);

        using var collectionOverview = await client.CallTool(
            "get_collection_overview",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, collectionOverview.RootElement.GetProperty("stats").ValueKind);
        Assert.Equal(JsonValueKind.Object, collectionOverview.RootElement.GetProperty("detailedStats").ValueKind);

        using var databaseConfiguration = await client.CallTool(
            "get_database_configuration",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, databaseConfiguration.RootElement.GetProperty("configuration").ValueKind);

        using var clientConfiguration = await client.CallTool(
            "get_client_configuration",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, clientConfiguration.RootElement.GetProperty("configuration").ValueKind);

        using var indexingOverview = await client.CallTool(
            "get_indexing_overview",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Array, indexingOverview.RootElement.GetProperty("indexes").ValueKind);
        Assert.Equal(JsonValueKind.Array, indexingOverview.RootElement.GetProperty("stats").ValueKind);
        Assert.Equal(JsonValueKind.Array, indexingOverview.RootElement.GetProperty("errors").ValueKind);
        Assert.Equal(JsonValueKind.Array, indexingOverview.RootElement.GetProperty("performance").ValueKind);
        Assert.Equal(JsonValueKind.Object, indexingOverview.RootElement.GetProperty("status").ValueKind);
        Assert.Equal(JsonValueKind.Object, indexingOverview.RootElement.GetProperty("progress").ValueKind);
        AssertAvailable(indexingOverview, "progress");
        AssertAvailabilityMetadata(indexingOverview, "suggestedMerges");
        AssertAvailable(indexingOverview, "totalTime");

        using var index = await client.CallTool(
            "get_index",
            new { databaseName = fixture.DatabaseName, indexName = fixture.IndexName },
            timeout.Token);
        Assert.Equal(fixture.IndexName, index.RootElement.GetProperty("indexName").GetString());

        using var indexTerms = await client.CallTool(
            "get_index_terms",
            new
            {
                databaseName = fixture.DatabaseName,
                indexName = fixture.IndexName,
                fieldName = fixture.IndexFieldName,
                fromValue = (string?)null,
                pageSize = 16
            },
            timeout.Token);
        Assert.Equal(JsonValueKind.Array, indexTerms.RootElement.GetProperty("terms").ValueKind);

        using var replication = await client.CallTool(
            "get_replication_tasks_details",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, replication.RootElement.GetProperty("tasks").ValueKind);
        Assert.Equal(JsonValueKind.Object, replication.RootElement.GetProperty("performance").ValueKind);

        using var queryDiagnostics = await client.CallTool(
            "get_query_diagnostics",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, queryDiagnostics.RootElement.GetProperty("runningQueries").ValueKind);
        AssertAvailable(queryDiagnostics, "runningQueries");
        AssertAvailable(queryDiagnostics, "queryCache");

        using var backupDiagnostics = await client.CallTool(
            "get_backup_diagnostics",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, backupDiagnostics.RootElement.GetProperty("tasks").ValueKind);

        using var backupTasks = await client.CallTool(
            "get_backup_tasks",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, backupTasks.RootElement.GetProperty("tasks").ValueKind);

        using var etlTasks = await client.CallTool(
            "get_etl_tasks",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, etlTasks.RootElement.GetProperty("tasks").ValueKind);

        using var etlDiagnostics = await client.CallTool(
            "get_etl_diagnostics",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, etlDiagnostics.RootElement.GetProperty("tasks").ValueKind);

        using var ongoingTasks = await client.CallTool(
            "get_database_tasks",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, ongoingTasks.RootElement.GetProperty("tasks").ValueKind);

        using var subscriptions = await client.CallTool(
            "get_subscriptions",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Array, subscriptions.RootElement.GetProperty("subscriptions").ValueKind);

        using var subscriptionDiagnostics = await client.CallTool(
            "get_subscription_diagnostics",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Array, subscriptionDiagnostics.RootElement.GetProperty("subscriptions").ValueKind);

        using var tcp = await client.CallTool(
            "get_database_tcp_info",
            new { databaseName = fixture.DatabaseName, nodeTag },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, tcp.RootElement.GetProperty("tcp").ValueKind);

        using var identities = await client.CallTool(
            "get_identities",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, identities.RootElement.GetProperty("identities").ValueKind);

        using var storageOverview = await client.CallTool(
            "get_storage_overview",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, storageOverview.RootElement.GetProperty("report").ValueKind);

        using var storageTrees = await client.CallTool(
            "get_storage_trees",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, storageTrees.RootElement.GetProperty("trees").ValueKind);

        using var storageTreeStructure = await client.CallTool(
            "get_storage_tree_structure",
            new
            {
                databaseName = fixture.DatabaseName,
                treeName = "Docs",
                treeKind = (string?)null
            },
            timeout.Token);
        Assert.NotEmpty(storageTreeStructure.RootElement.GetProperty("structure").GetString()!);

        using var serverResources = await client.CallTool("get_server_resources", null, timeout.Token);
        Assert.Equal(JsonValueKind.Object, serverResources.RootElement.GetProperty("metrics").ValueKind);
        Assert.Equal(JsonValueKind.Object, serverResources.RootElement.GetProperty("cpu").ValueKind);
        Assert.Equal(JsonValueKind.Object, serverResources.RootElement.GetProperty("process").ValueKind);
        Assert.Equal(JsonValueKind.Array, serverResources.RootElement.GetProperty("threads").ValueKind);

        using var tcpStats = await client.CallTool("get_tcp_stats", null, timeout.Token);
        Assert.Equal(JsonValueKind.Object, tcpStats.RootElement.GetProperty("tcp").ValueKind);
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

    private static void AssertAvailable(JsonDocument document, string propertyName)
    {
        var section = document.RootElement.GetProperty(propertyName);
        Assert.True(section.GetProperty("available").GetBoolean(), $"{propertyName}: {section}");
    }

    private static void AssertAvailabilityMetadata(JsonDocument document, string propertyName)
    {
        var section = document.RootElement.GetProperty(propertyName);
        Assert.True(section.GetProperty("available").ValueKind is JsonValueKind.True or JsonValueKind.False);
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

    private static McpStdioClient StartProcess(ProcessStartInfo startInfo)
    {
        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start RavenDB.Mcp process.");

        process.ErrorDataReceived += (_, _) => { };
        process.BeginErrorReadLine();

        return new McpStdioClient(process);
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

        if (!result.TryGetProperty("structuredContent", out var structuredContent))
            throw new InvalidOperationException(response.RootElement.GetRawText());

        return JsonDocument.Parse(structuredContent.GetRawText());
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
