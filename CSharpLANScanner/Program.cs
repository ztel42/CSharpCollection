using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

class Program
{
    class HostEntry
    {
        public string IPAddress { get; set; }
        public string HostName { get; set; }
    }

    static int scanned = 0; // For progress tracking
    static int totalIPs = 254;

    static async Task Main(string[] args)
    {
        Console.WriteLine("⚡ Fancy Network Scanner ⚡\n");

        string localIP = GetLocalIPAddress();
        if (localIP == null)
        {
            Console.WriteLine("Could not determine local IP Address.");
            return;
        }

        Console.WriteLine($"Local IP Address: {localIP}");

        string baseIP = GetBaseIP(localIP);
        int startIP = GetValidatedOctet("Enter Start IP (last octet, 1-254): ");
        int endIP;

        while (true)
        {
            endIP = GetValidatedOctet("Enter End IP (last octet, 1-254): ");
            if (endIP < startIP)
            {
                Console.WriteLine("End IP must be greater than or equal to Start IP. Try again.");
                continue;
            }
            break;
        }

        totalIPs = endIP - startIP + 1;
        scanned = 0;

        ConcurrentBag<HostEntry> activeHosts = new ConcurrentBag<HostEntry>();
        List<Task> tasks = new List<Task>();

        for (int i = startIP; i <= endIP; i++)
        {
            string ip = $"{baseIP}{i}";
            tasks.Add(Task.Run(async () =>
            {
                if (await IsHostAlive(ip))
                {
                    string hostName = GetHostName(ip);
                    activeHosts.Add(new HostEntry
                    {
                        IPAddress = ip,
                        HostName = hostName ?? "Unknown"
                    });
                }
                Interlocked.Increment(ref scanned);
            }));
        }

        Task progressTask = Task.Run(() => ShowProgress());

        await Task.WhenAll(tasks);
        await progressTask;

        Console.Clear();
        Console.WriteLine("⚡ Fancy Network Scanner ⚡\n");
        Console.WriteLine($"Active Hosts ({startIP}-{endIP}):\n");

        var sortedHosts = activeHosts.OrderBy(h => IPToLong(h.IPAddress)).ToList();
        DisplayTable(sortedHosts);

    }


        // Progress bar updater
     

    static async Task<bool> IsHostAlive(string ip)
    {
        using (Ping ping = new Ping())
        {
            try
            {
                PingReply reply = await ping.SendPingAsync(ip, 100);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }
    }

    static string GetHostName(string ip)
    {
        try
        {
            IPHostEntry entry = Dns.GetHostEntry(ip);
            return entry.HostName;
        }
        catch (SocketException)
        {
            return null; // Hostname not found
        }
    }

    static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return null;
    }

    static string GetBaseIP(string ip)
    {
        var parts = ip.Split('.');
        return $"{parts[0]}.{parts[1]}.{parts[2]}.";
    }

    static void ShowProgress()
    {
        while (scanned < totalIPs)
        {
            Console.CursorVisible = false;
            Console.SetCursorPosition(0, Console.CursorTop);

            int progress = (scanned * 100) / totalIPs;
            Console.Write($"Scanning: [{new string('#', progress / 2)}{new string('-', 50 - (progress / 2))}] {progress}%");

            Thread.Sleep(100);
        }
    }

    static void DisplayTable(IEnumerable<HostEntry> hosts)
    {
        var sortedHosts = hosts.OrderBy(h => IPToLong(h.IPAddress)).ToList();

        Console.WriteLine("+-------------------+------------------------------------------+");
        Console.WriteLine("| IP Address        | Hostname                                 |");
        Console.WriteLine("+-------------------+------------------------------------------+");

        foreach (var host in sortedHosts)
        {
            Console.ForegroundColor = host.HostName == "Unknown" ? ConsoleColor.Yellow : ConsoleColor.Green;
            Console.WriteLine($"| {host.IPAddress,-17} | {host.HostName,-40} |");
        }

        Console.ResetColor();
        Console.WriteLine("+-------------------+------------------------------------------+");
    }

    static long IPToLong(string ipAddress)
    {
        string[] octets = ipAddress.Split('.');
        return (long)(int.Parse(octets[0]) << 24) + (long)(int.Parse(octets[1]) << 16) +
               (long)(int.Parse(octets[2]) << 8) + (long)(int.Parse(octets[3]));
    }

    static int GetValidatedOctet(string prompt)
    {
        int value;
        while (true)
        {
            Console.Write(prompt);
            string input = Console.ReadLine();

            if (int.TryParse(input, out value) && value >= 1 && value <= 254)
            {
                return value;
            }
            Console.WriteLine("❌ Invalid input. Please enter a number between 1 and 254.");
        }
    }


}