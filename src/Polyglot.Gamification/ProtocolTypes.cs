using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Polyglot.Gamification;

public record StringSpan(int Start, int End);

public class PolyglotEdge
{
    public string Id { get; init; }
    public string Title { get; init; }
    public string Code { get; init; }

    [JsonIgnore]
    public bool Satisfied { get; set; }

    [JsonConverter(typeof(ExpandoObjectConverter))]
    public dynamic Data { get; init; }
}

public record PolyglotNode(
    string Title,
    string Description,
    string Language,
    IEnumerable<string> Setup,
    IEnumerable<string> Display,
    IEnumerable<string> PostExecution,
    IEnumerable<PolyglotEdge> Validation,

    [JsonConverter(typeof(ExpandoObjectConverter))]
    dynamic Data
);