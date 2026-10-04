using BatchWatermarkExport;

namespace BatchWatermarkExport.Cli;

public static class Program
{
    public const string Version = "0.1.0";

    public static int Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            PrintHelp();
            return args.Length == 0 ? 2 : 0;
        }

        if (args.Contains("--version"))
        {
            Console.WriteLine($"BatchWatermarkExport {Version}");
            return 0;
        }

        string? input = null;
        string? output = null;
        var text = "";
        var positionText = "bottom-right";
        var opacityText = "0.45";
        var fontSizeText = "0";
        var presetText = "youtube";
        var exifText = "strip";

        try
        {
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "--input":
                        input = NeedValue(args, ref i, arg);
                        break;
                    case "--output":
                        output = NeedValue(args, ref i, arg);
                        break;
                    case "--text":
                        text = NeedValue(args, ref i, arg) ?? "";
                        break;
                    case "--position":
                        positionText = NeedValue(args, ref i, arg) ?? positionText;
                        break;
                    case "--opacity":
                        opacityText = NeedValue(args, ref i, arg) ?? opacityText;
                        break;
                    case "--font-size":
                        fontSizeText = NeedValue(args, ref i, arg) ?? fontSizeText;
                        break;
                    case "--preset":
                        presetText = NeedValue(args, ref i, arg) ?? presetText;
                        break;
                    case "--exif":
                        exifText = NeedValue(args, ref i, arg) ?? exifText;
                        break;
                    default:
                        Console.Error.WriteLine($"ERROR: unknown option: {arg}");
                        return 2;
                }
            }

            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
            {
                Console.Error.WriteLine("ERROR: --input and --output are required");
                return 2;
            }

            if (!PlatformPresets.TryParsePosition(positionText, out var position))
            {
                Console.Error.WriteLine($"ERROR: unknown position '{positionText}'");
                return 2;
            }

            if (!float.TryParse(opacityText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var opacity))
            {
                Console.Error.WriteLine($"ERROR: opacity '{opacityText}' is not a number");
                return 2;
            }

            if (!float.TryParse(fontSizeText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fontSize))
            {
                Console.Error.WriteLine($"ERROR: font size '{fontSizeText}' is not a number");
                return 2;
            }

            var presets = new List<PlatformPreset>();
            foreach (var part in presetText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!PlatformPresets.TryParse(part, out var preset))
                {
                    Console.Error.WriteLine($"ERROR: unknown preset '{part}'");
                    return 2;
                }

                presets.Add(preset);
            }

            ExifPolicy exif;
            switch (exifText.Trim().ToLowerInvariant())
            {
                case "keep":
                    exif = ExifPolicy.Keep;
                    break;
                case "strip":
                    exif = ExifPolicy.Strip;
                    break;
                default:
                    Console.Error.WriteLine($"ERROR: --exif must be keep or strip, not '{exifText}'");
                    return 2;
            }

            var result = BatchExporter.Export(new ExportRequest(
                input,
                output,
                new WatermarkOptions(text, position, opacity, fontSize),
                exif,
                presets));

            Console.WriteLine($"Wrote {result.Written.Count}, skipped {result.Skipped.Count}, failed {result.Failed.Count}");
            foreach (var file in result.Written)
            {
                Console.WriteLine($"  {file.Preset} -> {file.OutputPath}");
            }

            foreach (var skip in result.Skipped)
            {
                Console.WriteLine($"  skip {skip}");
            }

            foreach (var fail in result.Failed)
            {
                Console.Error.WriteLine($"  FAIL {fail}");
            }

            return result.Failed.Count == 0 ? 0 : 1;
        }
        catch (Exception ex) when (ex is ArgumentException or DirectoryNotFoundException or IOException)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 2;
        }
    }

    private static string? NeedValue(string[] args, ref int index, string name)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith('-'))
        {
            throw new ArgumentException($"Missing value for {name}");
        }

        index++;
        return args[index];
    }

    private static void PrintHelp()
    {
        Console.WriteLine($"""
            BatchWatermarkExport {Version}
            Folder watermark, platform resize, EXIF keep or strip.

            The Windows desktop UI is BatchWatermarkExport.App (WPF).
            This console runner exercises the same pipeline and is what tests call.

            Usage:
              BatchWatermarkExport.Cli --input DIR --output DIR [options]

            Options:
              --text TEXT            Watermark text. Empty skips the stamp.
              --position POS         top-left, top-right, bottom-left, bottom-right, center
                                     (default bottom-right)
              --opacity 0-1          White text opacity (default 0.45)
              --font-size N          Pixel size. 0 = automatic from the export height
              --preset LIST          Comma-separated: youtube, instagram-square,
                                     instagram-portrait, full-res (default youtube)
              --exif keep|strip      Default strip. Keep copies EXIF ImageSharp can read.
              --version
              -h, --help

            Resize is center cover-crop (uniform scale, no stretch). The watermark
            is drawn after resize. Output files are NAME__PRESET.ext and are never
            written back onto the sources. The output folder must sit outside input.
            Only top-level .jpg, .jpeg, and .png files are exported.
            """);
    }
}
