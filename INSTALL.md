# Installing RavenDB MCP

A local, **read-only** MCP diagnostics server for RavenDB. It runs beside an AI agent over
**stdio**, connects to a single RavenDB cluster, and exposes **21 read-only tools** so the agent
can inspect cluster / database / index / task / storage / performance state, logs, support
packages, and read-only data — without hand-writing client code, opening Studio, or hitting the
raw REST API.

> **Pre-release note.** This package is **not on NuGet yet**. For now you build it from this
> repository (the [From source](#install-options) options below). The NuGet-based options
> ([`dnx`](#option-d-nuget-via-dnx-once-published), [global tool](#option-e-global-tool-from-nuget-once-published))
> are documented for completeness but only work **once the package is published**.

---

## Table of contents

- [Prerequisites](#prerequisites)
- [Quick start (Claude Code)](#quick-start-claude-code)
- [Install options](#install-options)
  - [Option A — Self-contained executable (recommended, pre-release)](#option-a--self-contained-executable-recommended-pre-release)
  - [Option B — Global .NET tool from a locally-built package](#option-b--global-net-tool-from-a-locally-built-package)
  - [Option C — Run from source (`dotnet run`)](#option-c--run-from-source-dotnet-run)
  - [Option D — NuGet via `dnx` (once published)](#option-d--nuget-via-dnx-once-published)
  - [Option E — Global tool from NuGet (once published)](#option-e--global-tool-from-nuget-once-published)
- [Connecting it to a client](#connecting-it-to-a-client)
  - [Claude Code](#claude-code)
  - [Claude Desktop](#claude-desktop)
  - [VS Code / Visual Studio](#vs-code--visual-studio)
- [Configuration reference](#configuration-reference)
  - [Environment variables](#environment-variables)
  - [Config file (`--config`)](#config-file---config)
  - [Precedence](#precedence)
  - [Secured servers & certificates](#secured-servers--certificates)
  - [Single cluster, multiple nodes](#single-cluster-multiple-nodes)
  - [Artifacts path](#artifacts-path)
- [Verify the connection](#verify-the-connection)
- [Updating & removing](#updating--removing)
- [Troubleshooting](#troubleshooting)
- [FAQ](#faq)

---

## Prerequisites

| You need | For |
| --- | --- |
| **.NET 10 SDK** | Building from source (every pre-release option). Download from <https://dotnet.microsoft.com>. |
| **Claude Code CLI** (`claude`) | Registering and running the server. |
| **git** | Cloning the repository. |
| **Network access** to your RavenDB cluster | The server connects out to the URL(s) you configure. |
| A **client certificate** (`.pfx`) | Only for **secured** (HTTPS) clusters — see [Secured servers](#secured-servers--certificates). |

Docker is **not** required to run the server (it's only used for test fixtures).

---

## Quick start (Claude Code)

```powershell
# 1. Clone
git clone https://github.com/poissoncorp/ravendb-mcp.git
cd ravendb-mcp

# 2. Build a standalone executable (pick your OS RID — see Option A)
dotnet publish src/RavenDB.Mcp/RavenDB.Mcp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist/win-x64

# 3. Register it with Claude Code (plain-HTTP cluster, minimal)
claude mcp add ravendb --scope user --env RAVENDB_URLS=http://localhost:8080 -- "<repo>\dist\win-x64\ravendb-mcp.exe"

# 4. Verify
claude mcp list
```

Replace `<repo>` with the absolute path to your clone and `RAVENDB_URLS` with your cluster.
For a **secured** cluster, add the certificate env vars from
[Secured servers](#secured-servers--certificates). Then open a Claude Code session and run `/mcp`
(or just ask *"list my RavenDB databases"*).

---

## Install options

All pre-release options build from this checkout. **Option A** is recommended: a single standalone
binary, no .NET runtime needed at run time, and no build output leaking onto the stdio stream.

### Option A — Self-contained executable (recommended, pre-release)

```powershell
git clone https://github.com/poissoncorp/ravendb-mcp.git
cd ravendb-mcp
dotnet publish src/RavenDB.Mcp/RavenDB.Mcp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist/win-x64
```

Pick the **runtime identifier (RID)** for the target machine:

| OS / arch | RID | Output binary |
| --- | --- | --- |
| Windows x64 | `win-x64` | `dist/win-x64/ravendb-mcp.exe` |
| Windows ARM64 | `win-arm64` | `dist/win-arm64/ravendb-mcp.exe` |
| macOS Apple Silicon | `osx-arm64` | `dist/osx-arm64/ravendb-mcp` |
| macOS Intel | `osx-x64` | `dist/osx-x64/ravendb-mcp` |
| Linux x64 | `linux-x64` | `dist/linux-x64/ravendb-mcp` |

The produced binary is the only file required at run time; the `.pdb` next to it is debug symbols
and can be ignored. Point your client at this binary — see
[Connecting it to a client](#connecting-it-to-a-client).

### Option B — Global .NET tool from a locally-built package

Builds a NuGet package locally and installs it as a global tool, so the command becomes just
`ravendb-mcp` (no path to manage). Requires the .NET 10 runtime present at run time (you have it
via the SDK).

```powershell
git clone https://github.com/poissoncorp/ravendb-mcp.git
cd ravendb-mcp
dotnet pack src/RavenDB.Mcp/RavenDB.Mcp.csproj -c Release -o dist/package
dotnet tool install --global --add-source ./dist/package RavenDB.Mcp
```

Then register `ravendb-mcp` directly:

```powershell
claude mcp add ravendb --scope user --env RAVENDB_URLS=http://localhost:8080 -- ravendb-mcp
```

Update with `dotnet tool update --global --add-source ./dist/package RavenDB.Mcp`; remove with
`dotnet tool uninstall --global RavenDB.Mcp`. (You may need to open a new shell after install so
the tool is on `PATH`.)

### Option C — Run from source (`dotnet run`)

For local development on the server itself. Logs go to stderr; the transport is stdio.

```powershell
dotnet run --project src/RavenDB.Mcp -- --config C:\tools\ravendb-mcp\ravendb-mcp.json
```

> **Caveat for client wiring:** `dotnet run` can emit MSBuild output to **stdout** on first build,
> which corrupts the JSON-RPC handshake. If you wire this into a client, **build first**
> (`dotnet build -c Release`) or prefer **Option A**.

### Option D — NuGet via `dnx` (once published)

When the package is on NuGet.org, MCP clients can acquire and launch it in one shot with the
.NET 10 `dnx` runner — no separate install step:

```jsonc
{
  "command": "dnx",
  "args": ["RavenDB.Mcp@0.2.0", "--yes"],
  "env": { "RAVENDB_URLS": "http://localhost:8080" }
}
```

### Option E — Global tool from NuGet (once published)

```powershell
dotnet tool install --global RavenDB.Mcp
```

```jsonc
{
  "command": "ravendb-mcp",
  "args": ["--config", "C:\\tools\\ravendb-mcp\\ravendb-mcp.json"]
}
```

---

## Connecting it to a client

Whichever install option you used, you end up pointing the client at either a **binary path**
(Option A), the **`ravendb-mcp`** command (Options B/E), or **`dnx`** (Option D). Configuration is
always passed the same way: environment variables or a `--config` file (see
[Configuration reference](#configuration-reference)).

### Claude Code

Add via the CLI. The `--` separates Claude's own flags from the server command and its arguments.

```powershell
# Minimal (plain HTTP)
claude mcp add ravendb --scope user --env RAVENDB_URLS=http://localhost:8080 -- "<repo>\dist\win-x64\ravendb-mcp.exe"
```

**Scope (`--scope`, short `-s`)** controls where the server is registered:

| Scope | Flag | Stored in | Visible to |
| --- | --- | --- | --- |
| Local (default) | *(none)* | `~/.claude.json` (this project only) | you, in the current folder only |
| User | `--scope user` | `~/.claude.json` | you, in **every** project |
| Project | `--scope project` | `.mcp.json` (committed) | anyone who clones the repo |

For testing across all your work, `--scope user` is the convenient choice.

Prefer a config file over env vars? Pass it as a server argument:

```powershell
claude mcp add ravendb --scope user -- "<repo>\dist\win-x64\ravendb-mcp.exe" --config "C:\tools\ravendb-mcp\ravendb-mcp.json"
```

Or add it as JSON in one go:

```powershell
claude mcp add-json ravendb '{"type":"stdio","command":"<repo>\\dist\\win-x64\\ravendb-mcp.exe","args":[],"env":{"RAVENDB_URLS":"http://localhost:8080"}}'
```

### Claude Desktop

Edit the config file (`%APPDATA%\Claude\claude_desktop_config.json` on Windows,
`~/Library/Application Support/Claude/claude_desktop_config.json` on macOS) and restart the app:

```json
{
  "mcpServers": {
    "ravendb": {
      "type": "stdio",
      "command": "C:\\path\\to\\ravendb-mcp\\dist\\win-x64\\ravendb-mcp.exe",
      "env": {
        "RAVENDB_URLS": "http://localhost:8080"
      }
    }
  }
}
```

### VS Code / Visual Studio

Create `.vscode/mcp.json` (the editor prompts for declared inputs):

```json
{
  "servers": {
    "ravendb": {
      "type": "stdio",
      "command": "C:\\path\\to\\ravendb-mcp\\dist\\win-x64\\ravendb-mcp.exe",
      "env": { "RAVENDB_URLS": "http://localhost:8080" }
    }
  }
}
```

---

## Configuration reference

The server targets **one RavenDB cluster**. The only required setting is the cluster URL(s);
everything else is optional.

### Environment variables

Used by clients that pass `env` (Claude Code/Desktop, VS Code).

| Variable | Required | Maps to | Notes |
| --- | --- | --- | --- |
| `RAVENDB_URLS` | **yes** | `Urls` | One or more node URLs of a **single** cluster, comma- or semicolon-separated. |
| `RAVENDB_CERTIFICATE_PATH` | no | `CertificatePath` | Path to a client `.pfx` for **secured** clusters. Omit for unsecured. |
| `RAVENDB_CERTIFICATE_PASSWORD` | no | `CertificatePassword` | Password for the `.pfx` (secret). Omit if the cert has none. |
| `RAVENDB_ARTIFACTS_PATH` | no | `ArtifactsPath` | Where exported files are written. See [Artifacts path](#artifacts-path). |

### Config file (`--config`)

Pass `--config <path>` as a server argument to load a flat JSON file instead of (or on top of)
env vars:

```json
{
  "Urls": ["https://node-a.example.development.run:443", "https://node-b.example.development.run:443"],
  "CertificatePath": "C:\\certs\\operator.client.certificate.pfx",
  "CertificatePassword": "secret",
  "ArtifactsPath": "D:\\ravendb-mcp-artifacts"
}
```

`CertificatePath`, `CertificatePassword`, and `ArtifactsPath` are optional. (JSON requires
escaping Windows backslashes as `\\`.)

### Precedence

Environment variables are applied first; a `--config` file, if provided, **overrides** them. If no
URL is configured by either, the server fails fast at startup with:

```
At least one RavenDB URL must be configured.
```

### Secured servers & certificates

Secured RavenDB uses **HTTPS** and authenticates clients with a `.pfx` certificate. Point the
server at one:

```powershell
claude mcp add ravendb --scope user --env RAVENDB_URLS=https://node-a.example.development.run:443 --env RAVENDB_CERTIFICATE_PATH="C:\certs\operator.client.certificate.pfx" --env RAVENDB_CERTIFICATE_PASSWORD=pfxPasswordIfAny -- "<repo>\dist\win-x64\ravendb-mcp.exe"
```

- **Use an Operator-clearance certificate.** It grants cluster-wide read access for diagnostics
  without full Cluster Admin rights — the right level for a read-only diagnostics agent. The
  certificate's RavenDB permissions are what ultimately bound what the agent can see.
- Drop the password env var if your `.pfx` is not password-protected.
- For **unsecured** (HTTP) clusters, leave both certificate settings unset.

Download or generate a client certificate from RavenDB Studio → **Manage Server → Certificates**.

### Single cluster, multiple nodes

List all of the cluster's node URLs in `RAVENDB_URLS`. The typed RavenDB client **fails over**
across them; raw diagnostic routes target the **first** URL. The server connects to **one cluster**
— to inspect a different cluster, register a second MCP server entry with its own name and URLs.

### Artifacts path

Some tools (logs, debug/support packages, large reports) write a file to disk and hand the agent a
small reference instead of flooding the context:

```json
{ "path": "...\\ravendb-mcp-artifacts\\...", "contentType": "application/octet-stream", "bytes": 12345 }
```

If `RAVENDB_ARTIFACTS_PATH` is **not** set, files default to a **`ravendb-mcp-artifacts` folder
inside the system temp directory** (`%TEMP%\ravendb-mcp-artifacts` on Windows,
`$TMPDIR/ravendb-mcp-artifacts` or `/tmp/ravendb-mcp-artifacts` on macOS/Linux). Set the variable
only when you want artifacts in a **persistent, predictable** location — the OS may purge temp.

---

## Verify the connection

```powershell
claude mcp list          # lists servers with a ✓ / ✗ connection status
claude mcp get ravendb   # shows the resolved command, args, and env for this server
```

Inside a Claude Code session, run `/mcp` to see the connected server and its tools, then try a
prompt such as *"list my RavenDB databases"* or *"show cluster overview."*

---

## Updating & removing

```powershell
# Re-register (e.g. after changing URL/cert): remove then add again
claude mcp remove ravendb

# Option A — rebuild the binary; same path means the client picks it up on next launch
dotnet publish src/RavenDB.Mcp/RavenDB.Mcp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist/win-x64

# Option B — update the global tool from the rebuilt package
dotnet pack src/RavenDB.Mcp/RavenDB.Mcp.csproj -c Release -o dist/package
dotnet tool update --global --add-source ./dist/package RavenDB.Mcp
```

---

## Troubleshooting

| Symptom | Likely cause / fix |
| --- | --- |
| `At least one RavenDB URL must be configured` at startup | No `RAVENDB_URLS` and no `--config` URL. Set one. |
| `✗` / "failed to connect" in `claude mcp list` | URL unreachable, wrong scheme (`http` vs `https`), or (secured) bad/missing certificate path or clearance. |
| Tools never appear after `add` | Open a **new** Claude Code session; user-scope servers load on session start. |
| `claude mcp add` says the name already exists | `claude mcp remove ravendb` first, then re-add. |
| Garbage / handshake errors when using `dotnet run` | Build output hit stdout. `dotnet build -c Release` first, or use **Option A**. |
| Secured cluster rejects the agent | Certificate clearance too low or expired — use an **Operator** cert (or one with read on the target databases). |
| Windows path errors in JSON config | Escape backslashes as `\\`. |

The server logs to **stderr** (stdout is reserved for the JSON-RPC protocol), so client logs are
where you'll see startup and connection messages.

---

## FAQ

**Is it really read-only? Can it change or delete my data?**
Yes, read-only. There are no write/delete tools — only metadata/diagnostics plus read-only document
and query access. Connection-string secrets (passwords, API keys, cloud credentials, SAS tokens)
are masked as `***redacted***` at the database-record boundary, so they don't leak through
`get_database_record` or any tool projecting it.

**Do I need .NET installed to run it?**
With the **self-contained executable (Option A)**, no — the binary bundles the runtime; you only
need the SDK to build it. The global tool, `dotnet run`, and `dnx` need .NET present on the machine.

**Do I need Docker?**
No. Docker is only used for RavenDB test fixtures, not to run the server.

**What's the difference between a secured and an unsecured cluster here?**
Secured = HTTPS + client certificate (set `RAVENDB_CERTIFICATE_PATH`, and password if any).
Unsecured = HTTP, no certificate. The server works with both.

**Which certificate clearance should I use?**
**Operator** is preferred — cluster-wide read for diagnostics without full Cluster Admin. The
certificate's RavenDB permissions bound what the agent can see.

**Where do exported files go by default?**
A `ravendb-mcp-artifacts` folder inside the system temp directory. Set `RAVENDB_ARTIFACTS_PATH`
for a persistent location. See [Artifacts path](#artifacts-path).

**Can one server connect to multiple clusters?**
No — one server targets one cluster. List that cluster's nodes in `RAVENDB_URLS`. For another
cluster, register a second MCP server with a different name.

**My `.pfx` password ends up in the client config in plain text. Is that OK?**
For internal testing, yes. To keep it out of the client config, use a `--config` JSON file (still
plaintext on disk, but a single file you control) or a certificate without a password.

**Which RID do I pick for Option A?**
Match the machine that runs Claude: Windows x64 → `win-x64`, Apple Silicon → `osx-arm64`, etc. See
the table under [Option A](#option-a--self-contained-executable-recommended-pre-release).

**Can I use a config file instead of environment variables?**
Yes — pass `--config <path>` as a server argument. It overrides any env vars.

**How many tools does it expose, and won't that bloat my context?**
21 read-only tools. Most are *facet* tools that take selectors and return only the requested
sections, which keeps the tool list small and the responses scoped.

**Is it published to NuGet yet?**
Not yet. Until it is, use the from-source options (A/B/C). The `dnx` and NuGet global-tool options
are documented for when it ships.
