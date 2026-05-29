using System.Text.Json;

namespace RavenDB.Mcp.Tests;

public sealed class RawRouteVerificationTests(RavenDbTestFixture fixture)
    : IClassFixture<RavenDbTestFixture>
{
    private static readonly string[] ServerRoutes =
    [
        "/debug/routes",
        "/admin/configuration/settings",
        "/admin/metrics",
        "/debug/cpu-credits",
        "/admin/debug/databases/idle",
        "/license-server/connectivity",
        "/admin/cluster/maintenance-stats",
        "/admin/cluster/observer/decisions",
        "/admin/cluster/log",
        "/admin/debug/cluster/history-logs",
        "/admin/debug/node/ping",
        "/admin/debug/node/remote-connections",
        "/admin/debug/node/engine-logs",
        "/admin/debug/node/state-change-history",
        "/cluster-dashboard/watch",
        "/admin/debug/operations/longest-running",
        "/admin/debug/txinfo",
        "/admin/configuration/server-wide/backup",
        "/admin/traffic-watch/configuration",
        "/admin/logs/download",
        "/admin/traffic-watch",
        "/admin/server/notifications",
        "/admin/logs/watch",
        "/admin/debug/info-package",
        "/admin/debug/cluster-info-package",
        "/admin/debug/cpu/stats",
        "/admin/debug/io-metrics",
        "/admin/debug/memory/gc",
        "/admin/debug/memory/stats",
        "/admin/debug/proc/stats",
        "/admin/debug/memory/low-mem-log",
        "/admin/debug/memory/encryption-buffer-pool",
        "/admin/debug/memory/gc-events",
        "/admin/debug/memory/allocations",
        "/admin/debug/threads/contention",
        "/admin/debug/threads/runaway",
        "/admin/debug/threads/stack-trace",
        "/admin/debug/script-runners",
        "/admin/debug/info/tcp/stats",
        "/admin/debug/info/tcp/active-connections"
    ];

    private static readonly string[] DatabaseRoutes =
    [
        "/queries",
        "/debug/documents/huge",
        "/debug/documents/scan-corrupted-ids",
        "/revisions",
        "/docs",
        "/debug/attachments/missing",
        "/revisions/collections/stats",
        "/debug/info-package",
        "/notifications",
        "/indexes/progress",
        "/indexes/suggest-index-merge",
        "/indexes/total-time",
        "/replication/active-connections",
        "/replication/conflicts",
        "/replication/debug/outgoing-failures",
        "/replication/debug/incoming-last-activity-time",
        "/replication/debug/incoming-rejection-info",
        "/replication/debug/outgoing-reconnect-queue",
        "/replication/progress",
        "/replication/internal/outgoing/progress",
        "/indexes/staleness",
        "/indexes/debug",
        "/indexes/debug/metadata",
        "/indexes/history",
        "/debug/queries/running",
        "/debug/queries/cache/list",
        "/operations",
        "/admin/debug/txinfo",
        "/admin/debug/cluster/txinfo",
        "/admin/debug/periodic-backup/timers",
        "/etl/stats",
        "/etl/performance",
        "/etl/debug/stats",
        "/etl/progress",
        "/subscriptions/state",
        "/subscriptions/connection-details",
        "/debug/storage/report",
        "/debug/storage/all-environments/report",
        "/debug/storage/trees",
        "/debug/storage/environment/report",
        "/debug/storage/fst-structure",
        "/debug/storage/btree-structure",
        "/debug/storage/compression-dictionaries",
        "/debug/storage/environment/scratch-buffer-info",
        "/debug/storage/environment/free-space-snapshot",
        "/debug/io-metrics",
        "/debug/script-runners",
        "/info/tcp"
    ];

    [Fact]
    public async Task RawRoutesUsedByToolsExistInRavenDb72()
    {
        using var http = new HttpClient();
        await using var stream = await http.GetStreamAsync(new Uri(new Uri(fixture.Url), "/debug/routes"));
        using var document = await JsonDocument.ParseAsync(stream);

        var availableRoutes = document.RootElement
            .EnumerateObject()
            .SelectMany(category => category.Value.EnumerateArray())
            .Select(route => route.GetProperty("Path").GetString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var expectedRoutes = ServerRoutes.Concat(DatabaseRoutes.Select(route => $"/databases/*{route}"));

        foreach (var route in expectedRoutes.Distinct(StringComparer.OrdinalIgnoreCase))
            Assert.Contains(route, availableRoutes);
    }
}
