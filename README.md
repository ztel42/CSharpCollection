# CSharpCollection

Collected C# projects from my portfolio.

| Project | Original repo / notes |
| --- | --- |
| `AuthAnomalyDigester` | C# port of the Python [AuthAnomalyDigester](https://github.com/ztel42/PythonCollection/tree/main/AuthAnomalyDigester) (lives in this collection) |
| `BatchWatermarkExport` | *(new — lives in this collection)* WPF folder watermark, platform resize, EXIF keep/strip |
| `DigitalPreFlightChecklist` | *(new — lives in this collection)* WPF pre-flight checklist (props/firmware/batteries/LAANC/Remote ID) with timestamped logs |
| `C-BasicCalculator` | https://github.com/ztel42/C-BasicCalculator |
| `CSharpLANScanner` | https://github.com/ztel42/CSharpLANScanner |
| `CsharpMicrosoftAppScanner` | https://github.com/ztel42/CsharpMicrosoftAppScanner |
| `PasswordStrengthAnalyzer` | https://github.com/ztel42/PasswordStrengthAnalyzer |
| `ProcessPortSnapshot` | https://github.com/ztel42/ProcessPortSnapshot |

## Changelog

- **Sat Oct 10, 2026 ET** — `BatchWatermarkExport` security hardening: PNG/JPEG-only decoding plus file-size/pixel limits; stays on ImageSharp 3.1.12 (4.x needs a license key). Mitigates GHSA-wmxv-xphr-5c9g and GHSA-gwg2-r3hj-4w44. See that folder’s README.

- **Wed Oct 7, 2026 ET** — Added `DigitalPreFlightChecklist`: props/firmware/batteries/LAANC/Remote ID checklist with timestamped completion logs. See that folder’s README.

- **Sun Oct 4, 2026 ET** — Added `BatchWatermarkExport`: folder watermark, platform resize presets, and EXIF keep/strip. See that folder’s README.
