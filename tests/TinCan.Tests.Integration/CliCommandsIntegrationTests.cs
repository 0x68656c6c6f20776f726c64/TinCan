using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace TinCan.Tests.Integration;

[TestClass]
public class CliCommandsIntegrationTests
{
    private string _apiKey = "";
    private Process? _runningProcess;

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
            Error = error,
            TimedOut = !_runningProcess?.HasExited ?? false
        };
    }

    private async Task<ProcessResult> RunCliWithEnvAsync(string args, string envVar, string envValue, int timeoutSeconds = 30)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{GetTinCanDir()}\" -- {args}",
            WorkingDirectory = GetTinCanDir(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            EnvironmentVariables = { [envVar] = envValue }
        };

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
            Error = error,
            TimedOut = false
        };
    }

    [DataTestMethod]
    [DataRow("price", 1, "Symbol is required")]
    [DataRow("backfill", 1, "Symbol is required")]
    [DataRow("--help", 0, "TinCan")]
    public async Task CliCommand_ReturnsExpectedExitCodeAndOutput(string command, int expectedExitCode, string expectedOutput)
    {
        var result = await RunCliAsync(command);

        Assert.AreEqual(expectedExitCode, result.ExitCode, $"Command '{command}' exit code mismatch. Error: {result.Error}");
        StringAssert.Contains(result.Output + result.Error, expectedOutput, $"Command '{command}' output mismatch");
    }

    [DataTestMethod]
    [DataRow("price --help", 0, "Stock symbol")]
    [DataRow("backfill --help", 0, "symbol")]
    [DataRow("context --help", 0, "context")]
    [DataRow("orders --help", 0, "orders")]
    public async Task CliSubcommand_Help_ReturnsHelpText(string command, int expectedExitCode, string expectedOutput)
    {
        var result = await RunCliAsync(command);

        Assert.AreEqual(expectedExitCode, result.ExitCode, $"Command '{command}' failed. Error: {result.Error}");
        StringAssert.Contains(result.Output, expectedOutput, $"Command '{command}' help text missing expected content");
    }

    [DataTestMethod]
    [DataRow("price U", 0, "$")]
    [DataRow("price AAPL", 0, "$")]
    [DataRow("price U --json", 0, "\"Symbol\"")]
    public async Task PriceCommand_WithSymbol_ReturnsPrice(string command, int expectedExitCode, string expectedOutput)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Inconclusive("FINNHUB_API_KEY not configured");
            return;
        }

        var result = await RunCliWithEnvAsync(command, "FINNHUB_API_KEY", _apiKey);

        Assert.AreEqual(expectedExitCode, result.ExitCode, $"Command '{command}' failed. Output: {result.Output}, Error: {result.Error}");
        StringAssert.Contains(result.Output, expectedOutput, $"Command '{command}' output missing expected content");
    }

    private class ProcessResult
    {
        public int ExitCode { get; init; }
        public string Output { get; init; } = "";
        public string Error { get; init; } = "";
        public bool TimedOut { get; init; }
    }
}
