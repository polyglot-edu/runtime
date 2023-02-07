using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Polyglot.Interactive
{
    internal static class MultipleChoiceKernelExtensions
    {
        public static string ToChoiceString(this IEnumerable<string> choices)
        {
            return string.Join(", ", choices.Select(s => $"'{s}'"));
        }
        public static IEnumerable<Match> Choices(this string submission)
        {
            return Regex.Matches(submission, @"[^\W]+").Where(m => !string.IsNullOrWhiteSpace(m.Value));
        }
        public static IEnumerable<string> Values(this IEnumerable<Match> choices)
        {
            return choices.Select(c => c.Value);
        }
    }
}
