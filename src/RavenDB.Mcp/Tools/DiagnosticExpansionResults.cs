using System.Text.Json;

namespace RavenDB.Mcp.Tools;

public sealed record DiagnosticArtifactResult(string Path, string ContentType, long Bytes);

public sealed record DiagnosticTextSampleResult(string Kind, int Seconds, string Sample);

public sealed record GetServerDiagnosticsOverviewResult(
    JsonElement Routes,
    JsonElement Configuration,
    JsonElement Metrics,
    JsonElement CpuCredits,
    JsonElement IdleDatabases,
    JsonElement LicenseConnectivity,
    JsonElement ClusterMaintenance);

public sealed record GetClusterDiagnosticsOverviewResult(
    JsonElement ObserverDecisions,
    JsonElement ClusterLog,
    JsonElement History,
    JsonElement RemoteConnections,
    JsonElement EngineLogs,
    JsonElement StateChanges);

public sealed record PingClusterNodeResult(
    string Url,
    int? StatusCode,
    bool Success,
    long ElapsedMilliseconds,
    string? Error);

public sealed record GetIndexStalenessResult(string DatabaseName, string IndexName, JsonElement Staleness);

public sealed record GetIndexDebugDetailsResult(
    string DatabaseName,
    string IndexName,
    JsonElement Debug,
    JsonElement Metadata,
    JsonElement History);

public sealed record GetQueryDiagnosticsResult(
    string DatabaseName,
    JsonElement RunningQueries,
    JsonElement QueryCache);

public sealed record GetOperationsOverviewResult(
    string? DatabaseName,
    JsonElement RunningOperations,
    JsonElement ServerWideOperations);

public sealed record GetTransactionDiagnosticsResult(
    string? DatabaseName,
    JsonElement ServerTransactions,
    JsonElement DatabaseTransactions,
    JsonElement DatabaseClusterTransactions);

public sealed record WaitForConditionResult(
    string Kind,
    string DatabaseName,
    long? OperationId,
    string? IndexName,
    bool Completed,
    int Polls,
    JsonElement LastState);

public sealed record GetDocumentConflictsResult(string DatabaseName, string DocumentId, JsonElement Conflicts);

public sealed record GetBackupDiagnosticsResult(
    string DatabaseName,
    JsonElement Tasks,
    JsonElement NextOccurrences,
    JsonElement ServerWideConfigurations);

public sealed record GetEtlDiagnosticsResult(
    string DatabaseName,
    JsonElement Tasks,
    JsonElement Stats,
    JsonElement Performance,
    JsonElement DebugStats,
    JsonElement Progress);

public sealed record GetSubscriptionDiagnosticsResult(
    string DatabaseName,
    JsonElement Subscriptions,
    JsonElement State,
    JsonElement ConnectionDetails);

public sealed record GetTrafficWatchConfigurationResult(JsonElement Configuration);

public sealed record GetNotificationsResult(string? DatabaseName, JsonElement Notifications);

public sealed record GetCollectionSampleShapeResult(
    string DatabaseName,
    string CollectionName,
    JsonElement Shape);

public sealed record GetHugeDocumentsReportResult(string DatabaseName, JsonElement Report);

public sealed record GetDocumentRevisionsResult(string DatabaseName, string DocumentId, JsonElement Revisions);

public sealed record GetRevisionsCollectionStatsResult(string DatabaseName, JsonElement Stats);

public sealed record QueryMetadataOnlyResult(string DatabaseName, JsonElement Metadata);

public sealed record CollectDiagnosticSnapshotResult(
    string DatabaseName,
    JsonElement Cluster,
    JsonElement Database,
    JsonElement Indexes,
    JsonElement Tasks,
    JsonElement Notifications,
    DiagnosticArtifactResult DatabaseInfoPackage);
