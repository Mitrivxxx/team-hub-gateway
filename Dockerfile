FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY services/team-hub-gateway/team-hub-gateway/team-hub-gateway.csproj team-hub-gateway/
RUN dotnet restore team-hub-gateway/team-hub-gateway.csproj

COPY services/team-hub-gateway/team-hub-gateway/ team-hub-gateway/
RUN dotnet publish team-hub-gateway/team-hub-gateway.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends openssl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app/publish .
COPY scripts/generate-certs.sh /generate-certs.sh
COPY services/team-hub-gateway/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /generate-certs.sh /docker-entrypoint.sh

ENV ASPNETCORE_URLS=https://+:8080
EXPOSE 8080

ENTRYPOINT ["/docker-entrypoint.sh"]
