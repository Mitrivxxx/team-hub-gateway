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
- Keep routing `/api/graphql/{**catch-all}` to `bff-cluster`.
- Keep destinations per environment from config.
- In dev (manual), keep gateway on `http://localhost:5000`, auth destination on `http://localhost:5001/`, team destination on `http://localhost:5002/`, bff destination on `http://localhost:5003/`.
- In dev (Aspire), AppHost overrides auth destination to `http://team-hub-auth`, team destination to `http://team-hub-organization`, bff destination to `http://team-hub-bff` (YARP service discovery via `Microsoft.Extensions.ServiceDiscovery.Yarp`).
- In docker (Production), map gateway to host `5000` (`team-hub-gateway-prod`, HTTP only, debug); bff cluster -> `http://bff:8080/`.
- TLS terminates at infrastructure nginx; gateway listens on HTTP only.
- Use `ForwardedHeaders` (`X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host`) before other middleware.
- Keep global `UseExceptionHandler` returning JSON `{"error":"Internal Server Error"}` without stack trace.
- Keep CORS on gateway with origins from config (`Cors:AllowedOrigins`).
- Keep rate limiting per IP (`RateLimiting:*`) at gateway entry.
- Keep JWT auth validation configurable (`AuthValidation:*`, `Jwt:*`); disabled by default.
- Keep healthcheck at `/health` as minimal API with Swagger summary; log only unhealthy results.
- Exclude `/health` and `/metrics` from Serilog request logging (`UseSerilogRequestLoggingExcludingHealth`).
- Observability via `TeamHub.Observability`: Serilog (console + OTLP prod), metrics (`/metrics`), OpenTelemetry traces (OTLP).
- Keep `CorrelationIdMiddleware` before `UseSerilogRequestLogging` (`X-Correlation-ID` = OpenTelemetry `TraceId`).
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
