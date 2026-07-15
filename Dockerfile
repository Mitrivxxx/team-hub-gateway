FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY services/team-hub-gateway/team-hub-gateway/team-hub-gateway.csproj team-hub-gateway/
RUN dotnet restore team-hub-gateway/team-hub-gateway.csproj

COPY services/team-hub-gateway/team-hub-gateway/ team-hub-gateway/
RUN dotnet publish team-hub-gateway/team-hub-gateway.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "team-hub-gateway.dll"]
