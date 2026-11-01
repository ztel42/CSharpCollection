# ProcessPortSnapshot

One-shot desktop / console inventory of running processes and listening TCP/UDP ports.

Useful for quick host hygiene checks and IR triage: what is listening, and which process owns it.

## Features

- Lists processes (PID, name, path when readable)
- Lists listening endpoints (protocol, address, port, owning PID/name on Linux)
- Console table, JSON, or CSV export
- Works on Linux and Windows (.NET 10)

## Requirements

- .NET 10 SDK

## Build and test

```bash
dotnet test ProcessPortSnapshot.Tests/ProcessPortSnapshot.Tests.csproj
```

## Run

```bash
dotnet run --project ProcessPortSnapshot.csproj
dotnet run --project ProcessPortSnapshot.csproj -- --json --out snapshot.json
dotnet run --project ProcessPortSnapshot.csproj -- --csv --out snapshot.csv
```

On Linux, listener rows include PID/process via `/proc`. On Windows, listeners come from the managed TCP/UDP tables (PID enrichment is best-effort / may be blank without elevated APIs).

## Disclaimer

For systems you own or are authorized to inspect. Not a substitute for EDR.

## Changelog

- **Sun Nov 1, 2026 ET** — Retargeted from .NET 8 to **.NET 10** (`net10.0`) because .NET 8 support ends Nov 10, 2026 and stops getting security patches. Builds Debug + Release on the .NET 10 SDK; no package changes.
