namespace AuthAnomalyDigester;

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
            Console.WriteLine($"AuthAnomalyDigester {Version}");
            return 0;
        }

        string? format = "auto";
        string? jsonPath = null;
        string? csvPath = null;
        string? baseline = null;
        var bfThreshold = 10;
        var bfWindow = 10;
        var oddStart = 7;
        var oddEnd = 21;
        var paths = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "--format":
                    format = NeedValue(args, ref i, a);
                    break;
                case "--json":
                    jsonPath = NeedValue(args, ref i, a);
                    break;
                case "--csv":
                    csvPath = NeedValue(args, ref i, a);
                    break;
                case "--baseline":
                    baseline = NeedValue(args, ref i, a);
                    break;
                case "--bf-threshold":
                    bfThreshold = int.Parse(NeedValue(args, ref i, a)!);
                    break;
                case "--bf-window":
                    bfWindow = int.Parse(NeedValue(args, ref i, a)!);
                    break;
                case "--odd-start":
                    oddStart = int.Parse(NeedValue(args, ref i, a)!);
                    break;
                case "--odd-end":
                    oddEnd = int.Parse(NeedValue(args, ref i, a)!);
                    break;
                case "--version":
                case "-h":
                case "--help":
                    break;
                default:
                    if (a.StartsWith('-'))
                    {
                        Console.Error.WriteLine($"ERROR: unknown option: {a}");
                        return 2;
                    }
                    paths.Add(a);
                    break;
            }
        }

        ReportWriter.PrintBanner();

        if (paths.Count == 0)
        {
            Console.Error.WriteLine("ERROR: at least one LOG path is required");
            return 2;
        }

        var missing = paths.Where(p => !File.Exists(p)).ToList();
        if (missing.Count > 0)
        {
            foreach (var p in missing)
                Console.Error.WriteLine($"ERROR: file not found: {p}");
            return 2;
        }

        if (baseline is not null && !File.Exists(baseline))
        {
            Console.Error.WriteLine($"ERROR: baseline not found: {baseline}");
            return 2;
        }

        if (oddStart is < 0 or > 23 || oddEnd is < 0 or > 23)
        {
            Console.Error.WriteLine("ERROR: --odd-start/--odd-end must be in 0..23");
            return 2;
        }

        if (format is not ("auto" or "linux" or "windows"))
        {
            Console.Error.WriteLine("ERROR: --format must be auto, linux, or windows");
            return 2;
        }

        var report = Digester.Run(
            paths,
            formatHint: format,
            baseline: baseline,
            bfThreshold: bfThreshold,
            bfWindow: bfWindow,
            oddStart: oddStart,
            oddEnd: oddEnd);

        ReportWriter.PrintSummary(report);

        if (jsonPath is not null)
        {
            ReportWriter.WriteJson(report, jsonPath);
            Console.WriteLine($"JSON report written: {jsonPath}");
        }
        if (csvPath is not null)
        {
            ReportWriter.WriteCsv(report, csvPath);
            Console.WriteLine($"CSV report written: {csvPath}");
        }

        return 0;
    }

    private static string? NeedValue(string[] args, ref int i, string opt)
    {
        if (i + 1 >= args.Length)
        {
            Console.Error.WriteLine($"ERROR: missing value for {opt}");
            Environment.Exit(2);
        }
        return args[++i];
    }

    private static void PrintHelp()
    {
        Console.WriteLine(
@"AuthAnomalyDigester — read-only auth log anomaly digester (C# port)

Usage:
  AuthAnomalyDigester LOG [LOG...] [options]

Options:
  --format auto|linux|windows   Input format (default: auto)
  --json PATH                   Write full JSON report
  --csv PATH                    Write findings CSV report
  --baseline PATH               Known-good source IPs (one per line)
  --bf-threshold N              Brute-force failure threshold (default: 10)
  --bf-window MINUTES           Brute-force window minutes (default: 10)
  --odd-start HOUR              Normal hours start [0-23] (default: 7)
  --odd-end HOUR                Normal hours end exclusive [0-23] (default: 21)
  --version                     Show version
  -h, --help                    Show this help

Authorized use only. Analyzes files you provide; no live attacks.
Binary .evtx is not parsed — export to CSV/XML text first.");
    }
}
