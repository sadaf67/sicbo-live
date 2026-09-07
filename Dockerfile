# ─── Stage 1: Client build (Vite → wwwroot) ────────────────────────────────
FROM node:20-alpine AS client-build
WORKDIR /src
COPY client/package*.json client/
RUN cd client && npm ci
COPY client client
# vite.config.ts outDir = '../src/Presentation/SicBoLive.WebApi/wwwroot' (relative to client/)
# so this writes into /src/src/Presentation/SicBoLive.WebApi/wwwroot inside this stage.
RUN mkdir -p src/Presentation/SicBoLive.WebApi/wwwroot && cd client && npm run build

# ─── Stage 2: .NET build/publish ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/Core/SicBoLive.Domain/SicBoLive.Domain.csproj src/Core/SicBoLive.Domain/
COPY src/Core/SicBoLive.Application/SicBoLive.Application.csproj src/Core/SicBoLive.Application/
COPY src/Infrastructure/SicBoLive.Infrastructure/SicBoLive.Infrastructure.csproj src/Infrastructure/SicBoLive.Infrastructure/
COPY src/Presentation/SicBoLive.WebApi/SicBoLive.WebApi.csproj src/Presentation/SicBoLive.WebApi/
RUN dotnet restore src/Presentation/SicBoLive.WebApi/SicBoLive.WebApi.csproj

COPY src/Core/SicBoLive.Domain src/Core/SicBoLive.Domain
COPY src/Core/SicBoLive.Application src/Core/SicBoLive.Application
COPY src/Infrastructure/SicBoLive.Infrastructure src/Infrastructure/SicBoLive.Infrastructure
COPY src/Presentation/SicBoLive.WebApi src/Presentation/SicBoLive.WebApi
COPY --from=client-build /src/src/Presentation/SicBoLive.WebApi/wwwroot src/Presentation/SicBoLive.WebApi/wwwroot

RUN dotnet publish src/Presentation/SicBoLive.WebApi/SicBoLive.WebApi.csproj -c Release -o /app/publish --no-restore

# ─── Stage 3: Runtime ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# SQLite DB lives in /app/data - persisted via a named volume so data survives container
# rebuilds/redeploys. Auth is stateless JWT, so there's no DataProtection-Keys volume to persist.
RUN mkdir -p /app/data && chown -R $APP_UID:$APP_UID /app

USER $APP_UID

EXPOSE 8080

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__Default="Data Source=/app/data/SicBoLive.db"

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/ || exit 1

ENTRYPOINT ["dotnet", "SicBoLive.WebApi.dll"]
