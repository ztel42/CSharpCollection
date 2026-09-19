using AuthAnomalyDigester.Parsers;

namespace AuthAnomalyDigester.Tests;

public class LinuxParserTests
{
    [Fact]
    public void Parse_SshFailuresAndSuccesses()
    {
        var text = File.ReadAllText(FixturePaths.AuthLog);
        var events = LinuxAuthParser.Parse(text);
        var failures = events.Where(e => e.Outcome == "failure" && e.EventType.StartsWith("ssh")).ToList();
        var successes = events.Where(e => e.Outcome == "success" && e.EventType.StartsWith("ssh")).ToList();
        Assert.True(failures.Count >= 11);
        Assert.Contains(failures, e => e.SourceIp == "203.0.113.10");
        Assert.Contains(successes, e => e.Username == "alice" && e.SourceIp == "198.51.100.20");
        Assert.Contains(successes, e => e.EventType == "ssh_publickey");
    }

    [Fact]
    public void Parse_Sudo()
    {
        var text = File.ReadAllText(FixturePaths.AuthLog);
        var events = LinuxAuthParser.Parse(text);
        var sudo = events.Where(e => e.EventType == "sudo").ToList();
        Assert.Contains(sudo, e => e.Outcome == "failure" && e.Username == "alice");
        Assert.Contains(sudo, e => e.Outcome == "success" && e.Username == "alice");
    }

    [Fact]
    public void DetectFormat_Linux()
    {
        var text = File.ReadAllText(FixturePaths.AuthLog);
        Assert.Equal("linux", LogParser.DetectFormat(text, FixturePaths.AuthLog));
    }

    [Fact]
    public void ParseFile_Linux()
    {
        var (events, fmt) = LogParser.ParseFile(FixturePaths.AuthLog);
        Assert.Equal("linux", fmt);
        Assert.NotEmpty(events);
    }
}
