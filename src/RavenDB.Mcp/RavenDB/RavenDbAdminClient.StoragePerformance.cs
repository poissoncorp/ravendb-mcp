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
        var reportTask = GetStorageEnvironmentReport(databaseName, environmentName, environmentType, cancellationToken);
        var scratchBuffersTask = GetStorageScratchBufferInfo(databaseName, environmentName, environmentType, cancellationToken);
        var freeSpaceTask = GetStorageFreeSpaceSnapshot(databaseName, environmentName, environmentType, cancellationToken);
        await Task.WhenAll(reportTask, scratchBuffersTask, freeSpaceTask);

        var report = await reportTask;
        return new GetStorageEnvironmentDetailsResult(
            databaseName,
            report.EnvironmentName,
            report.EnvironmentType,
            report.Report,
            (await scratchBuffersTask).ScratchBuffers,
            (await freeSpaceTask).FreeSpace);
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
        var metricsTask = GetPerformanceOverview(cancellationToken);
        var cpuTask = GetCpuStats(cancellationToken);
        var ioTask = GetIoStats(null, cancellationToken);
        var gcTask = GetGcMemoryStats(cancellationToken);
        var memoryTask = GetOsMemoryStats(cancellationToken);
        var processTask = GetProcessStats(cancellationToken);

        await Task.WhenAll(metricsTask, cpuTask, ioTask, gcTask, memoryTask, processTask);

        var memory = (await memoryTask).Memory;
        var threads = memory.TryGetProperty("Threads", out var threadsValue)
            ? threadsValue.Clone()
            : ToJson(new { available = false, reason = "Threads not present in memory stats." });

        return new GetServerResourcesResult(
            (await metricsTask).Metrics,
            (await cpuTask).Cpu,
            (await ioTask).Io,
            (await gcTask).Gc,
            memory,
            (await processTask).Process,
            threads);
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

        var sample = await GetServerTextSample(path, seconds, cancellationToken);
        return new SampleRuntimeEventsResult(
            kind,
            Math.Clamp(seconds, 1, 30),
            sample.Text,
            sample.Truncated,
            sample.Limit);
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
            return new SampleThreadDiagnosticsResult(kind, 0, await GetServerText(path, cancellationToken), false, 0);

        var sample = await GetServerTextSample(path, seconds, cancellationToken);
        return new SampleThreadDiagnosticsResult(
            kind,
            Math.Clamp(seconds, 1, 30),
            sample.Text,
            sample.Truncated,
            sample.Limit);
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
