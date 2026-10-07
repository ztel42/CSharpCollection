# DigitalPreFlightChecklist

WPF desktop app for a digital drone pre-flight checklist. Checkboxes cover **props**, **firmware**, **batteries**, **LAANC authorization**, and **Remote ID**. Completing the list writes a timestamped JSON log. Core logic is a .NET 8 class library so it can be tested on Linux. A small console runner calls the same service.

**Author:** Zach Telford ([ztel42](https://github.com/ztel42))

---

## What it does

- Five required items must all be checked before **Complete / Save** is enabled (WPF) or before `complete` succeeds (CLI).
- Optional aircraft name, site / location, and free-form notes.
- Each completion writes a **new** JSON file under a logs folder. Prior logs are never overwritten.
- Timestamps are stored as UTC (`completedAtUtc`) and labeled America/New_York Eastern time (`completedAtEastern`).

## Required items

| Item | CLI slug |
| --- | --- |
| Props | `props` |
| Firmware | `firmware` |
| Batteries | `batteries` |
| LAANC authorization | `laanc-authorization` |
| Remote ID | `remote-id` |

## Log format

Files are named `preflight_yyyyMMddTHHmmssZ_<id8>.json`, for example:

```json
{
  "completionId": "a1b2c3d4e5f60718293a4b5c6d7e8f90",
  "completedAtUtc": "2026-10-07T17:40:00.0000000Z",
  "completedAtEastern": "2026-10-07 13:40:00 ET (-04:00)",
  "aircraft": "Mavic 3",
  "site": "Space Coast",
  "notes": "Clear skies",
  "items": {
    "Props": true,
    "Firmware": true,
    "Batteries": true,
    "LAANC authorization": true,
    "Remote ID": true
  }
}
```

Default logs folder: `~/DigitalPreFlightChecklist/logs` (override with `--logs` or by changing the service path).

## Projects

| Project | TFM | Role |
| --- | --- | --- |
| `DigitalPreFlightChecklist.Core` | `net8.0` | Draft validation and log writer. This is what tests call. |
| `DigitalPreFlightChecklist.Cli` | `net8.0` | Console runner for headless complete/save. |
| `DigitalPreFlightChecklist.App` | `net8.0-windows` (WPF) | Checkboxes, notes, aircraft/site, Complete / Save. |
| `DigitalPreFlightChecklist.Tests` | `net8.0` | xUnit. |

The WPF project sets `EnableWindowsTargeting` so the Windows desktop project can compile on a non-Windows .NET 8 SDK. The WinExe still **runs only on Windows**.

## Requirements

- .NET 8 SDK
- Windows to launch the WPF app

## Build and test

```bash
dotnet test DigitalPreFlightChecklist.sln
```

## Desktop app

On Windows:

```bash
dotnet run --project DigitalPreFlightChecklist.App
```

Check all five items, optionally fill aircraft / site / notes, then **Complete / Save**.

## Console runner

```bash
dotnet run --project DigitalPreFlightChecklist.Cli -- \
  complete --all \
  --aircraft "Mavic 3" --site "Field A" --notes "Winds calm" \
  --logs ./logs
```

Or check items individually:

```bash
dotnet run --project DigitalPreFlightChecklist.Cli -- \
  complete \
  --item props --item firmware --item batteries \
  --item laanc-authorization --item remote-id \
  --logs ./logs
```

Incomplete checklists exit with code `1` and write nothing.

## Layout

```
DigitalPreFlightChecklist/
  DigitalPreFlightChecklist.sln
  DigitalPreFlightChecklist.Core/     # Checklist service and log writer
  DigitalPreFlightChecklist.Cli/      # net8.0 console runner
  DigitalPreFlightChecklist.App/      # WPF UI (net8.0-windows)
  DigitalPreFlightChecklist.Tests/
  README.md
```

## Changelog

- **Wed Oct 7, 2026 ET** — Initial version: five-item pre-flight checklist, WPF UI, CLI complete/save, timestamped JSON logs (UTC + Eastern).

## License

Portfolio demonstration. Not a substitute for official FAA / LAANC / manufacturer procedures.
