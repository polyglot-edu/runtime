using Microsoft.DotNet.Interactive.Journey;

namespace Polyglot.Gamification;

public class PolyglotValidationContext
{
    public RuleContext JourneyContext { get; init; }

    public PolyglotValidationContext(RuleContext journeyContext)
    {
        JourneyContext = journeyContext;
    }
}