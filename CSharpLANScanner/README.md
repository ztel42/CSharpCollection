# CSharpLANScanner
Basic console command scanner to scan your LAN.  Right now only works on local host and connections directly to your host.

# ⚡ Fancy Network Scanner

A fast, multithreaded console-based network scanner written in C#. Scans a customizable range of IP addresses on your local network to detect live hosts and their hostnames. Outputs results in a clean table format and saves to CSV.

---

## Features

- Scans any user-defined IP range (last octet based)
- Uses asynchronous multithreading for speed
- Resolves hostnames via reverse DNS
- Live console progress bar
- Outputs results as a clean ASCII table
- Handles invalid user input gracefully

---

## Sample Output
⚡ Fancy Network Scanner ⚡

Local IP Address: 192.168.1.23 Base IP for scanning: 192.168.1.0/24

Enter Start IP (last octet, 1-254): 10 Enter End IP (last octet, 1-254): 50

Scanning: [####################-----------------------] 54%

⚡ Fancy Network Scanner ⚡

Active Hosts (10-50):

+-------------------+--------------------------+ | IP Address | Hostname | +-------------------+--------------------------+ | 192.168.1.12 | my-laptop.local | | 192.168.1.23 | Brother-Printer.local | +-------------------+--------------------------+

## Build Instructions

1. Make sure [.NET 6 or later](https://dotnet.microsoft.com/download) is installed.
2. Clone this repository.
3. Open a terminal in the project directory.
4. Run:

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true

Future Enhancements
Allow full IP range input (Start IP: 192.168.0.10, End IP: 192.168.1.100)

Include basic TCP port scanning (e.g., test if ports like 80/443 are open)

Optionally export to JSON or HTML

GUI version (WinForms/WPF)

Add command-line arguments for automation
