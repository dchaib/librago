# Librago

Librago is a self-hosted web application for managing library accounts across multiple library networks.

It aims to provide a single, family-oriented view of library activity so that loans, deadlines, reservations, and availability do not have to be checked across several separate accounts and portals.

## Goals

Librago is intended to help with:

- consolidated loan tracking;
- due-date awareness;
- reservation tracking;
- library opening hours;
- renewals;
- federated catalog search;
- reservations;
- preparing a library visit;
- series and next-volume assistance.

The project is designed to support multiple library networks through network-specific connectors while keeping the core domain generic.

## Project status

Version 0.1 is under active development. Its first slice consolidates current loans from the Nantes and Nozay library networks.

See [`docs/README.md`](docs/README.md) for an overview of the project documentation.

## Development

Librago requires the .NET 10 SDK. The Nozay connector also requires a Playwright Chromium installation.

```powershell
Copy-Item src/Librago/appsettings.Local.example.json src/Librago/appsettings.Local.json
dotnet restore Librago.slnx
dotnet build Librago.slnx
pwsh src/Librago/bin/Debug/net10.0/playwright.ps1 install chromium
dotnet run --project src/Librago
```

Edit the ignored `src/Librago/appsettings.Local.json` file with local account configuration before starting the application. Never commit this file or paste its contents into an issue.

This file is intentionally excluded from build and container artifacts. Supply it as a read-only external configuration file when deploying the container.

Each entry under `Librago:Accounts` has a stable local `AccountId`, a `Network` (`Nantes` or `Nozay`), optional borrower display name, and the library credentials. `AccountId` is not a library-issued identifier; choose an opaque local value such as `nantes-reader-a`.

For a family Nozay account, configure that parent account only. Its optional `BorrowerAliases` object maps the borrower names displayed by Nozay to the display names used by Librago. Matching ignores case and repeated whitespace; an unmatched name stays unchanged. This prevents duplicate borrowers when another network uses a shorter configured display name.

At startup, the application synchronizes when there is no previous attempt or when the previous attempt is due, then every six hours by default. The interval can be changed with `Librago:SynchronizationInterval` using standard ASP.NET Core configuration.

Run the automated checks with:

```powershell
dotnet format Librago.slnx --verify-no-changes
dotnet test Librago.slnx
```

## Container

For a local container deployment, fill in `src/Librago/appsettings.Local.json`, then run:

```text
docker compose up --build -d
```

The web interface is exposed on port 8080. The container layout separates immutable application files (`/app`), read-only external configuration (`/config`), persistent SQLite data (`/data` in the mounted volume), and ephemeral state (`/tmp`).

The container starts Xvfb so that connectors requiring a visible Chromium session can run without a desktop environment.

The application runs as the `app` user provided by the .NET base image (UID/GID `1654:1654`). A new named volume mounted at `/data` is prepared for this user. The image does not change mounted file ownership at startup.

If you override the container user or bind-mount `/data`, arrange write access to the mounted directory and its files for the chosen UID/GID before starting Librago. The external configuration must also be readable by the chosen user.
