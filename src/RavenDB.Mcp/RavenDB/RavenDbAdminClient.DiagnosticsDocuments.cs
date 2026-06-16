using System.Text.Json;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    public async Task<GetHugeDocumentsReportResult> GetHugeDocumentsReport(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetHugeDocumentsReportResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/documents/huge", cancellationToken));
    }

    public async Task<GetDocumentRevisionsResult> GetDocumentRevisions(
        string databaseName,
        string documentId,
        int? start,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        ValidateName(documentId, "Document id", nameof(documentId));

        return new GetDocumentRevisionsResult(
            databaseName,
            documentId,
            await GetDatabaseJson(
                databaseName,
                "/revisions",
                cancellationToken,
                ("id", documentId),
                ("start", Math.Max(start ?? 0, 0).ToString()),
                ("pageSize", Math.Clamp(pageSize ?? 25, 1, 1024).ToString())));
    }

    public async Task<GetRevisionsCollectionStatsResult> GetRevisionsCollectionStats(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetRevisionsCollectionStatsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/revisions/collections/stats", cancellationToken));
    }

    public Task<DiagnosticArtifactResult> CollectServerInfoPackage(CancellationToken cancellationToken)
    {
        return SaveServerArtifact("server-info-package", "/admin/debug/info-package", cancellationToken);
    }

    public Task<DiagnosticArtifactResult> CollectClusterInfoPackage(CancellationToken cancellationToken)
    {
        return SaveServerArtifact("cluster-info-package", "/admin/debug/cluster-info-package", cancellationToken);
    }

    public Task<DiagnosticArtifactResult> CollectDatabaseInfoPackage(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return SaveDatabaseArtifact(databaseName, "database-info-package", "/debug/info-package", cancellationToken);
    }

}
