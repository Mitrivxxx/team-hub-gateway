FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY aspire/TeamHub.ServiceDefaults/TeamHub.ServiceDefaults.csproj aspire/TeamHub.ServiceDefaults/
COPY building-blocks/TeamHub.Observability/TeamHub.Observability.csproj building-blocks/TeamHub.Observability/
COPY services/team-hub-gateway/team-hub-gateway/team-hub-gateway.csproj services/team-hub-gateway/team-hub-gateway/
RUN dotnet restore services/team-hub-gateway/team-hub-gateway/team-hub-gateway.csproj

COPY aspire/TeamHub.ServiceDefaults/ aspire/TeamHub.ServiceDefaults/
COPY building-blocks/TeamHub.Observability/ building-blocks/TeamHub.Observability/
COPY services/team-hub-gateway/team-hub-gateway/ services/team-hub-gateway/team-hub-gateway/
RUN dotnet publish services/team-hub-gateway/team-hub-gateway/team-hub-gateway.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "team-hub-gateway.dll"]
