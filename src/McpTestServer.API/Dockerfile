FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
USER app
WORKDIR /app
EXPOSE 8080

FROM node:22-alpine AS client-build
WORKDIR /client
COPY client/package.json client/package-lock.json ./
RUN npm ci
COPY client/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/McpTestServer.API/McpTestServer.API.csproj", "src/McpTestServer.API/"]
COPY ["src/McpTestServer.API/packages.lock.json", "src/McpTestServer.API/"]
COPY ["src/McpTestServer.Infrastructure/McpTestServer.Infrastructure.csproj", "src/McpTestServer.Infrastructure/"]
COPY ["src/McpTestServer.Migrations.PostgreSql/McpTestServer.Migrations.PostgreSql.csproj", "src/McpTestServer.Migrations.PostgreSql/"]
RUN dotnet restore "src/McpTestServer.API/McpTestServer.API.csproj"
COPY src/ src/
COPY --from=client-build /client/dist src/McpTestServer.API/wwwroot/
WORKDIR /src/src/McpTestServer.API
RUN dotnet publish "McpTestServer.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
USER root
RUN mkdir -p /app/data-protection-keys
COPY --from=publish /app/publish .
COPY src/McpTestServer.API/docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh
ENTRYPOINT ["/app/docker-entrypoint.sh"]
