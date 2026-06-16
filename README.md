# RavenDB MCP

Read-only MCP diagnostics server for RavenDB. The server runs beside an AI agent over stdio, connects to a configured RavenDB URL, and exposes diagnostic tools for cluster, database, indexing, tasks, logs, storage, performance, and support artifacts.

## Install And Run

Create a flat config file:

```json
{
  "Urls": ["http://127.0.0.1:8070/"],
  "CertificatePath": "C:\\certs\\operator.pfx",
  "CertificatePassword": "password",
  "ArtifactsPath": "C:\\tmp\\ravendb-mcp-artifacts"
}
```

`CertificatePath`, `CertificatePassword`, and `ArtifactsPath` are optional. Leave certificate settings empty for unsecured RavenDB.

Point the server at a **single RavenDB cluster**. List that cluster's node URLs in `Urls`; the typed RavenDB client fails over across them, while raw diagnostic routes target the first URL.

If no URL is configured (no `--config` file and no environment variable), the server fails fast at startup with `At least one RavenDB URL must be configured`.

### Configuration via environment variables

Instead of `--config`, the server reads these environment variables (used by MCP clients that prompt for inputs, such as VS Code). A `--config` file, if provided, overrides them.

| Variable | Required | Maps to |
| --- | --- | --- |
| `RAVENDB_URLS` | yes | `Urls` (comma- or semicolon-separated) |
| `RAVENDB_CERTIFICATE_PATH` | no | `CertificatePath` |
| `RAVENDB_CERTIFICATE_PASSWORD` | no | `CertificatePassword` (secret) |
| `RAVENDB_ARTIFACTS_PATH` | no | `ArtifactsPath` |

### Self-contained executable

Download the release archive for your OS, extract it, and point the MCP client at the executable:

```json
{
  "command": "C:\\tools\\ravendb-mcp\\ravendb-mcp.exe",
  "args": ["--config", "C:\\tools\\ravendb-mcp\\ravendb-mcp.json"]
}
```

The self-contained executable does not require .NET to be installed on the machine.

### NuGet tool via `dnx` (recommended for MCP clients)

The package is published to NuGet.org as an `McpServer` package. With the .NET 10 SDK installed, MCP clients acquire and launch it in one shot with `dnx` (no separate install step):

```json
{
  "command": "dnx",
  "args": ["RavenDB.Mcp@0.1.0", "--yes"],
  "env": {
    "RAVENDB_URLS": "http://127.0.0.1:8070/"
  }
}
```

**VS Code / Visual Studio:** find the package on NuGet.org (it has an **MCP Server** tab), copy the generated snippet into your `.vscode/mcp.json`, and the editor prompts for the declared inputs (`RAVENDB_URLS`, certificate path/password, artifacts path).

**Claude Desktop / Claude Code:** add the same `command`/`args`/`env` block to your MCP server configuration.

You can still pass `--config <path>` in `args` instead of environment variables for secured or multi-setting configurations.

### .NET tool (global install)

For machines with the .NET SDK, install once and run by command name:

```powershell
dotnet tool install --global RavenDB.Mcp
```

```json
{
  "command": "ravendb-mcp",
  "args": ["--config", "C:\\tools\\ravendb-mcp\\ravendb-mcp.json"]
}
```

### Source checkout

For local development:

```powershell
dotnet run --project src/RavenDB.Mcp -- --config C:\tools\ravendb-mcp\ravendb-mcp.json
```

The transport is stdio, so logs are written to stderr.

Docker is used for RavenDB test fixtures, not as a v1 distribution path for the MCP server. Running a stdio MCP server inside Docker makes RavenDB URLs, certificate mounts, and agent process wiring harder for users.

## Build the distribution artifacts

Both distribution lanes are produced from the project with the .NET 10 SDK. CI runs these automatically on a tagged release (`.github/workflows/release.yml`); the commands below reproduce them locally.

### Self-contained executables (no .NET on the target machine)

Single-file, self-contained per operating system. Run on any OS — pick the runtime identifier:

```powershell
dotnet publish src/RavenDB.Mcp/RavenDB.Mcp.csproj -c Release -r win-x64   --self-contained true -p:PublishSingleFile=true -o dist/win-x64
dotnet publish src/RavenDB.Mcp/RavenDB.Mcp.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o dist/linux-x64
dotnet publish src/RavenDB.Mcp/RavenDB.Mcp.csproj -c Release -r osx-x64   --self-contained true -p:PublishSingleFile=true -o dist/osx-x64
dotnet publish src/RavenDB.Mcp/RavenDB.Mcp.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o dist/osx-arm64
```

Output: `dist/<rid>/ravendb-mcp.exe` (or `ravendb-mcp` on Linux/macOS). The executable is the only file required at runtime; the accompanying `.pdb` is debug symbols and can be omitted. No .NET runtime is needed on the target.

### NuGet `McpServer` package (for `dnx` / gallery)

```powershell
dotnet pack src/RavenDB.Mcp/RavenDB.Mcp.csproj -c Release -o dist/package
```

Output: `dist/package/RavenDB.Mcp.<version>.nupkg` — declares the `McpServer` and `DotnetTool` package types and embeds `.mcp/server.json`. Publish it with:

```powershell
dotnet nuget push dist/package/*.nupkg --api-key <NUGET_API_KEY> --source https://api.nuget.org/v3/index.json --skip-duplicate
```

### Point an MCP client at a locally-built executable

No NuGet/publish required — use the self-contained exe directly. Claude Code:

```powershell
claude mcp add ravendb -e RAVENDB_URLS=http://127.0.0.1:8070/ -- "<repo>\dist\win-x64\ravendb-mcp.exe"
```

Claude Desktop (`%APPDATA%\Claude\claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "ravendb": {
      "command": "<repo>\\dist\\win-x64\\ravendb-mcp.exe",
      "env": { "RAVENDB_URLS": "http://127.0.0.1:8070/" }
    }
  }
}
```

Replace `<repo>` with the absolute path to this checkout, and `RAVENDB_URLS` with your cluster. Restart the client; it will spawn the exe over stdio and list the tools.

## Tool Surface

**21 read-only tools** — 15 parameterized *facet* tools plus 6 singletons. Tool names are `snake_case`. Each facet tool takes enum/enum-array selectors (the agent sees the allowed values as a JSON-Schema `enum`) and returns a result keyed by the selected sections, so one tool covers a whole subject without bloating `tools/list`. Tools whose result is fully typed advertise a structured output schema (`UseStructuredContent`); facet tools and tools that pass through RavenDB's large/variable JSON return it as a text content block instead, so the tool list stays small and works with strict MCP clients.

Facet tools (selector-driven): `get_cluster_overview`, `get_server_config`, `get_server_resources`, `get_network_details`, `get_database_stats`, `get_database_config`, `get_index`, `get_tasks`, `get_live_workload`, `inspect_storage`, `get_document_data`, `sample_live_feed`, `wait_for_completion`, `collect_debug_package`, `get_ai_agents`.

Singletons: `list_databases`, `get_database_record` (secrets redacted), `get_notifications`, `run_query`, `list_compare_exchange`, `export_server_logs`.

Connection-string secrets (passwords, API keys, cloud credentials, SAS tokens) are masked as `***redacted***` at the database-record boundary, so `get_database_record` and any facet projecting the record never leak them.

Some tools return artifact references instead of large payloads:

```json
{
  "path": "C:\\tmp\\ravendb-mcp-artifacts\\...",
  "contentType": "application/octet-stream",
  "bytes": 12345
}
```

## Safety

The server exposes read-only tools only. MCP read-only annotations are used where the SDK supports them. Sensitive diagnostics are tracked as project metadata, not as a protocol guarantee; the current MCP SDK does not expose a standard `sensitive` tool annotation.

Use RavenDB permissions to control what the configured certificate can see. Operator certificates are the current target for secured diagnostics.

## Tests

Run unit and integration tests:

```powershell
dotnet test RavenDB.Mcp.slnx --configuration Release
```

RavenDB-backed tests require:

```powershell
$env:RAVENDB_TEST_URL = "http://127.0.0.1:8070/"
dotnet test RavenDB.Mcp.slnx --configuration Release
```

Start a local unsecured RavenDB test container for tests:

```powershell
./scripts/start-ravendb-test-container.ps1 -Port 8070 -Name ravendb-mcp-test
```

Secured RavenDB coverage is exercised by CI through the Docker/certificate setup in `.github/workflows/ci.yml`.
