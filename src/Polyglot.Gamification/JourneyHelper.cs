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

    public static Challenge ToJourneyChallenge(this PolyglotNode node, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(node);

        var challenge = new Challenge(name: node.Title);

        var exercise = new Exercise(node.Data);

        node.Validation.ToList().ForEach(edge => challenge.AddRuleAsync(edge.Title, configureExecution(edge)));
        return challenge;

        Func<RuleContext, Task> configureExecution(PolyglotEdge edge)
        {
            return async (RuleContext context) =>
            {
                try
                {
                    var globals = new ScriptParams
                    {
                        polyglotContext = new PolyglotValidationContext(
                            context,
                            exercise,
                            new Condition(edge.Data)
                        )
                    };

                    var references = new[]
                    {
                        typeof(RuleContext).Assembly,
                        typeof(PolyglotValidationContext).Assembly
                    };

                    var imports = new[]
                    {
                        "System",
                        "Microsoft.DotNet.Interactive.Journey",
                        "Polyglot.Gamification"
                    };


                    var scriptOptions = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default.AddReferences(references)
                                                                                                        .AddImports(imports);


                    var script = CSharpScript.Create<(bool, string)>($"return validate(polyglotContext);\n{edge.Code}", scriptOptions, globalsType: typeof(ScriptParams));

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
