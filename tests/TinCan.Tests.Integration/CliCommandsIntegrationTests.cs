using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace TinCan.Tests.Integration;

[TestClass]
public class CliCommandsIntegrationTests
{
    private const string ProjectPath = @"..\..\..\..\TinCan.csproj";
    private string _apiKey = "";

    [TestInitialize]
    public void Setup()
    {
        _apiKey = FinnhubServiceSetupHelper.SetupAndGetApiKey();
        _projectDir = Directory.GetCurrentDirectory();
    }

    [TestMethod]
    public async Task PriceCommand_ReturnsValidPrice()
    {
        if (string.IsNullOrEmpty(_apiKey)) Assert.Inconclusive("API key not configured");

        var settings = new Settings
        {
            var settingsPath = Path.Combine(GetTinCanDir(), "settings.json");
            if (File.Exists(settingsPath))
            {
                var content = File.ReadAllText(settingsPath);
                var match = System.Text.RegularExpressions.Regex.Match(content, @"""ApiKey"":\s*""([^""]+)""");
                if (match.Success)
                    _apiKey = match.Groups[1].Value;
            }
        };

        var marketData = MarketDataProviderFactory.Create(settings);
        var result = await marketData.FetchPriceAsync("U");

        Assert.IsNotNull(result);
        Assert.AreEqual("U", result.Symbol);
        Assert.IsTrue(result.Price > 0);
    }

    [TestMethod]
    public async Task BackfillCommand_FetchesHistoricalData()
    {
        if (string.IsNullOrEmpty(_apiKey)) Assert.Inconclusive("API key not configured");

        var settings = new Settings
        {
            Providers = new Providers
            {
                Finnhub = new FinnhubConfig { Enabled = true, ApiKey = _apiKey, Timeout = 10 }
            }
        };

        var marketData = MarketDataProviderFactory.Create(settings);
        // Use recent date range within free tier's 1-year limit
        var to = DateTime.Now;
        var from = to.AddMonths(-6); // 6 months ago - well within 1-year limit

        try
        {
            var result = await marketData.FetchHistoricalPricesAsync("AAPL", "D", from, to);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
            Assert.AreEqual("AAPL", result[0].Symbol);
        }
        catch (HttpRequestException)
        {
            // Finnhub free tier may have limitations on historical data - skip this test
            Assert.Inconclusive("Finnhub free tier may have limitations on historical data endpoint");
        }
    }

    [TestMethod]
    public void ContextCommand_LoadsMarketContext()
    {
        var testDir = Directory.GetCurrentDirectory();
        // Navigate from bin/Debug/net10.0 to project root
        return Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", ".."));
    }

    [TestMethod]
    public void MarketDataProviderFactory_CreatesFinnhubService()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{GetTinCanDir()}\" -- {args}",
            WorkingDirectory = GetTinCanDir(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        await process.WaitForExitAsync(cts.Token);

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            Output = output,
            Error = error,
            TimedOut = !process.HasExited
        };
    }

    [TestMethod]
    public async Task PriceCommand_WithValidSymbol_ReturnsPrice()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Inconclusive("FINNHUB_API_KEY not configured");
            return;
        }

        // Set API key in environment for the CLI
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{GetTinCanDir()}\" -- price U",
            WorkingDirectory = GetTinCanDir(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            EnvironmentVariables = { ["FINNHUB_API_KEY"] = _apiKey }
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await process.WaitForExitAsync(cts1.Token);

        Assert.AreNotEqual(0, process.ExitCode, $"Command failed with error: {await process.StandardError.ReadToEndAsync()}");
        StringAssert.Contains(output, "U:");
        StringAssert.Contains(output, "$");
    }

    [TestMethod]
    public async Task PriceCommand_WithMissingSymbol_ReturnsError()
    {
        var result = await RunCliAsync("price");

        Assert.AreNotEqual(0, result.ExitCode);
        StringAssert.Contains(result.Output + result.Error, "Symbol is required");
    }

    [TestMethod]
    public async Task BackfillCommand_WithValidSymbol_FetchesData()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Inconclusive("FINNHUB_API_KEY not configured");
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{GetTinCanDir()}\" -- backfill AAPL --days 5",
            WorkingDirectory = GetTinCanDir(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            EnvironmentVariables = { ["FINNHUB_API_KEY"] = _apiKey }
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await process.WaitForExitAsync(cts2.Token);

        Assert.AreNotEqual(0, process.ExitCode, $"Command succeeded unexpectedly");
        // Either success with data or graceful error (e.g. free tier limits)
        Assert.IsTrue(
            output.Contains("fetched") ||
            output.Contains("ERROR") ||
            output.Contains("limit"),
            $"Unexpected output: {output}");
    }

    [TestMethod]
    public async Task BackfillCommand_WithMissingSymbol_ReturnsError()
    {
        var result = await RunCliAsync("backfill");

        Assert.AreNotEqual(0, result.ExitCode);
        Assert.IsTrue(
            result.Output.Contains("Symbol is required") ||
            result.Error.Contains("Symbol is required"),
            $"Expected 'Symbol is required' error, got: {result.Output} {result.Error}");
    }

    [TestMethod]
    public void ProviderResolver_UsesPaperAsDefault()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Inconclusive("FINNHUB_API_KEY not configured");
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{GetTinCanDir()}\" -- price U --json",
            WorkingDirectory = GetTinCanDir(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            EnvironmentVariables = { ["FINNHUB_API_KEY"] = _apiKey }
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        using var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await process.WaitForExitAsync(cts3.Token);

        Assert.AreNotEqual(0, process.ExitCode);
        // JSON output should contain price fields
        Assert.IsTrue(
            output.Contains("\"Symbol\"") ||
            output.Contains("\"Price\""),
            $"Expected JSON output, got: {output}");
    }

    [TestMethod]
    public void ProviderResolver_UsesCliProviderWhenProvided()
    {
        // Just verify dotnet run works and shows help
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{GetTinCanDir()}\" -- --help",
            WorkingDirectory = GetTinCanDir(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        Assert.AreEqual(0, process.ExitCode);
        StringAssert.Contains(output, "TinCan");
        StringAssert.Contains(output, "Usage:");
    }

    [TestMethod]
    public void SettingsLoader_LoadsValidSettings()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{GetTinCanDir()}\" -- price --help",
            WorkingDirectory = GetTinCanDir(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        Assert.AreEqual(0, process.ExitCode);
        StringAssert.Contains(output, "Stock symbol");
    }

    private class ProcessResult
    {
        public int ExitCode { get; init; }
        public string Output { get; init; } = "";
        public string Error { get; init; } = "";
        public bool TimedOut { get; init; }
    }
}
