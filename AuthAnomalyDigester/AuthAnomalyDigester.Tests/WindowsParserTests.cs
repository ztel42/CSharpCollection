using AuthAnomalyDigester.Parsers;

namespace AuthAnomalyDigester.Tests;

public class WindowsParserTests
{
    [Fact]
    public void Parse_WindowsCsv_4624_4625()
    {
        var text = File.ReadAllText(FixturePaths.WindowsCsv);
        var events = WindowsSecurityParser.ParseCsv(text);
        var fails = events.Where(e => e.Outcome == "failure").ToList();
        var oks = events.Where(e => e.Outcome == "success").ToList();
        Assert.Equal(11, fails.Count);
        Assert.All(fails, e => Assert.Equal("4625", e.Extras["event_id"]));
        Assert.Equal(3, oks.Count);
        Assert.Contains(oks, e => e.Username == "alice" && e.SourceIp == "198.51.100.20");
    }

    [Fact]
    public void Parse_WindowsXml()
    {
        var text = File.ReadAllText(FixturePaths.WindowsXml);
        var events = WindowsSecurityParser.ParseXml(text);
        Assert.Contains(events, e => e.Outcome == "failure" && e.SourceIp == "203.0.113.50");
        Assert.Contains(events, e => e.Outcome == "success" && e.Username == "alice");
    }

    [Fact]
    public void DetectFormat_Windows()
    {
        var text = File.ReadAllText(FixturePaths.WindowsCsv);
        Assert.Equal("windows", LogParser.DetectFormat(text, FixturePaths.WindowsCsv));
    }

    [Fact]
    public void ParseFile_Windows()
    {
        var (events, fmt) = LogParser.ParseFile(FixturePaths.WindowsCsv);
        Assert.Equal("windows", fmt);
        Assert.True(events.Count >= 14);
    }

    [Fact]
    public void Dispatch_Security()
    {
        Assert.True(WindowsSecurityParser.Parse(File.ReadAllText(FixturePaths.WindowsCsv)).Count >= 14);
        Assert.True(WindowsSecurityParser.Parse(File.ReadAllText(FixturePaths.WindowsXml)).Count >= 2);
    }
}
