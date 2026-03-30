namespace TinCan.Infrastructure;

public static class ProviderResolver
{
    private const string EnvVarProvider = "TINCAN_PROVIDER";

    public static string Resolve(string? cliProvider, string? configProvider)
    {
        return cliProvider
            ?? Environment.GetEnvironmentVariable(EnvVarProvider)
            ?? configProvider
            ?? "paper";
    }
}
