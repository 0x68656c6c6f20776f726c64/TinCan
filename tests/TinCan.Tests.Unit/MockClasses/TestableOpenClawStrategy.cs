using TinCan.Models;
using TinCan.Services;
using TinCan.Strategies;

namespace TinCan.Tests.Unit;

public class TestableOpenClawStrategy : OpenClawStrategy
{
    public TestableOpenClawStrategy(OpenClawService service) : base(service) { }

    public Task<Signal> CallBuildSignalFromResponseAsync(OpenClawResponse? response)
    {
        return BuildSignalFromResponseAsync(response);
    }
}