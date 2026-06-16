using System.Security.Cryptography.X509Certificates;
using Raven.Client.ServerWide.Operations.Certificates;
using RavenDB.Mcp.Configuration;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tests;

internal static class SecuredRavenDbTestSettings
{
    public const string UrlVariable = "RAVENDB_SECURE_TEST_URL";
    public const string AdminCertificatePathVariable = "RAVENDB_SECURE_ADMIN_CERTIFICATE_PATH";
    public const string AdminCertificatePasswordVariable = "RAVENDB_SECURE_ADMIN_CERTIFICATE_PASSWORD";
    public const string OperatorCertificatePathVariable = "RAVENDB_SECURE_OPERATOR_CERTIFICATE_PATH";
    public const string OperatorCertificatePasswordVariable = "RAVENDB_SECURE_OPERATOR_CERTIFICATE_PASSWORD";

    public static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(UrlVariable)) &&
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AdminCertificatePathVariable)) &&
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(AdminCertificatePasswordVariable)) &&
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(OperatorCertificatePathVariable)) &&
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(OperatorCertificatePasswordVariable));

    public static RavenDbOptions AdminOptions => new()
    {
        Urls = [GetRequired(UrlVariable)],
        CertificatePath = GetRequired(AdminCertificatePathVariable),
        CertificatePassword = GetRequired(AdminCertificatePasswordVariable)
    };

    public static RavenDbOptions OperatorOptions => new()
    {
        Urls = [GetRequired(UrlVariable)],
        CertificatePath = GetRequired(OperatorCertificatePathVariable),
        CertificatePassword = GetRequired(OperatorCertificatePasswordVariable)
    };

    public static string MissingMessage =>
        $"Set {UrlVariable}, admin certificate variables, and operator certificate variables to run secured RavenDB tests.";

    // The operator certificate is NOT a well-known admin — it must be registered with the server
    // (Operator clearance) using the admin certificate before any client can authenticate with it.
    // Every secured test that connects as the operator (the shared fixture and the connection tests)
    // must ensure this first; registration is idempotent and runs once per test-process.
    private static readonly SemaphoreSlim RegistrationLock = new(1, 1);
    private static bool _operatorRegistered;

    public static async Task EnsureOperatorCertificateRegisteredAsync(CancellationToken cancellationToken)
    {
        if (!IsConfigured || _operatorRegistered)
            return;

        await RegistrationLock.WaitAsync(cancellationToken);
        try
        {
            if (_operatorRegistered)
                return;

            var operatorOptions = OperatorOptions;
            using var adminStore = DocumentStoreFactory.Create(AdminOptions);
            var operatorCertificate = X509CertificateLoader.LoadPkcs12FromFile(
                operatorOptions.CertificatePath!,
                operatorOptions.CertificatePassword);

            await adminStore.Maintenance.Server.SendAsync(
                new PutClientCertificateOperation(
                    "ravendb-mcp-operator",
                    operatorCertificate,
                    new Dictionary<string, DatabaseAccess>(),
                    SecurityClearance.Operator),
                cancellationToken);

            _operatorRegistered = true;
        }
        finally
        {
            RegistrationLock.Release();
        }
    }

    private static string GetRequired(string name)
    {
        return Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException($"{name} must be set.");
    }
}

internal sealed class SecuredRavenDbFactAttribute : FactAttribute
{
    public SecuredRavenDbFactAttribute()
    {
        if (!SecuredRavenDbTestSettings.IsConfigured)
            Skip = SecuredRavenDbTestSettings.MissingMessage;
    }
}
