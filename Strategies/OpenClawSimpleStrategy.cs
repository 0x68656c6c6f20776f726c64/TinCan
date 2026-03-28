using TinCan.Services;

namespace TinCan.Strategies;

public class OpenClawSimpleStrategy : OpenClawStrategy
{
    public OpenClawSimpleStrategy(IOpenClawService openClawService) 
        : base(openClawService)
    {
    }

    public override string Name => "OpenClawSimpleStrategy";
}
