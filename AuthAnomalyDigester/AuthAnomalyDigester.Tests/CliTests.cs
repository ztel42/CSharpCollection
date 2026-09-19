using System.Text.Json;

namespace AuthAnomalyDigester.Tests;

public class CliTests
{
    [Fact]
    public void RunDigest_Linux()
    {
        var report = Digester.Run(
            new[] { FixturePaths.AuthLog },
            baseline: FixturePaths.BaselineIps,
            bfThreshold: 10,
            bfWindow: 10);
        Assert.True(report.FailureCount >= 11);
        Assert.True(report.SuccessCount >= 3);
        Assert.Contains(report.Findings, f => f.Detector == "brute_force");
    }

    [Fact]
    public void WriteJsonAndCsv()
    {
        var report = Digester.Run(new[] { FixturePaths.AuthLog });
        var tmp = Path.Combine(Path.GetTempPath(), "aad-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            var jsonPath = Path.Combine(tmp, "out.json");
            var csvPath = Path.Combine(tmp, "out.csv");
            ReportWriter.WriteJson(report, jsonPath);
            ReportWriter.WriteCsv(report, csvPath);

            var data = JsonDocument.Parse(File.ReadAllText(jsonPath));
            Assert.True(data.RootElement.TryGetProperty("findings", out _));
            Assert.Contains("READ-ONLY", data.RootElement.GetProperty("banner").GetString());
            Assert.True(File.Exists(csvPath));
            var header = File.ReadAllLines(csvPath)[0];
            Assert.Contains("detector", header);
        }
        finally
        {
            Directory.Delete(tmp, true);
        }
    }

    [Fact]
    public void BannerConstant()
    {
        Assert.Contains("AUTHORIZED USE ONLY", ReportWriter.Banner);
        Assert.Contains("READ-ONLY", ReportWriter.Banner);
    }

    [Fact]
    public void Digester_WindowsCsv()
    {
        var report = Digester.Run(
            new[] { FixturePaths.WindowsCsv },
            formatHint: "windows");
        Assert.True(report.FailureCount >= 11);
        Assert.Contains(report.Findings, f => f.Detector == "brute_force");
    }
}
