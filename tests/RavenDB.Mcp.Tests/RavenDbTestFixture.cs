using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations.Indexes;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using RavenDB.Mcp.Configuration;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tests;

public sealed class RavenDbTestFixture : IAsyncLifetime
{
    public string DatabaseName { get; } = $"RavenDB_Mcp_Tests_{Guid.NewGuid():N}";

    public string IndexName { get; } = "TestUsers/ByName";

    public string IndexFieldName { get; } = "Name";

    // Prefer the secured endpoint when CI configured it, so the full suite (including the raw
    // HTTPS diagnostic routes) runs against a certificate-secured server; otherwise unsecured.
    public string Url { get; } =
        Environment.GetEnvironmentVariable("RAVENDB_SECURE_TEST_URL")
        ?? Environment.GetEnvironmentVariable("RAVENDB_TEST_URL")
        ?? throw new InvalidOperationException("RAVENDB_TEST_URL (or RAVENDB_SECURE_TEST_URL) must be set for RavenDB-backed tests.");

    public RavenDbOptions Options { get; } = new()
    {
        Urls = [
            Environment.GetEnvironmentVariable("RAVENDB_SECURE_TEST_URL")
            ?? Environment.GetEnvironmentVariable("RAVENDB_TEST_URL")
            ?? "http://127.0.0.1:8070/"
        ],
        CertificatePath = Environment.GetEnvironmentVariable("RAVENDB_SECURE_TEST_URL") is null
            ? null
            : Environment.GetEnvironmentVariable("RAVENDB_SECURE_OPERATOR_CERTIFICATE_PATH"),
        CertificatePassword = Environment.GetEnvironmentVariable("RAVENDB_SECURE_TEST_URL") is null
            ? null
            : Environment.GetEnvironmentVariable("RAVENDB_SECURE_OPERATOR_CERTIFICATE_PASSWORD")
    };

    public IDocumentStore Store { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Store = DocumentStoreFactory.Create(Options);

        await Store.Maintenance.Server.SendAsync(
            new CreateDatabaseOperation(new DatabaseRecord(DatabaseName)));

        await SeedDatabase();
    }

    public async Task DisposeAsync()
    {
        if (Store is null)
            return;

        try
        {
            await Store.Maintenance.Server.SendAsync(
                new DeleteDatabasesOperation(DatabaseName, hardDelete: true));
        }
        finally
        {
            Store.Dispose();
        }
    }

    private async Task SeedDatabase()
    {
        using (var session = Store.OpenAsyncSession(DatabaseName))
        {
            await session.StoreAsync(new TestUser("Ada"));
            await session.SaveChangesAsync();
        }

        var index = new IndexDefinition { Name = IndexName };
        index.Maps.Add("from user in docs.TestUsers select new { user.Name }");

        await Store.Maintenance.ForDatabase(DatabaseName).SendAsync(new PutIndexesOperation(index));
    }

    private sealed record TestUser(string Name);
}
