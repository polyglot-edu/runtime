using Microsoft.DotNet.Interactive.Journey;

namespace Polyglot.Gamification;

public record PolyglotValidationContext(
    RuleContext JourneyContext,
    Exercise Exercise,
    Condition Condition
);

public record Exercise(dynamic Data, string NodeType);

public record Condition(dynamic Data);