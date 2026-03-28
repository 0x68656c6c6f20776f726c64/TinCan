using TinCan.Services;

namespace TinCan.Strategies;

public class OpenClawSimpleStrategy : OpenClawStrategy
{
    public override string Name => "OpenClawSimpleStrategy";

    public OpenClawSimpleStrategy(OpenClawService openClawService)
        : base(openClawService)
    {
    }
}
