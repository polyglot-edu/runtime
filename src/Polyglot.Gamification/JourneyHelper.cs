using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Journey;
using Polyglot.Interactive.SysML;
using Polyglot.Metrics.SysML;
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

        var setupSubmissions = node.RuntimeData.ChallengeSetup.Select(s => new SubmitCode(s)).ToList().AsReadOnly();
        var contentSubmissions = node.RuntimeData.ChallengeContent
                                                                            .OrderBy(c => c.Priority)
                                                                            .Select(c => new SendEditableCode(c.ContentType, c.Content))
                                                                            .ToList().AsReadOnly();
        var challenge = new Challenge(setupSubmissions, contentSubmissions, name: node.Title);

        var exercise = new Exercise(node.Data, node.NodeType);


        node.Validation.ToList().ForEach(edge => challenge.AddRuleAsync(edge.Title, configureExecution(edge)));
        ConfigurePolyglotProgressionHandler(challenge, node);
        //KernelInvocationContext.Current.Display(value: "converted to journey challenge");
        return challenge;

        Func<RuleContext, Task> configureExecution(PolyglotEdge edge)
        {
            //KernelInvocationContext.Current.Display(value: "configuring rule");
            return async (RuleContext context) =>
            {
                //KernelInvocationContext.Current.Display(value: "running rule");
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
                        typeof(PolyglotValidationContext).Assembly,
                        typeof(SysMLElement).Assembly,
                        typeof(SysMLElementKind).Assembly,
                        typeof(DefinitionStructureMetric).Assembly,
                        typeof(ReturnValueProduced).Assembly,
                        typeof(CSharpScript).Assembly,
                        typeof(Task).Assembly,
                        typeof(Enumerable).Assembly,
                        typeof(System.Collections.IEnumerable).Assembly,
                        typeof(System.Collections.Generic.IEnumerable<>).Assembly,
                        typeof(System.Collections.Generic.List<>).Assembly,
                        typeof(Microsoft.CodeAnalysis.Scripting.ScriptOptions).Assembly
                    };

                    var imports = new[]
                    {
                        "System",
                        "System.Linq",
                        "System.Collections",
                        "System.Collections.Generic",
                        "Microsoft.DotNet.Interactive.Journey",
                        "Microsoft.DotNet.Interactive.Events",
                        "Microsoft.CodeAnalysis.CSharp.Scripting",
                        "System.Threading",
                        "System.Threading.Tasks",
                        "Polyglot.Gamification",
                        "Polyglot.Interactive.SysML",
                        "Polyglot.Metrics.SysML"
                    };

                    var scriptOptions = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default.AddReferences(references)
                                                                                                        .AddImports(imports);

                    var script = CSharpScript.Create<(bool, string)>($"return await validate(polyglotContext);\n{edge.Code}", scriptOptions, globalsType: typeof(ScriptParams));

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

    private static void ConfigurePolyglotProgressionHandler(Challenge challenge, PolyglotNode node)
    {
        //KernelInvocationContext.Current.Display(value: "configuring progression handler");
        challenge.OnCodeSubmittedAsync(async context =>
        {
            //KernelInvocationContext.Current.Display(value: "running progression handler");
            var satisfiedConditions = node.Validation.Where(e => e.Satisfied).Select(e => e.Id);
            var nextPolyglotNode = await GamificationClient.Current.GetNextExerciseAsync(satisfiedConditions);
            if (nextPolyglotNode is not null)
            {
                if (Kernel.Root is CompositeKernel compositeKernel)
                {
                    //KernelInvocationContext.Current.Display(value: "current kernel is CompositeKernel");
                    nextPolyglotNode = await AutoSkipChallengesThatDontRequireASubmission(compositeKernel, nextPolyglotNode);
                }
                else
                {
                    KernelInvocationContext.Current.Display(value: "current kernel is not CompositeKernel");
                }
                //KernelInvocationContext.Current.Display(nextPolyglotNode);
                var nextChallenge = nextPolyglotNode.ToJourneyChallenge();
                await context.StartChallengeAsync(nextChallenge);
            }
        });
    }

    public static async Task<PolyglotNode> AutoSkipChallengesThatDontRequireASubmission(CompositeKernel compositeKernel, PolyglotNode node)
    {
        ArgumentNullException.ThrowIfNull(compositeKernel);
        ArgumentNullException.ThrowIfNull(node);

        var currentNode = node;
        var challengeDoesntRequireSubmission = currentNode.Validation.All(e => e.EdgeType == "unconditionalEdge") && currentNode.Validation.Any();
        while (challengeDoesntRequireSubmission)
        {

            // need to change this ASAP. This is a hacky fix to an architectural problem
            if (!currentNode.Platform.Equals("VSCode", StringComparison.Ordinal))
            {
                return currentNode;
            }

            // send challenge to journey so it displays
            var currentChallenge = currentNode.ToJourneyChallenge();
            await Lesson.StartChallengeAsync(currentChallenge);
            await compositeKernel.InitializeChallenge(currentChallenge);

            Thread.Sleep(1000); // TODO: find a better way to wait for the challenge to be displayed

            // retrieve next challenge
            currentNode = await GamificationClient.Current.GetNextExerciseAsync(currentNode.Validation.Select(e => e.Id));
            challengeDoesntRequireSubmission = currentNode.Validation.All(e => e.EdgeType == "unconditionalEdge") && currentNode.Validation.Any();
        }
        return currentNode;
    }
}
