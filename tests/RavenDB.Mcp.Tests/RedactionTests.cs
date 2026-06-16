using System.Text.Json;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tests;

// Unit tests for the hybrid connection-string redaction (ADR-0011).
public sealed class RedactionTests
{
    private const string Redacted = "***redacted***";

    private static JsonElement Redact(string json)
        => RavenDbAdminClient.RedactSecrets(JsonDocument.Parse(json).RootElement);

    private static string Str(JsonElement root, params string[] path)
    {
        var e = root;
        foreach (var p in path) e = e.GetProperty(p);
        return e.GetString()!;
    }

    [Fact]
    public void TokenizesSqlConnectionString_MasksSecretKeepsRest()
    {
        var r = Redact("""
        { "SqlConnectionStrings": { "rep": { "ConnectionString": "Server=db;User Id=sa;Password=hunter2" } } }
        """);
        var cs = Str(r, "SqlConnectionStrings", "rep", "ConnectionString");
        Assert.Contains("Server=db", cs);
        Assert.Contains("User Id=sa", cs);
        Assert.Contains($"Password={Redacted}", cs);
        Assert.DoesNotContain("hunter2", cs);
    }

    [Fact]
    public void TokenizesAzureStorageAccountKey()
    {
        var r = Redact("""
        { "OlapConnectionStrings": { "olap": { "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=acct;AccountKey=c2VjcmV0Kzc4OQ==;EndpointSuffix=core.windows.net" } } }
        """);
        var cs = Str(r, "OlapConnectionStrings", "olap", "ConnectionString");
        Assert.Contains("AccountName=acct", cs);
        Assert.Contains($"AccountKey={Redacted}", cs);
        Assert.DoesNotContain("c2VjcmV0Kzc4OQ", cs);
    }

    [Fact]
    public void MasksBareSecretFieldInsideContainer()
    {
        var r = Redact("""
        { "ElasticSearchConnectionStrings": { "es": { "Authentication": { "ApiKey": { "ApiKey": "sk-abc123", "ApiKeyId": "id-1" } } } } }
        """);
        Assert.Equal(Redacted, Str(r, "ElasticSearchConnectionStrings", "es", "Authentication", "ApiKey", "ApiKey"));
        Assert.Equal(Redacted, Str(r, "ElasticSearchConnectionStrings", "es", "Authentication", "ApiKey", "ApiKeyId"));
    }

    [Fact]
    public void MasksCredentialsEmbeddedInUrl()
    {
        var r = Redact("""
        { "QueueConnectionStrings": { "q": { "ConnectionString": "amqp://svcuser:s3cretpass@rabbit:5672/vhost" } } }
        """);
        var cs = Str(r, "QueueConnectionStrings", "q", "ConnectionString");
        Assert.Contains("svcuser", cs);
        Assert.Contains($":{Redacted}@", cs);
        Assert.DoesNotContain("s3cretpass", cs);
    }

    [Fact]
    public void GlobalBackstop_MasksSecretInUnknownSection()
    {
        var r = Redact("""{ "SomeFutureSection": { "Password": "leaked", "ApiKey": "sk-x" } }""");
        Assert.Equal(Redacted, Str(r, "SomeFutureSection", "Password"));
        Assert.Equal(Redacted, Str(r, "SomeFutureSection", "ApiKey"));
    }

    [Fact]
    public void DoesNotOverRedact_AmbiguousNameOutsideContainer()
    {
        // "Secret"/"AccessKey" are ambiguous: outside a container they're not in the narrow global set.
        var r = Redact("""{ "CustomConfig": { "Secret": "not-a-credential", "AccessKey": "public-id" } }""");
        Assert.Equal("not-a-credential", Str(r, "CustomConfig", "Secret"));
        Assert.Equal("public-id", Str(r, "CustomConfig", "AccessKey"));
    }

    [Fact]
    public void PreservesNonSecretConnectionString()
    {
        var r = Redact("""
        { "RavenConnectionStrings": { "to-shop": { "Database": "Shop", "TopologyDiscoveryUrls": ["http://localhost:8080"] } } }
        """);
        Assert.Equal("Shop", Str(r, "RavenConnectionStrings", "to-shop", "Database"));
        Assert.Equal("http://localhost:8080",
            r.GetProperty("RavenConnectionStrings").GetProperty("to-shop").GetProperty("TopologyDiscoveryUrls")[0].GetString());
    }
}
