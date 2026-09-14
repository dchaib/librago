# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

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

FROM mcr.microsoft.com/playwright/dotnet:v1.62.0-noble AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0 \
    HOME=/tmp \
    PLAYWRIGHT_BROWSERS_PATH=/ms-playwright \
    XDG_CACHE_HOME=/tmp/.cache \
    XDG_CONFIG_HOME=/tmp/.config

COPY --from=build --chown=pwuser:pwuser /app/ ./
COPY docker-entrypoint.sh /usr/local/bin/librago-entrypoint.sh
RUN chmod +x /usr/local/bin/librago-entrypoint.sh \
    && mkdir -p /app/storage \
    && chown pwuser:pwuser /app/storage

USER pwuser
EXPOSE 8080
ENTRYPOINT ["/usr/local/bin/librago-entrypoint.sh"]
CMD ["./Librago"]
