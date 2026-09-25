# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

COPY Directory.Build.props Directory.Packages.props NuGet.Config global.json ./
COPY src/Librago/Librago.csproj src/Librago/packages.lock.json src/Librago/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore src/Librago/Librago.csproj \
    --runtime linux-x64 \
    --property:SelfContained=true \
    --locked-mode

COPY src/Librago/ src/Librago/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/Librago/Librago.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained true \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0 \
    HOME=/tmp \
    PLAYWRIGHT_BROWSERS_PATH=/ms-playwright \
    XDG_CACHE_HOME=/tmp/.cache \
    XDG_CONFIG_HOME=/tmp/.config

COPY --from=build /app/ ./

RUN chmod 755 /app/.playwright/node/linux-x64/node \
    && /app/.playwright/node/linux-x64/node /app/.playwright/package/cli.js install --with-deps --no-shell chromium \
    && command -v Xvfb \
    && apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN install -d -o app -g app -m 0750 /data

COPY --chmod=755 docker-entrypoint.sh /usr/local/bin/librago-entrypoint.sh

USER app
EXPOSE 8080
ENTRYPOINT ["/usr/local/bin/librago-entrypoint.sh"]
CMD ["./Librago"]
