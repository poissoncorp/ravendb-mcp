using System.Text.Json;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    public async Task<GetCollectionSampleShapeResult> GetCollectionSampleShape(
        string databaseName,
        string collectionName,
        int? sampleSize,
        CancellationToken cancellationToken)
    {
        ValidateName(collectionName, "Collection name", nameof(collectionName));

        var query = await PostDatabaseJson(
            databaseName,
            "/queries",
            new
            {
                Query = $"from {collectionName}",
                Start = 0,
                PageSize = Math.Clamp(sampleSize ?? 5, 1, 25)
            },
            cancellationToken);

        return new GetCollectionSampleShapeResult(
            databaseName,
            collectionName,
            SummarizeDocumentShape(query));
    }

    public async Task<GetHugeDocumentsReportResult> GetHugeDocumentsReport(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetHugeDocumentsReportResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/documents/huge", cancellationToken));
    }

    public Task<DiagnosticArtifactResult> ScanCorruptedDocumentIds(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return SaveDatabaseArtifact(databaseName, "corrupted-document-ids", "/debug/documents/scan-corrupted-ids", cancellationToken);
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

    public Task<DiagnosticArtifactResult> ExportDocumentIds(
        string databaseName,
        string? startsWith,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        return SaveDatabaseArtifact(
            databaseName,
            "document-ids",
            "/docs",
            cancellationToken,
            ("startsWith", startsWith),
            ("metadataOnly", "true"),
            ("pageSize", Math.Clamp(pageSize ?? 1024, 1, 10_000).ToString()));
    }

    public Task<DiagnosticArtifactResult> FindMissingAttachments(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return SaveDatabaseArtifact(databaseName, "missing-attachments", "/debug/attachments/missing", cancellationToken);
    }

    public async Task<GetRevisionsCollectionStatsResult> GetRevisionsCollectionStats(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetRevisionsCollectionStatsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/revisions/collections/stats", cancellationToken));
    }

    public async Task<QueryMetadataOnlyResult> QueryMetadataOnly(
        string databaseName,
        string query,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        ValidateName(query, "Query", nameof(query));

        var result = await PostDatabaseJson(
            databaseName,
            "/queries",
            new
            {
                Query = query,
                Start = 0,
                PageSize = Math.Clamp(pageSize ?? 0, 0, 32)
            },
            cancellationToken);

        return new QueryMetadataOnlyResult(databaseName, SelectQueryMetadata(result));
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

    public async Task<CollectDiagnosticSnapshotResult> CollectDiagnosticSnapshot(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var cluster = await GetClusterNodes(cancellationToken);
        var database = await GetDatabaseOverview(databaseName, cancellationToken);
        var indexes = await GetIndexingOverview(databaseName, cancellationToken);
        var tasks = await GetDatabaseTasks(databaseName, cancellationToken);
        var notifications = await TryGetDatabaseJson(databaseName, "/notifications", cancellationToken);
        var package = await CollectDatabaseInfoPackage(databaseName, cancellationToken);

        return new CollectDiagnosticSnapshotResult(
            databaseName,
            ToJson(cluster),
            ToJson(database),
            ToJson(indexes),
            tasks.Tasks,
            notifications,
            package);
    }

    private static JsonElement SummarizeDocumentShape(JsonElement queryResult)
    {
        if (!queryResult.TryGetProperty("Results", out var results) || results.ValueKind != JsonValueKind.Array)
            return ToJson(new { documents = 0, fields = new Dictionary<string, string[]>() });

        var fields = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);
        var count = 0;

        foreach (var document in results.EnumerateArray())
        {
            if (document.ValueKind != JsonValueKind.Object)
                continue;

            count++;

            foreach (var property in document.EnumerateObject())
            {
                if (property.NameEquals("@metadata"))
                    continue;

                if (!fields.TryGetValue(property.Name, out var kinds))
                {
                    kinds = [];
                    fields[property.Name] = kinds;
                }

                kinds.Add(property.Value.ValueKind.ToString());
            }
        }

        return ToJson(new
        {
            documents = count,
            fields = fields.ToDictionary(item => item.Key, item => item.Value.ToArray())
        });
    }

    private static JsonElement SelectQueryMetadata(JsonElement queryResult)
    {
        return ToJson(SelectProperties(
            queryResult,
            "DurationInMs",
            "TotalResults",
            "SkippedResults",
            "ScannedResults",
            "IndexName",
            "IsStale",
            "LastQueryTime",
            "ResultEtag"));
    }
}
