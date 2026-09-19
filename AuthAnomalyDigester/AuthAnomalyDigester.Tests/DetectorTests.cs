using AuthAnomalyDigester.Detectors;
using AuthAnomalyDigester.Parsers;

namespace AuthAnomalyDigester.Tests;

public class DetectorTests
{
    [Fact]
    public void BruteForce_FromAuthLog()
    {
        var (events, _) = LogParser.ParseFile(FixturePaths.AuthLog);
        var findings = BruteForceDetector.Detect(events, threshold: 10, windowMinutes: 10);
        Assert.Contains(findings, f => f.Detector == "brute_force" && f.SourceIp == "203.0.113.10");
    }

    [Fact]
    public void BruteForce_FromWindowsCsv()
    {
        var (events, _) = LogParser.ParseFile(FixturePaths.WindowsCsv, formatHint: "windows");
        var findings = BruteForceDetector.Detect(events, threshold: 10, windowMinutes: 10);
        Assert.Contains(findings, f => f.SourceIp == "203.0.113.50");
    }

    [Fact]
    public void OddHours()
    {
        var events = new List<AuthEvent>
        {
            new()
            {
                Timestamp = new DateTime(2026, 9, 18, 2, 15, 0),
                Outcome = "success",
                Source = "linux_auth",
                EventType = "ssh_password",
                Username = "alice",
                SourceIp = "198.51.100.99",
            },
            new()
            {
                Timestamp = new DateTime(2026, 9, 18, 10, 0, 0),
                Outcome = "success",
                Source = "linux_auth",
                EventType = "ssh_password",
                Username = "bob",
                SourceIp = "198.51.100.20",
            },
        };
        var findings = OddHoursDetector.Detect(events, startHour: 7, endHour: 21);
        Assert.Single(findings);
        Assert.Equal("alice", findings[0].Username);
    }

    [Fact]
    public void NewSource_WithBaseline()
    {
        var events = new List<AuthEvent>
        {
            new()
            {
                Timestamp = new DateTime(2026, 9, 18, 10, 0, 0),
                Outcome = "success",
                Source = "linux_auth",
                EventType = "ssh_password",
                Username = "alice",
                SourceIp = "198.51.100.20",
            },
            new()
            {
                Timestamp = new DateTime(2026, 9, 18, 10, 5, 0),
                Outcome = "success",
                Source = "linux_auth",
                EventType = "ssh_password",
                Username = "eve",
                SourceIp = "203.0.113.99",
            },
        };
        var baseline = NewSourceDetector.LoadBaselineIps(FixturePaths.BaselineIps);
        var findings = NewSourceDetector.Detect(events, baseline);
        Assert.Single(findings);
        Assert.Equal("203.0.113.99", findings[0].SourceIp);
        Assert.Equal("medium", findings[0].Severity);
    }

    [Fact]
    public void NewSource_NoBaseline_FirstSeen()
    {
        var events = new List<AuthEvent>
        {
            new()
            {
                Timestamp = new DateTime(2026, 9, 18, 10, 0, 0),
                Outcome = "success",
                Source = "linux_auth",
                EventType = "ssh_password",
                Username = "alice",
                SourceIp = "198.51.100.20",
            },
            new()
            {
                Timestamp = new DateTime(2026, 9, 18, 11, 0, 0),
                Outcome = "success",
                Source = "linux_auth",
                EventType = "ssh_password",
                Username = "alice",
                SourceIp = "198.51.100.20",
            },
        };
        var findings = NewSourceDetector.Detect(events, baseline: null);
        Assert.Single(findings);
        Assert.Equal("low", findings[0].Severity);
    }

    [Fact]
    public void RunDetectors_Integration()
    {
        var (events, _) = LogParser.ParseFile(FixturePaths.AuthLog);
        var findings = DetectorRunner.Run(
            events,
            bfThreshold: 10,
            bfWindowMinutes: 10,
            oddStartHour: 7,
            oddEndHour: 21,
            baselinePath: FixturePaths.BaselineIps);
        var dets = findings.Select(f => f.Detector).ToHashSet();
        Assert.Contains("brute_force", dets);
        Assert.Contains("odd_hours", dets);
        Assert.Contains("new_source_ip", dets);
    }
}
