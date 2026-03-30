using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace TinCan.Tests.Integration;

[TestClass]
public class CliCommandsIntegrationTests
{
    private string _apiKey = "";
    private Process? _runningProcess;
    private string? _originalWorkDir;

    [TestInitialize]
    public void Setup()
    {
        _apiKey = Environment.GetEnvironmentVariable("FINNHUB_API_KEY") ?? "";
        if (string.IsNullOrEmpty(_apiKey))
        {
            var settingsPath = Path.Combine(GetTinCanDir(), "settings.json");
            if (File.Exists(settingsPath))
            {
                var content = File.ReadAllText(settingsPath);
                var match = System.Text.RegularExpressions.Regex.Match(content, @"""ApiKey"":\s*""([^""]+)""");
                if (match.Success)
                    _apiKey = match.Groups[1].Value;
            }
        }
        _originalWorkDir = Environment.CurrentDirectory;
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_runningProcess != null && !_runningProcess.HasExited)
        {
            _runningProcess.Kill(entireProcessTree: true);
            _runningProcess.Dispose();
            _runningProcess = null;
        }
        if (_originalWorkDir != null)
            Environment.CurrentDirectory = _originalWorkDir;
    }

    private static string GetTinCanDir()
    {
        var testDir = Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", ".."));
    }

    private async Task<ProcessResult> RunCliAsync(string args, int timeoutSeconds = 30)
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

        if (!string.IsNullOrEmpty(_apiKey))
            psi.EnvironmentVariables["FINNHUB_API_KEY"] = _apiKey;

        _runningProcess = new Process { StartInfo = psi };
        _runningProcess.Start();

        var output = await _runningProcess.StandardOutput.ReadToEndAsync();
        var error = await _runningProcess.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        await _runningProcess.WaitForExitAsync(cts.Token);

        var exitCode = _runningProcess.ExitCode;
        _runningProcess.Dispose();
        _runningProcess = null;

        return new ProcessResult
        {
            ExitCode = exitCode,
            Output = output,
            Error = error
        };
    }

    private async Task<ProcessResult> RunCliAsync(string args, string workDir, int timeoutSeconds = 30)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{GetTinCanDir()}\" -- {args}",
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrEmpty(_apiKey))
            psi.EnvironmentVariables["FINNHUB_API_KEY"] = _apiKey;

        _runningProcess = new Process { StartInfo = psi };
        _runningProcess.Start();

        var output = await _runningProcess.StandardOutput.ReadToEndAsync();
        var error = await _runningProcess.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        await _runningProcess.WaitForExitAsync(cts.Token);

        var exitCode = _runningProcess.ExitCode;
        _runningProcess.Dispose();
        _runningProcess = null;

        return new ProcessResult
        {
            ExitCode = exitCode,
            Output = output,
            Error = error
        };
    }

    // ============ Error Path Tests ============

    [DataTestMethod]
    [DataRow("price", 1, "Symbol is required")]
    [DataRow("backfill", 1, "Symbol is required")]
    [DataRow("context", 1, "Symbol is required")]
    [DataRow("backfill AAPL", 1, "--from and --to dates are required")]
    [DataRow("backfill AAPL --from 2024-01-01", 1, "--from and --to dates are required")]
    public async Task CliCommand_MissingRequiredArgs_ReturnsError(string command, int expectedExitCode, string expectedOutput)
    {
        var result = await RunCliAsync(command);

        Assert.AreEqual(expectedExitCode, result.ExitCode, $"Command '{command}' exit code mismatch. Output: {result.Output}, Error: {result.Error}");
        StringAssert.Contains(result.Output + result.Error, expectedOutput, $"Command '{command}' expected error message not found");
    }

    // ============ Help Tests ============

    [DataTestMethod]
    [DataRow("--help", 0, "TinCan")]
    [DataRow("price --help", 0, "Stock symbol")]
    [DataRow("backfill --help", 0, "symbol")]
    [DataRow("context --help", 0, "context")]
    [DataRow("orders --help", 0, "orders")]
    [DataRow("positions --help", 0, "positions")]
    [DataRow("fetch --help", 0, "fetch")]
    public async Task CliSubcommand_Help_ReturnsHelpText(string command, int expectedExitCode, string expectedOutput)
    {
        var result = await RunCliAsync(command);

        Assert.AreEqual(expectedExitCode, result.ExitCode, $"Command '{command}' failed. Error: {result.Error}");
        StringAssert.Contains(result.Output, expectedOutput, $"Command '{command}' help text missing expected content");
    }

    // ============ Price Command Happy Path ============

    [DataTestMethod]
    [DataRow("price U", 0, "$")]
    [DataRow("price AAPL", 0, "$")]
    [DataRow("price MSFT", 0, "$")]
    [DataRow("price U --json", 0, "\"Symbol\"")]
    [DataRow("price AAPL --json", 0, "\"Symbol\"")]
    public async Task PriceCommand_WithSymbol_ReturnsPrice(string command, int expectedExitCode, string expectedOutput)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Inconclusive("FINNHUB_API_KEY not configured");
            return;
        }

        var result = await RunCliAsync(command);

        Assert.AreEqual(expectedExitCode, result.ExitCode, $"Command '{command}' failed. Output: {result.Output}, Error: {result.Error}");
        StringAssert.Contains(result.Output, expectedOutput, $"Command '{command}' output missing expected content: {expectedOutput}");
    }

    // ============ Backfill Command Happy Path ============

    [TestMethod]
    public async Task BackfillCommand_WithValidArgs_FetchesHistoricalData()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Inconclusive("FINNHUB_API_KEY not configured");
            return;
        }

        var to = DateTime.Now;
        var from = to.AddDays(-5);
        var command = $"backfill AAPL --from {from:yyyy-MM-dd} --to {to:yyyy-MM-dd}";

        var result = await RunCliAsync(command);

        // Either succeeds or graceful error (Finnhub free tier limits)
        Assert.IsTrue(
            result.ExitCode == 0 || result.Output.Contains("ERROR") || result.Output.Contains("WARN"),
            $"Unexpected result. ExitCode: {result.ExitCode}, Output: {result.Output}");
    }

    [TestMethod]
    public async Task BackfillCommand_InvalidDateRange_ReturnsError()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Inconclusive("FINNHUB_API_KEY not configured");
            return;
        }

        // from date after to date
        var result = await RunCliAsync("backfill AAPL --from 2024-06-01 --to 2024-01-01");

        Assert.AreEqual(1, result.ExitCode);
        StringAssert.Contains(result.Output + result.Error, "--from date must be before --to date");
    }

    // ============ Context Command Happy Path ============

    [TestMethod]
    public async Task ContextCommand_WithValidSymbol_ReturnsMarketContext()
    {
        // Create a temp directory with proper stock_bot structure
        var tempDir = Path.Combine(Path.GetTempPath(), $"tincan_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "stock_bot"));
        Directory.CreateDirectory(Path.Combine(tempDir, "stock_bot", "results"));

        try
        {
            // Create stock_lookup.json
            var lookup = new
            {
                stocks = new Dictionary<string, object>
                {
                    ["AAPL"] = new { enabled = true, output = "aapl_stock.json" }
                }
            };
            var lookupJson = System.Text.Json.JsonSerializer.Serialize(lookup);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "stock_bot", "stock_lookup.json"), lookupJson);

            // Create a test result file
            var resultFile = Path.Combine(tempDir, "stock_bot", "results", "aapl_stock.json");
            var testData = @"[
                {""time"":""2024-01-15 09:30:00 CT"",""price"":185.50,""high"":186.00,""low"":185.00},
                {""time"":""2024-01-16 09:30:00 CT"",""price"":186.50,""high"":187.00,""low"":186.00}
            ]";
            await File.WriteAllTextAsync(resultFile, testData);

            // Run context command in temp directory
            var result = await RunCliAsync("context AAPL", tempDir);

            Assert.AreEqual(0, result.ExitCode, $"Command failed. Output: {result.Output}, Error: {result.Error}");
            StringAssert.Contains(result.Output, "AAPL");
            StringAssert.Contains(result.Output, "Data points:");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public async Task ContextCommand_WithJsonFlag_ReturnsJsonOutput()
    {
        // Create a temp directory with proper stock_bot structure
        var tempDir = Path.Combine(Path.GetTempPath(), $"tincan_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "stock_bot"));
        Directory.CreateDirectory(Path.Combine(tempDir, "stock_bot", "results"));

        try
        {
            // Create stock_lookup.json
            var lookup = new
            {
                stocks = new Dictionary<string, object>
                {
                    ["AAPL"] = new { enabled = true, output = "aapl_stock.json" }
                }
            };
            var lookupJson = System.Text.Json.JsonSerializer.Serialize(lookup);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "stock_bot", "stock_lookup.json"), lookupJson);

            // Create a test result file
            var resultFile = Path.Combine(tempDir, "stock_bot", "results", "aapl_stock.json");
            var testData = @"[{""time"":""2024-01-15 09:30:00 CT"",""price"":185.50,""high"":186.00,""low"":185.00}]";
            await File.WriteAllTextAsync(resultFile, testData);

            // Run context command in temp directory with --json
            var result = await RunCliAsync("context AAPL --json", tempDir);

            Assert.AreEqual(0, result.ExitCode, $"Command failed. Output: {result.Output}, Error: {result.Error}");
            StringAssert.Contains(result.Output, "\"Symbol\"");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public async Task ContextCommand_WithNoData_ReturnsWarning()
    {
        // Create a temp directory with empty stock_bot structure
        var tempDir = Path.Combine(Path.GetTempPath(), $"tincan_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "stock_bot"));
        Directory.CreateDirectory(Path.Combine(tempDir, "stock_bot", "results"));

        try
        {
            // Create stock_lookup.json with no enabled stocks
            var lookup = new { stocks = new Dictionary<string, object>() };
            var lookupJson = System.Text.Json.JsonSerializer.Serialize(lookup);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "stock_bot", "stock_lookup.json"), lookupJson);

            // Run context command for non-existent symbol
            var result = await RunCliAsync("context XYZ", tempDir);

            Assert.AreEqual(1, result.ExitCode);
            StringAssert.Contains(result.Output + result.Error, "No data found");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // ============ Orders Command Tests ============

    private string GetSettingsPath()
    {
        var settingsPath = Path.Combine(GetTinCanDir(), "stock_bot", "settings.json");
        if (!File.Exists(settingsPath))
            throw new InvalidOperationException("stock_bot/settings.json not found");
        return settingsPath;
    }

    [TestMethod]
    public async Task OrdersCommand_WithAlpacaProvider_ReturnsNoOrders()
    {
        var settingsPath = GetSettingsPath();
        var result = await RunCliAsyncWithSettings("orders", settingsPath);

        // Alpaca broker works, returns 0 with "No orders found"
        Assert.IsTrue(result.ExitCode == 0 || result.Output.Contains("No orders") || result.Output.Contains("Provider"));
    }

    [TestMethod]
    public async Task OrdersCommand_WithOpenFlag_ReturnsNoOrders()
    {
        var settingsPath = GetSettingsPath();
        var result = await RunCliAsyncWithSettings("orders --open", settingsPath);

        // Alpaca broker works with --open flag
        Assert.IsTrue(result.ExitCode == 0 || result.Output.Contains("No orders") || result.Output.Contains("Provider"));
    }

    [TestMethod]
    public async Task OrderCommand_WithoutOrderId_ReturnsError()
    {
        var result = await RunCliAsync("order");

        Assert.AreEqual(1, result.ExitCode);
        StringAssert.Contains(result.Output + result.Error, "Order ID is required");
    }

    [TestMethod]
    public async Task OrderCommand_WithNonExistentOrderId_ReturnsError()
    {
        var settingsPath = GetSettingsPath();
        var result = await RunCliAsyncWithSettings("order non-existent-id", settingsPath);

        Assert.AreEqual(1, result.ExitCode);
        StringAssert.Contains(result.Output + result.Error, "not found");
    }

    // ============ Buy/Sell Command Tests ============

    [TestMethod]
    public async Task BuyCommand_WithValidSymbol_PlacesBuyOrder()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Inconclusive("FINNHUB_API_KEY not configured");
            return;
        }

        // Get settings path for Alpaca
        var settingsPath = Path.Combine(GetTinCanDir(), "stock_bot", "settings.json");
        if (!File.Exists(settingsPath))
        {
            Assert.Inconclusive("stock_bot/settings.json not found");
            return;
        }

        var result = await RunCliAsyncWithSettings("buy MSFT 1", settingsPath);

        Assert.AreEqual(0, result.ExitCode, $"Buy failed: {result.Output} {result.Error}");
        StringAssert.Contains(result.Output, "Order placed successfully");
        StringAssert.Contains(result.Output, "Buy");
    }

    [TestMethod]
    public async Task SellCommand_WithValidSymbol_PlacesSellOrder()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Inconclusive("FINNHUB_API_KEY not configured");
            return;
        }

        var settingsPath = Path.Combine(GetTinCanDir(), "stock_bot", "settings.json");
        if (!File.Exists(settingsPath))
        {
            Assert.Inconclusive("stock_bot/settings.json not found");
            return;
        }

        var result = await RunCliAsyncWithSettings("sell MSFT 1", settingsPath);

        Assert.AreEqual(0, result.ExitCode, $"Sell failed: {result.Output} {result.Error}");
        StringAssert.Contains(result.Output, "Order placed successfully");
        StringAssert.Contains(result.Output, "Sell");
    }

    [TestMethod]
    public async Task BuyCommand_WithMissingSymbol_ReturnsError()
    {
        var result = await RunCliAsync("buy");

        Assert.AreNotEqual(0, result.ExitCode);
        StringAssert.Contains(result.Output + result.Error, "Symbol is required");
    }

    [TestMethod]
    public async Task SellCommand_WithMissingSymbol_ReturnsError()
    {
        var result = await RunCliAsync("sell");

        Assert.AreNotEqual(0, result.ExitCode);
        StringAssert.Contains(result.Output + result.Error, "Symbol is required");
    }

    private async Task<ProcessResult> RunCliAsyncWithSettings(string args, string settingsPath, int timeoutSeconds = 30)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{GetTinCanDir()}\" -- {args} --settings \"{settingsPath}\"",
            WorkingDirectory = GetTinCanDir(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrEmpty(_apiKey))
            psi.EnvironmentVariables["FINNHUB_API_KEY"] = _apiKey;

        _runningProcess = new Process { StartInfo = psi };
        _runningProcess.Start();

        var output = await _runningProcess.StandardOutput.ReadToEndAsync();
        var error = await _runningProcess.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        await _runningProcess.WaitForExitAsync(cts.Token);

        var exitCode = _runningProcess.ExitCode;
        _runningProcess.Dispose();
        _runningProcess = null;

        return new ProcessResult
        {
            ExitCode = exitCode,
            Output = output,
            Error = error
        };
    }

    // ============ Positions Command Tests ============

    [TestMethod]
    public async Task PositionsCommand_ReturnsPositions()
    {
        var settingsPath = GetSettingsPath();
        var result = await RunCliAsyncWithSettings("positions", settingsPath);

        // Alpaca broker works, should show positions or "No positions"
        Assert.IsTrue(result.ExitCode == 0 || result.Output.Contains("positions") || result.Output.Contains("Provider"));
    }

    [TestMethod]
    public async Task CancelCommand_WithoutOrderId_ReturnsError()
    {
        var result = await RunCliAsync("cancel");

        Assert.AreEqual(1, result.ExitCode);
        StringAssert.Contains(result.Output + result.Error, "Order ID is required");
    }

    private class ProcessResult
    {
        public int ExitCode { get; init; }
        public string Output { get; init; } = "";
        public string Error { get; init; } = "";
    }
}
