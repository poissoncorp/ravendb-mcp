using System.Text.Json;
using Microsoft.Extensions.Options;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tests;

public sealed class SecuredRavenDbConnectionTests
{
    [SecuredRavenDbFact]
    public async Task RavenDbClientConnectsWithOperatorPfx()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await SecuredRavenDbTestSettings.EnsureOperatorCertificateRegisteredAsync(timeout.Token);

        var options = SecuredRavenDbTestSettings.OperatorOptions;
        using var store = DocumentStoreFactory.Create(options);
        var client = new RavenDbAdminClient(store, Options.Create(options));

        var serverInfo = await client.GetServerInfo(timeout.Token);

        Assert.NotEmpty(serverInfo.ProductVersion);
    }

    [SecuredRavenDbFact]
    public async Task McpServerConnectsWithOperatorPfxOverStdio()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await SecuredRavenDbTestSettings.EnsureOperatorCertificateRegisteredAsync(timeout.Token);

        await using var client = McpStdioClient.Start(SecuredRavenDbTestSettings.OperatorOptions);

        await client.Initialize(timeout.Token);

        using var databases = await client.CallTool("list_databases", null, timeout.Token);

        Assert.Equal(JsonValueKind.Array, databases.RootElement.GetProperty("databases").ValueKind);
    }
}
