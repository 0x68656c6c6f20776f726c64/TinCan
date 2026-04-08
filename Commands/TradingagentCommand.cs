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
        var runNowOpt = app.Option<bool>("--run-now", "Run immediately ignoring schedule", CommandOptionType.NoValue);
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

            var scheduler = settings.Scheduler;
            var hasScheduleTime = !string.IsNullOrWhiteSpace(scheduler?.TradingagentTime);

            if (hasScheduleTime && !runNowOpt.HasValue())
            {
                // Schedule mode - write job info to a status file and return
                return ScheduleTradingAgent(symbol, date, analysts, depth, llm, resultsPath, scheduler!.TradingagentTime!, tradingagents.Path);
            }
            else
            {
                // Run immediately
                return RunTradingAgentNow(symbol, date, analysts, depth, llm, resultsPath, tradingagents.Path);
            }
        });
    }

    private static int ScheduleTradingAgent(string symbol, string date, string analysts, int depth, string llm, string resultsPath, string scheduleTime, string tradingagentsPath)
    {
        var jobInfo = new TradingAgentJob
        {
            Symbol = symbol,
            Date = date,
            Analysts = analysts,
            Depth = depth,
            Llm = llm,
            ResultsPath = resultsPath,
            ScheduleTime = scheduleTime,
            TradingagentsPath = tradingagentsPath,
            ScheduledAt = DateTime.Now
        };

        var jobsFile = GetScheduledJobsFile();
        var jobs = LoadScheduledJobs();
        jobs.Add(jobInfo);
        SaveScheduledJobs(jobs, jobsFile);

        Console.WriteLine($"[INFO] TradingAgent job scheduled for {symbol} at {scheduleTime}");
        Console.WriteLine($"[INFO] Job saved to {jobsFile}");
        return 0;
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
            var cliPath = Path.Combine(tradingagentsPath, "cli");
            var mainPy = Path.Combine(cliPath, "main.py");

            if (!File.Exists(mainPy))
            {
                Console.WriteLine($"[ERROR] TradingAgents main.py not found at {mainPy}");
                ClearStatusFile();
                return 1;
            }

            var analystsList = analysts.Split(',').Select(a => a.Trim().ToLower()).ToList();
            var analystsArg = string.Join(" ", analystsList);

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"-m cli.main analyze --ticker {symbol} --date {date} --analysts \"{analystsArg}\" --depth {depth} --llm {llm}",
                    WorkingDirectory = cliPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine(e.Data);
                    UpdateStatusProgress(statusFile, symbol);
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($"[ERROR] {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            status.Pid = process.Id;
            File.WriteAllText(statusFile, JsonConvert.SerializeObject(status, Formatting.Indented));

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

    private static void UpdateStatusProgress(string statusFile, string symbol)
    {
        if (File.Exists(statusFile))
        {
            try
            {
                var json = File.ReadAllText(statusFile);
                var status = JsonConvert.DeserializeObject<TradingAgentStatus>(json);
                if (status != null)
                {
                    status.Status = "Running";
                    File.WriteAllText(statusFile, JsonConvert.SerializeObject(status, Formatting.Indented));
                }
            }
            catch { }
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

public class TradingAgentJob
{
    [JsonProperty("symbol")]
    public string Symbol { get; set; } = "";

    [JsonProperty("date")]
    public string Date { get; set; } = "";

    [JsonProperty("analysts")]
    public string Analysts { get; set; } = "";

    [JsonProperty("depth")]
    public int Depth { get; set; }

    [JsonProperty("llm")]
    public string Llm { get; set; } = "";

    [JsonProperty("results_path")]
    public string ResultsPath { get; set; } = "";

    [JsonProperty("schedule_time")]
    public string ScheduleTime { get; set; } = "";

    [JsonProperty("tradingagents_path")]
    public string TradingagentsPath { get; set; } = "";

    [JsonProperty("scheduled_at")]
    public DateTime ScheduledAt { get; set; }
}

public class TradingAgentStatus
{
    [JsonProperty("symbol")]
    public string Symbol { get; set; } = "";

    [JsonProperty("start_time")]
    public DateTime StartTime { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; } = "";

    [JsonProperty("pid")]
    public int Pid { get; set; }
}
