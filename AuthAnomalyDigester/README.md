# AuthAnomalyDigester (C#)

**This is the C# version of the original Python app:**
[AuthAnomalyDigester (Python)](https://github.com/ztel42/PythonCollection/tree/main/AuthAnomalyDigester)

Read-only **authentication log anomaly digester** CLI for portfolio / IR hygiene demos. It parses Linux `auth.log` / `secure`-style SSH & sudo lines and exported Windows Security events (4624/4625), then flags likely anomalies: brute-force bursts, odd-hour successes, and new source IPs.

```
========================================================================
  READ-ONLY ANALYSIS OF FILES YOU PROVIDE  |  AUTHORIZED USE ONLY
  No live network attacks. No credential guessing. Portfolio / IR hygiene.
========================================================================
```

**Author:** Zach Telford ([ztel42](https://github.com/ztel42))

---

## Why C# vs Python?

Both tools implement the same detection behavior. Pick the runtime that fits your environment:

| Prefer this C# port when… | Prefer the [Python original](https://github.com/ztel42/PythonCollection/tree/main/AuthAnomalyDigester) when… |
| --- | --- |
| You work primarily on **Windows SOC / IR workstations** or in a **.NET shop** where `dotnet` is already standard | You need **cross-platform scripting** on mixed Linux/macOS/Windows hosts with Python already installed |
| You want a **single self-contained publish** (`dotnet publish -c Release -r win-x64 --self-contained`) without managing a venv or pip | You are doing **rapid log-format experiments** and iterating on parsers in a REPL / notebook |
| You care about a stronger story for **Windows Event Log export workflows** and possible future **Event Log API** integration | Your primary target is **Linux-first** hosts (`auth.log` / `secure`) and lightweight shell pipelines |
| You need **easier packaging / code signing** in enterprise Windows environments (MSI, AppLocker-friendly binaries) | You want **stdlib-only** analysis with minimal toolchain (Python 3.10+) |

Neither version performs live attacks, credential guessing, or host modification. They are read-only analyzers of files you provide.

---

## Disclaimer

- Use only on **log files you own or are explicitly authorized** to analyze.
- This tool is **read-only by design**. It does **not** connect to hosts, attempt logons, or modify logs.
- Findings are heuristic review signals, not a full SIEM or incident-response conclusion.
- Binary `.evtx` is **not** parsed directly — export to CSV or XML text first (Event Viewer, `wevtutil`, or `Get-WinEvent`).

---

## Features

| Detector | Default | What it flags |
| --- | --- | --- |
| **Brute force** | 10 failures / 10 minutes | Many failures from the same source IP **or** against the same account within a sliding window |
| **Odd hours** | outside 07:00–21:00 | Successful logons outside a configurable local-hour window |
| **New source IP** | `--baseline` or first-seen | Successful logon from an IP not in a baseline file; without baseline, each distinct success IP is noted as first-seen-in-file |

### Outputs

- Console summary (banner + counts + top findings)
- `--json PATH` full report
- `--csv PATH` findings table

---

## Formats supported

1. **Linux auth / syslog-style** — SSH failed/accepted password or publickey; sudo failure/success (`auth.log`, `secure`, and similar exports).
2. **Windows Security exports** — CSV and simple XML / EVTX-export text with Event IDs **4624** (success) and **4625** (failure). Prefer CSV/XML fixtures for tests and demos.

Auto-detect with `--format auto` (default), or force `--format linux` / `--format windows`.

---

## Requirements

- .NET 8 SDK

---

## Build and test

```bash
dotnet test AuthAnomalyDigester.sln
```

Tests are offline and use snippets under `AuthAnomalyDigester.Tests/Fixtures/` (SSH failures, sudo, Windows 4625/4624 CSV/XML).

---

## Usage

```bash
# From the project directory
dotnet run --project AuthAnomalyDigester -- path/to/auth.log

# Windows CSV export + reports
dotnet run --project AuthAnomalyDigester -- security_export.csv --format windows \
  --json report.json --csv findings.csv

# Baseline of known-good IPs (one per line)
dotnet run --project AuthAnomalyDigester -- auth.log --baseline known_ips.txt

# Tune detectors
dotnet run --project AuthAnomalyDigester -- auth.log \
  --bf-threshold 10 --bf-window 10 \
  --odd-start 7 --odd-end 21
```

### Fixture demo

```bash
dotnet run --project AuthAnomalyDigester -- \
  AuthAnomalyDigester.Tests/Fixtures/auth.log \
  --baseline AuthAnomalyDigester.Tests/Fixtures/baseline_ips.txt \
  --json /tmp/aad.json --csv /tmp/aad.csv
```

### Self-contained publish (example)

```bash
dotnet publish AuthAnomalyDigester/AuthAnomalyDigester.csproj -c Release \
  -r win-x64 --self-contained true -o ./publish
```

---

## Layout

```
AuthAnomalyDigester/
  AuthAnomalyDigester.sln
  AuthAnomalyDigester/
    Parsers/          # LinuxAuth, WindowsSecurity, LogParser
    Detectors/        # BruteForce, OddHours, NewSource
    Models.cs
    Digester.cs
    ReportWriter.cs
    Program.cs
  AuthAnomalyDigester.Tests/
    Fixtures/
  README.md
```

---

## Parity with Python

Detection defaults, sliding-window brute-force logic, odd-hour window semantics, baseline / first-seen IP rules, console banner, and JSON/CSV report shapes match the Python tool as closely as practical.

---

## License

For portfolio demonstration. Use only with authorized log files.
