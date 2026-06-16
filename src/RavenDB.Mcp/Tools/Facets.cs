namespace RavenDB.Mcp.Tools;

// Selector enums for parameterized facet tools (ADR-0010). The MCP SDK maps each enum to a
// JSON-Schema `enum`, so the agent sees the exact allowed values. One enum per facet tool;
// added alongside the tool that uses it (no unused selectors).

/// <summary>What <c>wait_for_completion</c> blocks on.</summary>
public enum WaitCondition
{
    /// <summary>Wait for a server operation (by operationId) to reach a terminal state.</summary>
    Operation,

    /// <summary>Wait until the database has no stale indexes.</summary>
    Indexing
}

/// <summary>Live runtime activity returned by <c>get_live_workload</c>.</summary>
public enum WorkloadInclude
{
    /// <summary>Running operations (or the longest-running, server-wide).</summary>
    Operations,

    /// <summary>Currently running queries and the query cache.</summary>
    Queries,

    /// <summary>Server, database, and cluster transaction info.</summary>
    Transactions
}

/// <summary>What <c>get_network_details</c> returns.</summary>
public enum NetworkFacet
{
    /// <summary>Server TCP statistics (connection counts, bytes in/out).</summary>
    Stats,

    /// <summary>Active TCP connections (server-wide, or per database).</summary>
    Connections,

    /// <summary>TCP endpoint info to reach a database on a node (needs databaseName + nodeTag).</summary>
    DatabaseInfo
}

/// <summary>Shared helpers for parameterized facet tools.</summary>
internal static class Facet
{
    /// <summary>The requested selectors, or <paramref name="defaults"/> when none were supplied.</summary>
    public static HashSet<T> Resolve<T>(T[]? requested, params T[] defaults) where T : struct, Enum
        => new(requested is { Length: > 0 } ? requested : defaults);

    /// <summary>Guard a required argument for a facet section.</summary>
    public static string Require(string? value, string argName, string forSection)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{argName} is required for the '{forSection}' section.", argName)
            : value;

    /// <summary>Guard a facet that requires a database name.</summary>
    public static string RequireDatabase(string? databaseName, string forSection)
        => Require(databaseName, "databaseName", forSection);
}
