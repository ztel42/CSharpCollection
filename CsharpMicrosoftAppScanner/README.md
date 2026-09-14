A simple C# command-line tool that scans your local machine's C:\ drive to find installed Microsoft applications, including:

Microsoft Word

Microsoft PowerPoint

Microsoft Excel

Microsoft Access

Microsoft Power Apps

Microsoft Teams

Microsoft Power BI Desktop

Microsoft Edge

The application reports the file path and version number for each application found.

Features
Recursively scans the C:\ drive.

Identifies and locates common Microsoft applications.

Displays application version information.

Gracefully handles access permission issues.

Easy to customize or extend to other applications.

Prerequisites
Windows OS

.NET 6.0 SDK or later

Visual Studio 2022 or any other C# IDE (optional)

How to Build and Run
Clone or download the repository.

Open the solution in Visual Studio or your preferred C# editor.

Build the project.

Run the application.

Alternatively, using command-line:

bash
Copy
Edit
dotnet build
dotnet run
Application Logic
Recursive Search: The tool searches through the C:\ drive recursively.

File Identification: It looks for specific executable names (WINWORD.EXE, EXCEL.EXE, Teams.exe, etc.).

Version Extraction: If an executable is found, it retrieves its version using FileVersionInfo.

Error Handling: Access to protected system directories is skipped silently.

Example Output
mathematica
Copy
Edit
Starting scan for Microsoft applications...

Scanning for Microsoft Word...
Scanning for Microsoft PowerPoint...
Scanning for Microsoft Excel...
Scanning for Microsoft Access...
Scanning for Microsoft Power Apps...
Scanning for Microsoft Teams...
Scanning for Microsoft Power BI Desktop...
Scanning for Microsoft Edge...

--- Scan Results ---
Microsoft Word found at C:\Program Files\Microsoft Office\root\Office16\WINWORD.EXE (Version: 16.0.16924.20124)
Microsoft PowerPoint found at C:\Program Files\Microsoft Office\root\Office16\POWERPNT.EXE (Version: 16.0.16924.20124)
Microsoft Excel found at C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE (Version: 16.0.16924.20124)
Microsoft Access found at C:\Program Files\Microsoft Office\root\Office16\MSACCESS.EXE (Version: 16.0.16924.20124)
Microsoft Power Apps not found.
Microsoft Teams found at C:\Users\YourName\AppData\Local\Microsoft\Teams\current\Teams.exe (Version: 1.6.00.12345)
Microsoft Power BI Desktop found at C:\Program Files\Microsoft Power BI Desktop\bin\PBIDesktop.exe (Version: 2.123.4567.0)
Microsoft Edge found at C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe (Version: 123.0.2420.81)

Scan completed. Press any key to exit.
Customization
To add more applications:

Open Program.cs.

Add a new entry to the appsToFind dictionary:

csharp
Copy
Edit
{ "App Name", "AppExecutable.exe" }
Notes
Scanning the entire C:\ drive may take several minutes depending on disk size and number of files.

Power Apps is primarily a web application; a desktop version may not be installed.

Some applications may be installed per-user (in AppData) rather than system-wide.

License
This project is licensed under the MIT License.

