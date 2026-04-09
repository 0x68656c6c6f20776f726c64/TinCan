using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using TinCan.Infrastructure;
using TinCan.Models;

namespace TinCan.Commands;

public static class TradingagentCommand
{
    public static void Execute(CommandLineApplication app)
    {
        var symbolArg = app.Argument<string>("symbol", "Stock symbol to analyze (e.g. AAPL, U)", true);
        var dateOpt = app.Option<string>("--date", "Analysis date (YYYY-MM-DD)", CommandOptionType.SingleValue);
        var analystsOpt = app.Option<string>("--analysts", "Comma-separated analysts (market,social,news,fundamentals)", CommandOptionType.SingleValue);
        var depthOpt = app.Option<int>("--depth", "Research depth (1-5)", CommandOptionType.SingleValue);
        var llmOpt = app.Option<string>("--llm", "LLM provider", CommandOptionType.SingleValue);
        var settingsOpt = app.Option<string>("--settings", "Path to settings.json", CommandOptionType.SingleValue);
        app.HelpOption("-?|-h|--help");

        app.OnExecute(() =>
        {
            var settings = Infrastructure.SettingsLoader.Load(settingsOpt.Value());
            var tradingagents = settings.Tradingagents;

            if (tradingagents == null || string.IsNullOrWhiteSpace(tradingagents.Path))
            {
                Console.WriteLine("[ERROR] TradingAgents path not configured. Please set tradingagents.path in settings.json");
                return 1;
            }

            var symbol = symbolArg.Value;
            if (string.IsNullOrWhiteSpace(symbol))
            {
                Console.WriteLine("[ERROR] Symbol is required.");
                return 1;
            }
            symbol = symbol.ToUpperInvariant();

            var date = dateOpt.HasValue() ? dateOpt.ParsedValue : DateTime.Now.ToString("yyyy-MM-dd");
            var analysts = analystsOpt.HasValue() ? analystsOpt.ParsedValue : string.Join(",", tradingagents.DefaultAnalysts ?? new List<string> { "market", "social", "news", "fundamentals" });
            var depth = depthOpt.HasValue() ? depthOpt.ParsedValue : tradingagents.DefaultDepth;
            var llm = llmOpt.HasValue() ? llmOpt.ParsedValue : (tradingagents.DefaultLlm ?? "minimax");
            var resultsPath = tradingagents.ResultsPath ?? Path.Combine(tradingagents.Path, "eval_results");

            // Run immediately
            return RunTradingAgentNow(symbol, date, analysts, depth, llm, resultsPath, tradingagents.Path);
        });
    }

    private static int RunTradingAgentNow(string symbol, string date, string analysts, int depth, string llm, string resultsPath, string tradingagentsPath)
    {
        Console.WriteLine($"[INFO] Starting TradingAgent analysis for {symbol}...");
        Console.WriteLine($"[INFO] Date: {date}, Analysts: {analysts}, Depth: {depth}, LLM: {llm}");
        Console.WriteLine($"[INFO] Results will be saved to: {resultsPath}");
        Console.WriteLine("==================================================");

        var statusFile = GetStatusFile();
        var status = new TradingAgentStatus
        {
            Symbol = symbol,
            StartTime = DateTime.Now,
            Status = "Initializing"
        };
        File.WriteAllText(statusFile, JsonConvert.SerializeObject(status, Formatting.Indented));

        try
        {
            // Find the Python executable in the TradingAgents venv
            // Use venv/bin/python which automatically activates the venv
            var venvPython = Path.Combine(tradingagentsPath, "venv", "bin", "python");
            var pythonExe = File.Exists(venvPython) ? venvPython : "/opt/homebrew/opt/python@3.13/bin/python3.13";

            // Find our helper script in the binary output
            var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "run_trading_agent.py");
            if (!File.Exists(scriptPath))
            {
                scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "scripts", "run_trading_agent.py");
            }
            if (!File.Exists(scriptPath))
            {
                Console.WriteLine($"[ERROR] run_trading_agent.py not found at {scriptPath}");
                ClearStatusFile();
                return 1;
            }

            // Build command: activate venv then run script
            var activateScript = Path.Combine(tradingagentsPath, "venv", "bin", "activate");
            var args = $"source '{activateScript}' && python \"{scriptPath}\" {symbol} {date} \"{analysts}\" {depth} {llm} \"{resultsPath}\"";

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{args}\"",
                    WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? Directory.GetCurrentDirectory(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    EnvironmentVariables = { ["TA_TRADINGAGENTS_PATH"] = tradingagentsPath }
                }
            };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine(e.Data);
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($"[STDERR] {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            status.Pid = process.Id;
            status.Status = "Running";
            File.WriteAllText(statusFile, JsonConvert.SerializeObject(status, Formatting.Indented));

            // Wait for process to complete
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Console.WriteLine("[INFO] TradingAgent analysis completed successfully");
            }
            else
            {
                Console.WriteLine($"[ERROR] TradingAgent exited with code {process.ExitCode}");
            }

            ClearStatusFile();
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
            ClearStatusFile();
            return 1;
        }
    }

    private static void ClearStatusFile()
    {
        var statusFile = GetStatusFile();
        if (File.Exists(statusFile))
        {
            File.Delete(statusFile);
        }
    }

    private static string GetStatusFile()
    {
        return Path.Combine(Path.GetTempPath(), "tincan_tradingagent_status.json");
    }

    private static string GetScheduledJobsFile()
    {
        return Path.Combine(Path.GetTempPath(), "tincan_tradingagent_scheduled.json");
    }

    private static List<TradingAgentJob> LoadScheduledJobs()
    {
        var file = GetScheduledJobsFile();
        if (!File.Exists(file)) return new List<TradingAgentJob>();
        try
        {
            var json = File.ReadAllText(file);
            return JsonConvert.DeserializeObject<List<TradingAgentJob>>(json) ?? new List<TradingAgentJob>();
        }
        catch
        {
            return new List<TradingAgentJob>();
        }
    }

    private static void SaveScheduledJobs(List<TradingAgentJob> jobs, string file)
    {
        var json = JsonConvert.SerializeObject(jobs, Formatting.Indented);
        File.WriteAllText(file, json);
    }
}
