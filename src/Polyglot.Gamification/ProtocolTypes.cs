using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Polyglot.Gamification;

public record StringSpan(int Start, int End);

public class PolyglotEdge
{
    public string Id { get; init; }
    public string Title { get; init; }
    public string Code { get; init; }

    [JsonIgnore]
    public bool Satisfied { get; set; }
}

public record PolyglotNode(
    string Title,
    string Description,
    string Language,
    IEnumerable<string> Setup,
    IEnumerable<string> Display,
    IEnumerable<string> PostExecution,
    IEnumerable<PolyglotEdge> Validation
);