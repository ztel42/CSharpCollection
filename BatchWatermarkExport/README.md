# BatchWatermarkExport

WPF desktop app that watermarks a folder of stills, resizes them for a few platform canvases, and either keeps or strips EXIF. The image pipeline is a .NET 8 class library (SixLabors.ImageSharp) so it can be tested on Linux. A small console runner calls the same pipeline.

**Author:** Zach Telford ([ztel42](https://github.com/ztel42))

---

## What it does

- Reads top-level `.jpg`, `.jpeg`, and `.png` files. Other files are skipped. Subfolders are not scanned.
- Writes `{name}__{preset}{ext}` into an output folder. The output folder must sit **outside** the input folder. Sources are never opened for writing.
- Stamps white text (Liberation Sans) at a corner or the center, with an opacity from 0 to 1.
- Resizes to a preset canvas or leaves the original pixel size.

## Presets

| Preset | Canvas | Slug |
| --- | --- | --- |
| YouTube thumbnail | 1280×720 | `youtube-1280x720` |
| Instagram square | 1080×1080 | `instagram-1080x1080` |
| Instagram portrait | 1080×1350 | `instagram-1080x1350` |
| Full-res portfolio | original pixel size | `full-res` |

### Resize: center cover-crop, never stretch

Platform presets are exact frames. The image is scaled uniformly with Lanczos3 until it **covers** the frame (`ResizeMode.Crop`, anchor center), then the overflow is cropped. Aspect ratio is preserved. There is no stretch and no letterbox. Full-res portfolio does not resize or crop.

### Watermark: after resize

Order for every export:

1. Load.
2. Copy the EXIF profile aside when the choice is **keep**.
3. Auto-orient pixels from the EXIF orientation tag so the export is upright.
4. Cover-crop to the preset, or skip that step for full-res.
5. Draw the watermark. Doing this **after** resize keeps the type size tied to the export (a thumbnail does not inherit a tiny stamp from a large master). Font size `0` picks a size from the export height, clamped between 18 and 96 px.
6. Apply the EXIF choice and save.

Empty watermark text, or opacity 0, skips the stamp and still exports the resize.

### EXIF

- **Strip** (default): removes the EXIF, XMP, and IPTC profiles. An ICC color profile, if present, is left alone. The reloaded output has no EXIF profile. JPEG files can still carry a JFIF header; that is not EXIF.
- **Keep**: writes back the EXIF profile ImageSharp was able to read. Orientation is set to normal (`1`) because the pixels were already oriented. `PixelXDimension` / `PixelYDimension` are updated only when those tags were already present. Tags ImageSharp cannot parse are not invented.

## Projects

| Project | TFM | Role |
| --- | --- | --- |
| `BatchWatermarkExport.Core` | `net8.0` | Pipeline. This is what tests call. |
| `BatchWatermarkExport.Cli` | `net8.0` | Console runner for the same pipeline. |
| `BatchWatermarkExport.App` | `net8.0-windows` (WPF) | Folder pickers, watermark text, opacity, position, presets, keep/strip, export. |
| `BatchWatermarkExport.Tests` | `net8.0` | xUnit. |

The WPF project sets `EnableWindowsTargeting` so the Windows desktop project can compile on a non-Windows .NET 8 SDK. The WinExe still **runs only on Windows**.

## Requirements

- .NET 8 SDK
- Windows to launch the WPF app

The embedded face is the unmodified Liberation Sans Regular file (SIL Open Font License 1.1). See `BatchWatermarkExport.Core/Fonts/OFL-LICENSE.txt`.

## Build and test

```bash
dotnet test BatchWatermarkExport.sln
```

## Desktop app

On Windows:

```bash
dotnet run --project BatchWatermarkExport.App
```

Pick an input folder and an output folder, set the watermark, choose presets and EXIF, then Export.

## Console runner

```bash
dotnet run --project BatchWatermarkExport.Cli -- \
  --input ./stills --output ./export \
  --text "© Portfolio" --position bottom-right --opacity 0.45 \
  --preset youtube,instagram-square,instagram-portrait,full-res \
  --exif strip
```

`--font-size 0` (the default) sizes the stamp from the export height. `--exif keep` copies EXIF ImageSharp supports.

## Layout

```
BatchWatermarkExport/
  BatchWatermarkExport.sln
  BatchWatermarkExport.Core/     # ImageSharp pipeline and embedded font
  BatchWatermarkExport.Cli/      # net8.0 console runner
  BatchWatermarkExport.App/      # WPF UI (net8.0-windows)
  BatchWatermarkExport.Tests/
  README.md
```

## Changelog

- **Sun Oct 4, 2026 ET** — Initial version: folder watermark, YouTube / Instagram / full-res presets (center cover-crop, watermark after resize), and EXIF keep or strip.

## License

Portfolio demonstration. Use it on images you have the rights to export. The bundled font stays under the SIL Open Font License.
