using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.DotNet.Interactive.Journey;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Polyglot.Gamification;

public class ScriptParams
{
    public PolyglotValidationContext polyglotContext;
}

public static class JourneyHelper
{

    public static Challenge ToJourneyChallenge(this PolyglotNode exercise)
    {
        return exercise.ToJourneyChallenge(CancellationToken.None);
    }

    public static Challenge ToJourneyChallenge(this PolyglotNode exercise, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exercise);

        var challenge = new Challenge(name: exercise.Title);
        exercise.Validation.ToList().ForEach(edge => challenge.AddRuleAsync(edge.Title, configureExecution(edge)));
        return challenge;

        Func<RuleContext, Task> configureExecution(PolyglotEdge edge)
        {
            return async (RuleContext context) =>
            {
                try
                {
                    var globals = new ScriptParams
                    {
                        polyglotContext = new PolyglotValidationContext(context)
                    };

                    var scriptOptions = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default.WithImports("Polyglot.Gamification")
                                                                                                        .AddReferences(typeof(PolyglotValidationContext).Assembly);

                    var script = CSharpScript.Create<(bool, string)>(edge.Code, scriptOptions, globalsType: typeof(ScriptParams));
                    script = script.ContinueWith<(bool, string)>("return validate(polyglotContext);");

                    // https://github.com/dotnet/roslyn/issues/41722
                    var (result, reason) = (await script.RunAsync(globals, cancellationToken)).ReturnValue;
                    if (result)
                    {
                        edge.Satisfied = true;
                        context.Pass($"✅ {reason}");
                    }
                    else
                    {
                        edge.Satisfied = false;
                        context.Fail($"❌ {reason}");
                    }
                }
                catch (Exception ex)
                {
                    edge.Satisfied = false;
                    context.Fail($"❌ Failed with exception: {ex.Message}");
                    throw;
                }
            };
        }
    }
}
