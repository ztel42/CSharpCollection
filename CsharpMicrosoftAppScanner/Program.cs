using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace AppScanner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting scan for Microsoft applications...\n");

            // List of application executable names to search for
            Dictionary<string, string> appsToFind = new Dictionary<string, string>
            {
                { "Microsoft Word", "WINWORD.EXE" },
                { "Microsoft PowerPoint", "POWERPNT.EXE" },
                { "Microsoft Excel", "EXCEL.EXE" },
                { "Microsoft Access", "MSACCESS.EXE" },
                { "Microsoft Project", "PROJECT.EXE" },
                { "Microsoft Visio", "VISIO.EXE" },
                { "Microsoft Teams", "msteams.exe" },
                { "Microsoft Edge", "msedge.exe" } 
                // May vary
            };

            List<string> foundApps = new List<string>();

            try
            {
                foreach (var app in appsToFind)
                {
                    Console.WriteLine($"Scanning for {app.Key}...");
                    var foundPath = FindFile("C:\\", app.Value);

                    if (foundPath != null)
                    {
                        string version = GetFileVersion(foundPath);
                        foundApps.Add($"{app.Key} found at {foundPath} (Version: {version})");
                    }
                    else
                    {
                        foundApps.Add($"{app.Key} not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during scan: {ex.Message}");
            }

            Console.WriteLine("\n--- Scan Results ---");
            foreach (var result in foundApps)
            {
                Console.WriteLine(result);
            }

            Console.WriteLine("\nScan completed. Press any key to exit.");
            Console.ReadKey();
        }

        // Recursive file search
        static string FindFile(string rootFolder, string fileName)
        {
            try
            {
                foreach (string file in Directory.EnumerateFiles(rootFolder, fileName, SearchOption.TopDirectoryOnly))
                {
                    return file;
                }

                foreach (string dir in Directory.EnumerateDirectories(rootFolder))
                {
                    string result = FindFile(dir, fileName);
                    if (result != null)
                        return result;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore directories we can't access
            }
            catch (PathTooLongException)
            {
                // Ignore overly long paths
            }
            return null;
        }

        // Get file version info
        static string GetFileVersion(string filePath)
        {
            try
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                return versionInfo.FileVersion ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}