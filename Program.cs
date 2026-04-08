using McMaster.Extensions.CommandLineUtils;
using TinCan.Commands;

var app = new CommandLineApplication();

app.Name = "tincan";
app.Description = "TinCan CLI - Stock trading data, analysis, and execution platform";
app.VersionOption("--version", "1.0.0");
app.HelpOption("-?|-h|--help");

app.Command("fetch", FetchCommand.Execute);
app.Command("price", PriceCommand.Execute);
app.Command("backfill", BackfillCommand.Execute);
app.Command("context", ContextCommand.Execute);
app.Command("orders", OrdersCommand.Execute);
app.Command("order", OrderCommand.Execute);
app.Command("buy", BuyCommand.Execute);
app.Command("sell", SellCommand.Execute);
app.Command("positions", PositionsCommand.Execute);
app.Command("balance", BalanceCommand.Execute);
app.Command("cancel", CancelCommand.Execute);
app.Command("tradingagent", TradingagentCommand.Execute);

app.OnExecute(() =>
{
    app.ShowHint();
    return 0;
});

return app.Execute(args);
