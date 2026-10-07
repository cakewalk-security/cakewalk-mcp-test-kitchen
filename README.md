# MCP Test Kitchen

Configurable MCP testbed for exercising MCP clients and gateways against fault-injection scenarios, with a live observation log of every request.

- Streamable HTTP MCP at `/mcp` (PAT bearer auth)
- React admin UI and management API at `/api/management/*` (Google cookie auth)
- Postgres-backed per-user scenario selection and MCP observations

## Live demo

Try it without running anything: **https://mcp-test-kitchen.cakewalk.security/login**

1. Sign in with Google or GitHub.
2. Copy your personal PAT from the console.
3. Point your MCP client at `https://mcp-test-kitchen.cakewalk.security/mcp` with the header `Authorization: Bearer <your-pat>`.
4. Pick a scenario in the console and watch your client's requests arrive in the live observation log.

## Prerequisites

- .NET 10 SDK
- Node.js 22.17.1 (`client/.nvmrc`)
- Docker (Postgres and integration tests)

## Quick start (Docker)

```bash
cp docker-compose.override.yml.template-mac docker-compose.override.yml   # or -linux / -win
docker compose up --build
```

| Service | URL |
|---------|-----|
| API + MCP + UI | `http://localhost:5094` |
| MCP endpoint | `http://localhost:5094/mcp` |
| Postgres | `localhost:15433` |

Default shared MCP PAT in the override template: `local-dev-pat`.

For admin UI login, uncomment and set `INTERNAL_AUTHENTICATION_GOOGLE_CLIENT_ID`, `INTERNAL_AUTHENTICATION_GOOGLE_CLIENT_SECRET`, and `MCP_TEST_SERVER_OAUTH_PUBLIC_ORIGIN` in `docker-compose.override.yml`. Without Google OAuth, `/mcp` still works; management routes return `401`.

Unauthenticated visitors to `/` are redirected to `/login`. Sign in there (or via `/Account/Login`, which redirects to the same page). Provider login entry points:

- Google: `/Account/Login/Google?returnUrl=%2Fconsole`
- GitHub: `/Account/Login/GitHub?returnUrl=%2Fconsole`

Stop with `docker compose down` (`-v` drops the Postgres volume).

## Logging

Production (Fly.io) writes **one JSON object per event** to stdout. Each object includes `Category` (the `ILogger<T>` type, for example `Microsoft.EntityFrameworkCore.Database.Command`), `LogLevel`, `Timestamp`, `Message`, and any named properties from the message template.

HTTP completions are logged for non-GET traffic (such as `POST /mcp`), status codes 400+, and requests slower than 1s. Successful `GET /mcp` probes, health checks, and `/assets/` are omitted so Fly.io is not flooded.

Development keeps a single-line text formatter so `dotnet run` stays readable.

Levels live in `src/McpTestServer.API/appsettings.json`. EF SQL is `Warning` by default (that is the flood in the Fly log view). Override with env vars, no rebuild:

```bash
# See SQL again
Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=Information

# Quieter app logs
Logging__LogLevel__Default=Warning
```

In code, inject `ILogger<YourType>` and use message templates so values become JSON fields:

```csharp
logger.LogInformation("Pruned {DeletedCount} observations", deletedCount);
```

`Category` will be `McpTestServer.API.…YourType`.

## Local development

**Postgres only in Docker:**

```bash
docker compose up -d postgres
```

**API** (migrations run on startup):

```bash
cd src/McpTestServer.API
dotnet run --launch-profile http
```

Listens on `http://localhost:5094`. `launchSettings.json` points at Postgres on `localhost:15433` and sets `McpTestServer__MCP_PAT=local-dev-pat`.

**React client with HMR** (optional):

```bash
cd client
npm install
npm run dev
```

Vite serves `http://localhost:5174` and proxies `/api`, `/Account`, `/signin-google`, and `/signin-github` to the API.

### Auth

**MCP PAT** — each Google-authenticated user gets a personal PAT from **Your MCP PAT** in the admin UI. Use it in Cursor; observations show the linked email in the **Caller** column. A legacy shared PAT via `McpTestServer__MCP_PAT` still works (caller shows `—`).

**Google OAuth** — create a Google OAuth client and set:

```bash
export INTERNAL_AUTHENTICATION_GOOGLE_CLIENT_ID=...
export INTERNAL_AUTHENTICATION_GOOGLE_CLIENT_SECRET=...
export MCP_TEST_SERVER_OAUTH_PUBLIC_ORIGIN=http://localhost:5094   # or :5174 for Vite dev
```

**GitHub OAuth** — create a GitHub OAuth App and set:

```bash
export INTERNAL_AUTHENTICATION_GITHUB_CLIENT_ID=...
export INTERNAL_AUTHENTICATION_GITHUB_CLIENT_SECRET=...
```

Register callback `https://<app-host>/signin-github` (or `http://localhost:5094/signin-github` locally). GitHub users can access the console but never receive admin access, even with an email on the admin domain.

Add the matching redirect URI in Google Cloud Console:

| How you open the UI | Redirect URI |
|---------------------|--------------|
| Docker / `dotnet run` | `http://localhost:5094/signin-google` |
| Vite dev server | `http://localhost:5174/signin-google` |

**Admin usage page** — `/admin` shows masked, aggregated usage across all users. It is available to users signed in with a Google Workspace account on the configured domain (the email must be verified and Google's hosted-domain claim must match; consumer Gmail accounts never qualify); with no domain set, nobody has admin access:

```bash
export MCP_TEST_SERVER_ADMIN_EMAIL_DOMAIN=example.com
```

**Feedback emails** (optional) — the console's feedback form sends mail through [Resend](https://resend.com). Both values are required; without either, feedback is accepted and discarded with a warning in the logs:

```bash
export RESEND_APITOKEN=re_...
export MCP_TEST_SERVER_FEEDBACK_RECIPIENT=feedback@example.com
export MCP_TEST_SERVER_FEEDBACK_FROM="MCP Test Kitchen <kitchen@example.com>"   # optional
```

In Development, if no PAT is configured, `/mcp` allows anonymous access. Outside Development, missing PAT fails closed with `401`.

Docker persists ASP.NET Data Protection keys so encrypted personal PATs survive restarts.

### Connect Cursor

```json
{
  "mcpServers": {
    "mcp-test-kitchen": {
      "url": "http://localhost:5094/mcp",
      "headers": {
        "Authorization": "Bearer local-dev-pat"
      }
    }
  }
}
```

Use your personal PAT from the admin UI when logged in.

### Always-on catalog items

Every MCP session advertises these resources and prompts regardless of the selected scenario. Use them to exercise happy-path and schema-invalid fetch handling without changing scenario params.

| Kind | Name | URI / lookup | Behavior |
|------|------|--------------|----------|
| Resource | `valid_resource` | `test://resources/valid` | `resources/read` returns valid `text/plain` contents |
| Resource | `invalid_resource` | `test://resources/invalid` | `resources/read` returns HTTP 200 with a schema-invalid JSON-RPC result |
| Prompt | `valid_prompt` | name `valid_prompt` | `prompts/get` returns a valid user text message (optional `topic` argument) |
| Prompt | `invalid_prompt` | name `invalid_prompt` | `prompts/get` returns HTTP 200 with a schema-invalid JSON-RPC result |

### TypeScript test client (2026-07-28)

The reference client lives in the public repo [cakewalk-mcp-ts-test-client](https://github.com/cakewalk-security/cakewalk-mcp-ts-test-client). Connects with native `2026-07-28` protocol.

```bash
git clone https://github.com/cakewalk-security/cakewalk-mcp-ts-test-client.git
cd cakewalk-mcp-ts-test-client
cp .env.example .env            # set MCP_URL and MCP_PAT
npm install
npm start                       # list-tools + run-scenario
npm start -- list-tools         # tools only
npm start -- run-scenario       # scenario only
npm start -- list-resources     # resources/list
npm start -- read-resource      # resources/read valid_resource
npm start -- list-prompts       # prompts/list
npm start -- get-prompt         # prompts/get valid_prompt
```

## Rider / JetBrains

Open `McpTestServer.slnx` (includes `docker-compose.dcproj`). Copy a `docker-compose.override.yml.template-*` to `docker-compose.override.yml`, then run the **Docker Compose** configuration for `mcp-test-server.api`. Logs: `docker compose logs -f mcp-test-server.api`.

## Scenarios

Scenarios are code-first. Each MCP session exposes `run_configured_test_scenario`; some scenarios also register dedicated tools. Pick a scenario and params in the admin UI; the selection applies to your **next** MCP session (reconnecting in Cursor is enough — you do not need **Terminate sessions** unless you want to force-close a still-active connection). While a live MCP session is running, changing the saved selection does not mutate that session.

Six scenarios are enabled by default. Each exercises a different layer of the MCP stack so you can see exactly where your client breaks. Example params are also shown in the admin UI (**Load example**).

| ID | Layer | Description | Example params |
|----|-------|-------------|----------------|
| `baseline` | Success | Healthy server; optional delay before returning | `{"slowReportDelayMs":0}` |
| `errors.http_status_sequence` | HTTP transport | Returns configured HTTP status codes across successive MCP POSTs (2xx passes through) | `{"statusCodes":[503,200]}` |
| `errors.jsonrpc_error` | JSON-RPC envelope | Returns a JSON-RPC error on a configured `tools/call` invocation (HTTP 200 + error object) | `{"code":-32602,"message":"Invalid params","onInvocation":1}` |
| `errors.tool_error` | Tool result | Returns a valid MCP response with `isError: true` | `{"errorMessage":"Tool-side failure","onInvocation":1}` |
| `elicitation.approval` | Elicitation (MRTR) | Prompts the client for user approval via `elicitation/create` before completing `run_configured_test_scenario` | See admin UI **Load example** |
| `compat.sdk_v2` | C# SDK v2 | Dedicated tools for the official MCP C# SDK 2.0 / 2026-07-28 backward-compat matrix (`simulate_ticket_close_mrtr`, `show_negotiated_mcp_protocol`, `verify_order_region_param_header`). See [the v2 announcement](https://devblogs.microsoft.com/dotnet/announcing-v20-of-the-official-mcp-csharp-sdk/). | See admin UI **Load example** |

Clients must advertise elicitation support and handle `elicitation/create` (the TypeScript test client does this). Works over stateless Streamable HTTP with native 2026-07-28 MRTR.

`compat.sdk_v2` is the recommended way to test a client against the SDK v2 announcement: call `simulate_ticket_close_mrtr` with `closeReason` (any protocol), without it on a 2026-07-28 client (native MRTR), or without it on a 2025-11-25 session-less client (guidance to resend). `run_configured_test_scenario` returns the matrix of which cells this **stateless** host covers. Stateful SDK bridging (`elicitation/create` on a live session) is not available because the transport is `Stateless = true`.

### Disabled scenarios

Additional scenario implementations remain in `src/McpTestServer.API/Scenarios/` but are **not registered** in DI (timeout, auth, catalog, other elicitation variants, and other error variants). To re-enable one:

1. Add `services.AddScenario<YourScenario>();` in `Extensions/ServiceCollectionExtensions.cs`.
2. Add a `case` in `UserScenarioSelectionService.ValidateParamsForScenario`.
3. Un-skip its tests (search for `ScenarioTestSkipReasons.ScenarioNotRegistered`).

## Adding a scenario

1. **Add an ID** in `src/McpTestServer.API/Scenarios/ScenarioIds.cs` and (optionally) an area in `ScenarioAreas.cs`.

2. **Create a folder** under `src/McpTestServer.API/Scenarios/<area>/`:
   - `<Name>Params.cs` — JSON params with `[JsonPropertyName]` attributes
   - `<Name>Scenario.cs` — class extending `ScenarioBase` with a `ScenarioMetadata` record (id, title, description, area, example params JSON)

3. **Implement behavior** by overriding:
   - `RunToolAsync` — tool-call logic (read params via `session.GetParams<T>()`)
   - `OnRequestAsync` — wire-level behavior before MCP handling (return `WireDecision.RespondWithStatus`, `RespondWithBody`, `CloseConnection`, or `Delay`)
   - `ConfigureSession` — optional custom tool/resource collections (see `CatalogMutationScenario`)

   See `HttpStatusSequenceScenario` for wire faults, `MalformedPayloadScenario` for raw wire responses, and `ElicitationApprovalScenario` for server-to-client elicitation.

4. **Register** in `Extensions/ServiceCollectionExtensions.cs`:

   ```csharp
   services.AddScenario<MyNewScenario>();
   ```

5. **Validate params** in `UserScenarioSelectionService.ValidateParamsForScenario` (add a `case` that calls `scenarioCatalog.ValidateParams<MyNewParams>`).

6. **Add tests** under `tests/McpTestServer.API.Tests/Scenarios/`.

The admin UI lists scenarios from `GET /api/management/scenarios` automatically once registered.

## Management API

| Endpoint | Description |
|----------|-------------|
| `GET /api/health` | Health check (anonymous) |
| `GET /api/management/me` | Current user |
| `GET /api/management/me/pat` | Get or create personal MCP PAT |
| `POST /api/management/me/pat/regenerate` | Regenerate personal MCP PAT |
| `GET /api/management/scenarios` | List scenario definitions |
| `GET/PUT /api/management/me/scenario` | Read or save scenario selection |
| `GET /api/management/me/sessions` | Live MCP sessions |
| `POST /api/management/me/sessions/terminate` | Terminate live sessions |
| `POST /api/management/me/erase` | Erase the current user's stored data (requires confirmation phrase `erase everything`) |
| `GET /api/management/observations` | Recent MCP observations |
| `GET /api/management/observations/stream` | SSE stream for live UI |
| `DELETE /api/management/observations` | Clear observations |
| `GET /api/management/runtime` | Live sessions and per-tool invocation counts |
| `POST /api/management/runtime/reset` | Reset invocation counters |

## Tests

Requires Docker (Testcontainers Postgres):

```bash
dotnet test tests/McpTestServer.API.Tests/McpTestServer.API.Tests.csproj
```

Client:

```bash
cd client
npm ci
npm test
npm run lint
```

## Deploying

The image built from `src/McpTestServer.API/Dockerfile` runs anywhere that provides Postgres and a persistent volume for ASP.NET Data Protection keys (`MCP_TEST_SERVER_DATA_PROTECTION_KEYS_PATH`). `DATABASE_URL` (`postgres://` URI) takes precedence over `Database__CONNECTION_STRING`. Migrations run on startup. Outside Development, `McpTestServer__MCP_PAT` must be set.

Other production settings:

| Variable | Default | Purpose |
|----------|---------|---------|
| `MCP_TEST_SERVER_TRUST_PROXY_FORWARDED_FOR` | `false` | Use the client IP from the last `X-Forwarded-For` hop (for per-IP rate limits). Enable only when the app is reachable exclusively through a reverse proxy, as on Fly.io. |
| `MCP_TEST_SERVER_OBSERVATION_RETENTION_HOURS` | `168` | Delete observations older than this. `0` disables age-based pruning. |
| `MCP_TEST_SERVER_OBSERVATION_MAX_ROWS` | `100000` | Keep at most this many observations. `0` disables the cap. |
| `MCP_TEST_SERVER_CANONICAL_ORIGIN` / `MCP_TEST_SERVER_LEGACY_HOSTS` | unset | Redirect requests on legacy hostnames to the canonical origin. |

`fly.toml` and `.github/workflows/mcp-test-server-fly.yaml` are the configuration for the hosted instance on Fly.io. The deploy workflow only runs in the upstream repository; forks need their own `fly.toml` values and a `MCP_TEST_SERVER_FLY_API_TOKEN` secret.

## Security

See [SECURITY.md](SECURITY.md) to report a vulnerability.

## Project layout

```
.
├── src/McpTestServer.API/            # Host: MCP, management API, scenarios
├── src/McpTestServer.Infrastructure/ # EF entities, DbContext
├── src/McpTestServer.Migrations.PostgreSql/
├── client/                           # React + Vite admin UI
├── tests/McpTestServer.API.Tests/
└── docker-compose.yml
```

## License

[MIT](LICENSE)
