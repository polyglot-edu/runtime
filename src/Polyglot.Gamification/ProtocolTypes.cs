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

    [JsonProperty("type")]
    public string EdgeType { get; init; }

    [JsonIgnore]
    public bool Satisfied { get; set; }

    [JsonConverter(typeof(ExpandoObjectConverter))]
    public dynamic Data { get; init; }
}

public record ChallengeContent
{
    [JsonProperty("type")]
    public string ContentType { get; init; }
    public string Content { get; init; }
    public int Priority { get; init; } = 0;
}
public record RuntimeData(IEnumerable<string> ChallengeSetup, IEnumerable<ChallengeContent> ChallengeContent);

public record PolyglotNode(
    string Title,
    string Description,
    string Language,
    RuntimeData RuntimeData,
    IEnumerable<PolyglotEdge> Validation,

    [JsonProperty("type")]
    string NodeType,

    [JsonConverter(typeof(ExpandoObjectConverter))]
    dynamic Data
);