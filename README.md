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

Each entry under `Librago:Accounts` has a stable local `AccountId`, a `Network` (`Nantes` or `Nozay`), optional borrower display name, and the library credentials. `AccountId` is not a library-issued identifier; choose an opaque local value such as `nantes-reader-a`.

The application synchronizes on first use or when the previous attempt is due, then every six hours by default. The interval can be changed with `Librago:SynchronizationInterval` using standard ASP.NET Core configuration.

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

The web interface is exposed on port 8080. SQLite data and data-protection keys are stored in the `librago-data` Docker volume.

The container starts Xvfb so that connectors requiring a visible Chromium session can run without a desktop environment.
