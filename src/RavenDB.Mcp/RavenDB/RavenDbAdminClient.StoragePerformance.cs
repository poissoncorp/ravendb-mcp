using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    public async Task<GetStorageOverviewResult> GetStorageOverview(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetStorageOverviewResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/storage/report", cancellationToken),
            await GetDatabaseJson(databaseName, "/debug/storage/all-environments/report", cancellationToken));
    }

    public async Task<GetStorageTreesResult> GetStorageTrees(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetStorageTreesResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/storage/trees", cancellationToken));
    }

    public async Task<GetStorageEnvironmentReportResult> GetStorageEnvironmentReport(
        string databaseName,
        string? environmentName,
        string? environmentType,
        CancellationToken cancellationToken)
    {
        var name = string.IsNullOrWhiteSpace(environmentName) ? databaseName : environmentName;
        var type = string.IsNullOrWhiteSpace(environmentType) ? "Documents" : environmentType;

        return new GetStorageEnvironmentReportResult(
            databaseName,
            name,
            type,
            await GetDatabaseJson(
                databaseName,
                "/debug/storage/environment/report",
                cancellationToken,
                ("name", name),
                ("type", type)));
    }

    public async Task<GetStorageTreeStructureResult> GetStorageTreeStructure(
        string databaseName,
        string treeName,
        string? treeKind,
        CancellationToken cancellationToken)
    {
        ValidateName(treeName, "Tree name", nameof(treeName));

        var kind = string.IsNullOrWhiteSpace(treeKind) ? "btree" : treeKind;
        var path = kind.Equals("fixed_size", StringComparison.OrdinalIgnoreCase) ||
                   kind.Equals("fst", StringComparison.OrdinalIgnoreCase)
            ? "/debug/storage/fst-structure"
            : "/debug/storage/btree-structure";

        return new GetStorageTreeStructureResult(
            databaseName,
            treeName,
            kind,
            await GetDatabaseText(databaseName, path, cancellationToken, ("name", treeName)));
    }

    public async Task<GetStorageCompressionDictionariesResult> GetStorageCompressionDictionaries(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetStorageCompressionDictionariesResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/storage/compression-dictionaries", cancellationToken));
    }

    public async Task<GetStorageScratchBufferInfoResult> GetStorageScratchBufferInfo(
        string databaseName,
        string? environmentName,
        string? environmentType,
        CancellationToken cancellationToken)
    {
        var name = string.IsNullOrWhiteSpace(environmentName) ? databaseName : environmentName;
        var type = string.IsNullOrWhiteSpace(environmentType) ? "Documents" : environmentType;

        return new GetStorageScratchBufferInfoResult(
            databaseName,
            name,
            type,
            await GetDatabaseJson(
                databaseName,
                "/debug/storage/environment/scratch-buffer-info",
                cancellationToken,
                ("name", name),
                ("type", type)));
    }

    public async Task<GetStorageEnvironmentDetailsResult> GetStorageEnvironmentDetails(
        string databaseName,
        string? environmentName,
        string? environmentType,
        CancellationToken cancellationToken)
    {
        var report = await GetStorageEnvironmentReport(databaseName, environmentName, environmentType, cancellationToken);
        var scratchBuffers = await GetStorageScratchBufferInfo(databaseName, environmentName, environmentType, cancellationToken);
        var freeSpace = await GetStorageFreeSpaceSnapshot(databaseName, environmentName, environmentType, cancellationToken);

        return new GetStorageEnvironmentDetailsResult(
            databaseName,
            report.EnvironmentName,
            report.EnvironmentType,
            report.Report,
            scratchBuffers.ScratchBuffers,
            freeSpace.FreeSpace);
    }

    public async Task<GetStorageFreeSpaceSnapshotResult> GetStorageFreeSpaceSnapshot(
        string databaseName,
        string? environmentName,
        string? environmentType,
        CancellationToken cancellationToken)
    {
        var name = string.IsNullOrWhiteSpace(environmentName) ? databaseName : environmentName;
        var type = string.IsNullOrWhiteSpace(environmentType) ? "Documents" : environmentType;

        return new GetStorageFreeSpaceSnapshotResult(
            databaseName,
            name,
            type,
            await GetDatabaseJson(
                databaseName,
                "/debug/storage/environment/free-space-snapshot",
                cancellationToken,
                ("name", name),
                ("type", type)));
    }

    public async Task<GetPerformanceOverviewResult> GetPerformanceOverview(CancellationToken cancellationToken)
    {
        return new GetPerformanceOverviewResult(await GetServerJson("/admin/metrics", cancellationToken));
    }

    public async Task<GetServerResourcesResult> GetServerResources(CancellationToken cancellationToken)
    {
        var memory = await GetOsMemoryStats(cancellationToken);

        return new GetServerResourcesResult(
            (await GetPerformanceOverview(cancellationToken)).Metrics,
            (await GetCpuStats(cancellationToken)).Cpu,
            (await GetIoStats(null, cancellationToken)).Io,
            (await GetGcMemoryStats(cancellationToken)).Gc,
            memory.Memory,
            (await GetProcessStats(cancellationToken)).Process,
            memory.Memory.GetProperty("Threads").Clone());
    }

    public async Task<GetCpuStatsResult> GetCpuStats(CancellationToken cancellationToken)
    {
        return new GetCpuStatsResult(await GetServerJson("/admin/debug/cpu/stats", cancellationToken));
    }

    public async Task<GetIoStatsResult> GetIoStats(string? databaseName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            return new GetIoStatsResult(null, await GetServerJson("/admin/debug/io-metrics", cancellationToken));

        return new GetIoStatsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/io-metrics", cancellationToken));
    }

    public async Task<GetGcMemoryStatsResult> GetGcMemoryStats(CancellationToken cancellationToken)
    {
        return new GetGcMemoryStatsResult(await GetServerJson("/admin/debug/memory/gc", cancellationToken));
    }

    public async Task<GetOsMemoryStatsResult> GetOsMemoryStats(CancellationToken cancellationToken)
    {
        return new GetOsMemoryStatsResult(await GetServerJson("/admin/debug/memory/stats", cancellationToken));
    }

    public async Task<GetProcessStatsResult> GetProcessStats(CancellationToken cancellationToken)
    {
        return new GetProcessStatsResult(await GetServerJson("/admin/debug/proc/stats", cancellationToken));
    }

    public async Task<GetLowMemoryLogResult> GetLowMemoryLog(CancellationToken cancellationToken)
    {
        return new GetLowMemoryLogResult(await GetServerJson("/admin/debug/memory/low-mem-log", cancellationToken));
    }

    public async Task<GetEncryptionBufferPoolStatsResult> GetEncryptionBufferPoolStats(CancellationToken cancellationToken)
    {
        return new GetEncryptionBufferPoolStatsResult(await GetServerJson("/admin/debug/memory/encryption-buffer-pool", cancellationToken));
    }

    public async Task<SampleRuntimeEventsResult> SampleRuntimeEvents(
        string kind,
        int seconds,
        CancellationToken cancellationToken)
    {
        var path = kind.Equals("gc", StringComparison.OrdinalIgnoreCase)
            ? "/admin/debug/memory/gc-events"
            : "/admin/debug/memory/allocations";

        return new SampleRuntimeEventsResult(
            kind,
            Math.Clamp(seconds, 1, 30),
            await GetServerTextSample(path, seconds, cancellationToken));
    }

    public async Task<GetThreadStatsResult> GetThreadStats(CancellationToken cancellationToken)
    {
        var stats = await GetServerJson("/admin/debug/memory/stats", cancellationToken);
        return new GetThreadStatsResult(stats.GetProperty("Threads").Clone());
    }

    public async Task<SampleThreadDiagnosticsResult> SampleThreadDiagnostics(
        string kind,
        int seconds,
        CancellationToken cancellationToken)
    {
        var path = kind.Equals("contention", StringComparison.OrdinalIgnoreCase)
            ? "/admin/debug/threads/contention"
            : "/admin/debug/threads/runaway";

        if (path.EndsWith("/runaway", StringComparison.Ordinal))
            return new SampleThreadDiagnosticsResult(kind, 0, await GetServerText(path, cancellationToken));

        return new SampleThreadDiagnosticsResult(
            kind,
            Math.Clamp(seconds, 1, 30),
            await GetServerTextSample(path, seconds, cancellationToken));
    }

    public async Task<GetStackTracesResult> GetStackTraces(CancellationToken cancellationToken)
    {
        return new GetStackTracesResult(await GetServerJson("/admin/debug/threads/stack-trace", cancellationToken));
    }

    public async Task<GetScriptRunnersResult> GetScriptRunners(
        string? databaseName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            return new GetScriptRunnersResult(null, await GetServerJson("/admin/debug/script-runners", cancellationToken));

        return new GetScriptRunnersResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/script-runners", cancellationToken));
    }

    public async Task<GetTcpStatsResult> GetTcpStats(CancellationToken cancellationToken)
    {
        return new GetTcpStatsResult(await GetServerJson("/admin/debug/info/tcp/stats", cancellationToken));
    }

    public async Task<ListTcpConnectionsResult> ListTcpConnections(
        string? databaseName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            return new ListTcpConnectionsResult(null, await GetServerJson("/admin/debug/info/tcp/active-connections", cancellationToken));

        return new ListTcpConnectionsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/info/tcp", cancellationToken));
    }
}
