using DigitalPreFlightChecklist;

namespace DigitalPreFlightChecklist.Cli;

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
            Console.WriteLine($"DigitalPreFlightChecklist {Version}");
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        if (command is not ("complete" or "save"))
        {
            Console.Error.WriteLine($"ERROR: unknown command '{args[0]}'. Use complete.");
            return 2;
        }

        string? logsDir = null;
        var aircraft = "";
        var site = "";
        var notes = "";
        var checkedItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            for (var i = 1; i < args.Length; i++)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "--logs":
                        logsDir = NeedValue(args, ref i, arg);
                        break;
                    case "--aircraft":
                        aircraft = NeedValue(args, ref i, arg) ?? "";
                        break;
                    case "--site":
                        site = NeedValue(args, ref i, arg) ?? "";
                        break;
                    case "--notes":
                        notes = NeedValue(args, ref i, arg) ?? "";
                        break;
                    case "--all":
                        foreach (var item in ChecklistItems.Required)
                        {
                            checkedItems.Add(item);
                        }
                        break;
                    case "--item":
                        var name = NeedValue(args, ref i, arg);
                        if (name is null)
                        {
                            break;
                        }
                        var match = ChecklistItems.Required.FirstOrDefault(r =>
                            string.Equals(r, name, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(Slug(r), Slug(name), StringComparison.OrdinalIgnoreCase));
                        if (match is null)
                        {
                            Console.Error.WriteLine($"ERROR: unknown item '{name}'. Expected: {string.Join(", ", ChecklistItems.Required.Select(Slug))}");
                            return 2;
                        }
                        checkedItems.Add(match);
                        break;
                    default:
                        Console.Error.WriteLine($"ERROR: unknown option: {arg}");
                        return 2;
                }
            }

            var draft = new ChecklistDraft
            {
                Aircraft = aircraft,
                Site = site,
                Notes = notes
            };
            foreach (var item in ChecklistItems.Required)
            {
                draft.Checked[item] = checkedItems.Contains(item);
            }

            var service = new ChecklistService();
            var completion = service.Complete(draft, logsDir);
            Console.WriteLine($"Completed {completion.CompletionId}");
            Console.WriteLine($"  UTC: {completion.CompletedAtUtc:o}");
            Console.WriteLine($"  ET:  {completion.CompletedAtEastern}");
            Console.WriteLine($"  Log: {completion.LogPath}");
            if (!string.IsNullOrWhiteSpace(completion.Aircraft))
            {
                Console.WriteLine($"  Aircraft: {completion.Aircraft}");
            }
            if (!string.IsNullOrWhiteSpace(completion.Site))
            {
                Console.WriteLine($"  Site: {completion.Site}");
            }
            return 0;
        }
        catch (IncompleteChecklistException ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 1;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 2;
        }
    }

    private static string Slug(string name) =>
        name.Replace(' ', '-').ToLowerInvariant();

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
            DigitalPreFlightChecklist {Version}
            Digital pre-flight checklist with timestamped completion logs.

            The Windows desktop UI is DigitalPreFlightChecklist.App (WPF).
            This console runner completes the same checklist and is what tests call.

            Usage:
              DigitalPreFlightChecklist.Cli complete --all [options]
              DigitalPreFlightChecklist.Cli complete --item props --item firmware ... [options]

            Required items (all five for Complete):
              props, firmware, batteries, laanc-authorization, remote-id

            Options:
              --all                  Check all five required items
              --item NAME            Check one item (repeatable). Slugs or display names OK
              --aircraft TEXT        Aircraft name (optional)
              --site TEXT            Site / location (optional)
              --notes TEXT           Free-form notes (optional)
              --logs DIR             Log directory (default ~/DigitalPreFlightChecklist/logs)
              --version
              -h, --help

            Each successful complete writes a new JSON file and never overwrites prior logs.
            """);
    }
}
