## Purpose
- Short context for the gateway service agent.

## Source of truth
- `team-hub-gateway/Program.cs`
- `team-hub-gateway/Configuration/*.cs`
- `team-hub-gateway/reverseproxy.json`
- `team-hub-gateway/appsettings.*.json`
- `team-hub-gateway/Properties/launchSettings.json`
- `team-hub-gateway/.env` / `.env.example` (JWT secrets; same values as auth when AuthValidation is enabled)
- `team-hub-gateway.Test/*.cs`

## Do
- Keep routing `/api/auth/{**catch-all}` to `auth-cluster`.
- Keep dev/prod destination differences from config.
- In dev, keep gateway on `http://localhost:5000` and auth destination on `http://localhost:5001/`.
- In docker (Production), map gateway to host `5000` (`team-hub-gateway-prod`, HTTP only, debug).
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

## Don't
- Do not add routes without confirmed requirements.
- Do not enforce HTTPS redirection in gateway; nginx handles TLS at the edge.
