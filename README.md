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

### Self-contained executable

Download the release archive for your OS, extract it, and point the MCP client at the executable:

```json
{
  "command": "C:\\tools\\ravendb-mcp\\ravendb-mcp.exe",
  "args": ["--config", "C:\\tools\\ravendb-mcp\\ravendb-mcp.json"]
}
```

The self-contained executable does not require .NET to be installed on the machine.

### .NET tool

For machines with .NET installed:

```powershell
dotnet tool install --global RavenDB.Mcp
```

MCP client config:

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

## Tool Surface

The v1 tools are read-only and use structured results. Tool names are `snake_case`.

Current categories:

- cluster and node overview
- server diagnostics, configuration, logs, notifications, and traffic watch
- database record, configuration, stats, collections, and identities
- indexing overview, index details, terms, progress, staleness, and debug details
- operations, waits, queries, and transaction diagnostics
- ongoing tasks, backup, ETL, subscriptions, and replication diagnostics
- storage, memory, CPU, IO, TCP, stack traces, and runtime sampling
- document-shape diagnostics, revisions, document-id export, and support packages

Some tools return artifact references instead of large payloads:

```json
{
  "path": "C:\\tmp\\ravendb-mcp-artifacts\\...",
  "contentType": "application/octet-stream",
  "bytes": 12345
}
```

## Safety

The v1 server exposes read-only tools only. MCP read-only annotations are used where the SDK supports them. Sensitive diagnostics are tracked as project metadata, not as a protocol guarantee; the current MCP SDK does not expose a standard `sensitive` tool annotation.

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
