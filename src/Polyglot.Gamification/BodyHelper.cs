using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Net.Http;
using System.Text;

namespace Polyglot.Gamification;

public static class BodyHelper
{
    private static JsonSerializerSettings Options { get; } = new JsonSerializerSettings
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy
            {
                ProcessDictionaryKeys = false
            }
        },
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        Converters = {
            new ExpandoObjectConverter()
        }
    };

    public static StringContent ToBody(this object source)
    {
        return new(source.ToJson(), Encoding.UTF8, "application/json");
    }

    public static string ToJson(this object source)
    {
        var text = JsonConvert.SerializeObject(source, Options);
        return text;
    }

    public static T ToObject<T>(this string jsonString)
    {
        return JsonConvert.DeserializeObject<T>(jsonString, Options);
    }
}