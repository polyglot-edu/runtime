using Microsoft.CodeAnalysis.Tags;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Polyglot.Interactive
{
    public class MultipleChoiceKernel :
        Kernel,
        IKernelCommandHandler<SubmitCode>,
        IKernelCommandHandler<RequestCompletions>,
        IKernelCommandHandler<RequestDiagnostics>
    {
        public IReadOnlySet<string> Options { get; set; } = new HashSet<string>();

        public MultipleChoiceKernel() : base("multiplechoice")
        {
            KernelInfo.DisplayName = "Multiple Choice";
            KernelInfo.LanguageName = "Multiple Choice";
        }

        public Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            var choices = command.Code.Choices().Values().ToHashSet();

            if (!choices.IsSubsetOf(Options))
            {
                var diff = choices.Except(Options);
                context.Fail(command, message: $"""

Invalid multiplechoice option{(diff.Count() > 1 ? 's' : string.Empty)}: {diff.ToChoiceString()}
Valid options are: {Options.ToChoiceString()}
""");
            }
            else
            {
                context.Publish(new ReturnValueProduced(choices, command, FormattedValue.CreateManyFromObject(choices)));
            }

            return Task.CompletedTask;
        }

        public Task HandleAsync(RequestCompletions command, KernelInvocationContext context)
        {
            // get word at cursor position
            var cursorPosition = SourceUtilities.GetCursorOffsetFromPosition(command.Code, command.LinePosition);
            var currentWord = command.Code
                .Choices()
                .Where(m => m.Index <= cursorPosition && cursorPosition <= m.Index + m.Length)
                .Select(m => m.Value)
                .FirstOrDefault() ?? string.Empty;

            context.Publish(new CompletionsProduced(
                Options.Where(o => o.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                        .Select(o => new CompletionItem(o, WellKnownTags.EnumMember)),
                command
            ));

            return Task.CompletedTask;
        }

        public Task HandleAsync(RequestDiagnostics command, KernelInvocationContext context)
        {
            var choices = command.Code.Choices();

            var diagnostics = choices
                .Where(m => !Options.Contains(m.Value))
                .Select(m =>
                {
                    var linePositionSpan = SourceUtilities.GetLinePositionSpanFromStartAndEndIndices(command.Code, m.Index, m.Index + m.Length);

                    return new Diagnostic
                    (
                        linePositionSpan,
                        Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
                        "INVALID_CHOICE",
                        $"Invalid multiplechoice option: '{m.Value}'"
                    );
                });

            context.Publish(new DiagnosticsProduced(diagnostics, command));

            return Task.CompletedTask;
        }
    }
}
