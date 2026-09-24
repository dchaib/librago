# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

COPY Directory.Build.props Directory.Packages.props NuGet.Config global.json ./
COPY src/Librago/Librago.csproj src/Librago/packages.lock.json src/Librago/
RUN dotnet restore src/Librago/Librago.csproj --runtime linux-x64 --locked-mode

COPY src/Librago/ src/Librago/
RUN dotnet publish src/Librago/Librago.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained true \
    --no-restore \
    --output /app
RUN chmod 755 /app/.playwright/node/linux-x64/node

FROM mcr.microsoft.com/playwright/dotnet:v1.63.0-noble AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0 \
    HOME=/tmp \
    PLAYWRIGHT_BROWSERS_PATH=/ms-playwright \
    XDG_CACHE_HOME=/tmp/.cache \
    XDG_CONFIG_HOME=/tmp/.config

RUN install -d -m 1777 /data

COPY --from=build /app/ ./
COPY --chmod=755 docker-entrypoint.sh /usr/local/bin/librago-entrypoint.sh

USER pwuser
EXPOSE 8080
ENTRYPOINT ["/usr/local/bin/librago-entrypoint.sh"]
CMD ["./Librago"]
