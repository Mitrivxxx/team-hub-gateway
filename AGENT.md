## Purpose
- Short context for the gateway service agent.

## Source of truth
- `team-hub-gateway/Program.cs`
- `team-hub-gateway/Configuration/*.cs`
- `team-hub-gateway/reverseproxy.json`
- `team-hub-gateway/appsettings.*.json`
- `team-hub-gateway/Properties/launchSettings.json`
- `aspire/TeamHub.ServiceDefaults/Extensions.cs`
- `team-hub-gateway/.env` / `.env.example` (JWT secrets; same values as auth when AuthValidation is enabled)
- `team-hub-gateway.Test/*.cs`

## Do
- Keep routing `/api/auth/{**catch-all}` to `auth-cluster`.
- Keep routing `/api/organizations/{**catch-all}` to `team-cluster`.
- Keep routing `/api/notifications/{**catch-all}` to `notification-cluster`.
- Keep routing `/api/chat/{**catch-all}` to `chat-cluster`.
- Keep routing `/api/graphql/{**catch-all}` to `bff-cluster`.
- Keep destinations per environment from config.
- In dev (manual), keep gateway on `http://localhost:5000`, auth destination on `http://localhost:5001/`, team destination on `http://localhost:5002/`, notification destination on `http://localhost:5004/`, chat destination on `http://localhost:5005/`, bff destination on `http://localhost:5003/`.
- In dev (Aspire), AppHost overrides auth destination to `http://srv-auth`, team destination to `http://srv-organization`, notification destination to `http://srv-notification`, chat destination to `http://srv-chat`, bff destination to `http://srv-bff` (YARP service discovery via `Microsoft.Extensions.ServiceDiscovery.Yarp`).
- In docker (Production), map gateway to host `5000` (`gw-api-prod`, HTTP only, debug); bff cluster -> `http://srv-bff:8080/`; notification cluster -> `http://srv-notification:8080/`; chat cluster -> `http://srv-chat:8080/` when that service is deployed.
- TLS terminates at infrastructure nginx; gateway listens on HTTP only.
- Use `ForwardedHeaders` (`X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host`) before other middleware.
- Keep global `UseExceptionHandler` returning JSON `{"error":"Internal Server Error"}` without stack trace.
- Keep CORS on gateway with origins from config (`Cors:AllowedOrigins`).
- Keep rate limiting per IP (`RateLimiting:*`) at gateway entry (`RequireRateLimiting` on YARP + GlobalLimiter for invite/import/avatar paths).
- Sensitive path limits (config): invite create/resend, invite accept, import, avatar PUT.
- Keep JWT auth validation configurable (`AuthValidation:*`, `Jwt:*`); disabled by default.
- Keep healthcheck at `/health` as minimal API with Swagger summary; log only unhealthy results.
- Docker image runs as non-root (`USER app`); Dockerfile + Compose healthcheck hit `/health` (interval `120s`). `gw-nginx` waits for `gw-api` `service_healthy`.
- Exclude `/health` and `/metrics` from Serilog request logging (`UseSerilogRequestLoggingExcludingHealth` from `TeamHub.Observability`).
- Observability via `TeamHub.Observability`: Serilog (console + OTLP), metrics (`/metrics`), OpenTelemetry traces (OTLP), CorrelationId + SessionId + UserIdLogging.
- Keep `UseTeamHubCorrelationId` then `UseTeamHubSessionId` before auth (`X-Correlation-ID` = OpenTelemetry `TraceId`).
- Keep `UseTeamHubUserIdLogging` after the JWT gate so request logs include `UserId` when present.
- Enrich all request logs with Serilog `CorrelationId` via `LogContext`.
- Keep detailed proxy request logs: correlation id, method, path, status, duration, route, cluster, upstream destination.
- Echo `X-Correlation-ID` on every response.
- Keep Swagger UI at `/swagger`.
- Keep `team-hub-gateway.http` without starter artifacts; probe `/health`.
- Keep basic tests in `team-hub-gateway.Test` (XUnit), at least JWT/Auth config validation.
- Update this file after route, cluster, TLS, or port changes.

## CI (GitHub Actions)
- Workflow: `.github/workflows/ci.yml`.
- Branches: `stage` (build + tests), `main` (build + tests + GHCR image push). `dev` has no CI.
- PRs targeting `stage` or `main` run tests before merge.
- Shared deps: dual `actions/checkout` — monorepo `Mitrivxxx/team-hub@main` then overlay this repo at `services/team-hub-gateway`.
- Image: `ghcr.io/<owner>/team-hub-gateway` (`latest` + short commit SHA on `main` push). Uses `GITHUB_TOKEN` (no extra secrets).

## Don't
- Do not add routes without confirmed requirements.
- Do not enforce HTTPS redirection in gateway; nginx handles TLS at the edge.
