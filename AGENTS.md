# RavenDB MCP — Agent Orientation

## What this is

A local, external **read-only MCP diagnostics server for RavenDB**, written in C# (.NET 10) and spoken over **stdio**. It connects to one RavenDB cluster via configured URL(s) and an optional client certificate, and exposes read-only tools so an AI agent can assess cluster/database/index/task/storage/performance state and (read-only) data without hand-crafting REST calls.

- Transport is stdio, so all logs go to **stderr** — never write to stdout.
- The server targets **one cluster**; list that cluster's node URLs in configuration.
- v1 is **read-only**: no write/delete tools. Read-only document/query access is allowed under ADR-0009; everything else is metadata/diagnostics.

## Rules for the agent working in this repo

- The source of truth for decisions is the PRD and ADRs under `docs/` (local/gitignored). Check them before changing scope, the tool surface, error semantics, data exposure, or distribution. Other notes under `docs/` may be stale — prefer the ADRs.
- Keep the tool surface and result schemas small: they are agent-facing API contracts and a context-window cost. Prefer permissive `JsonElement` payloads described in each tool's `[Description]` over fully-expanded schemas.
- Every tool is read-only and carries a dual-use description (when to use it + what it returns).
- Make failure explicit at the boundary; do not add hidden fallbacks, fake generality, or future-proofing the current feature does not need.
- Detailed coding/contract conventions and the ratified error/distribution/long-result decisions live in `docs/engineering-guidelines.md` and `docs/adr/`.
