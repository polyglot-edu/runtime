using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Formatting;
using Polyglot.Gamification;
using System;
using System.Threading.Tasks;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;
using Journey = Microsoft.DotNet.Interactive.Journey;

namespace Polyglot.Interactive;

public class KernelExtension : IKernelExtension
{
    public async Task OnLoadAsync(Kernel kernel)
    {
        await RegisterFormattersAsync();
        await InitializePolyglotAsync();
        //await InitializeJourneyAsync(kernel);
        kernel.RegisterCommands();


        if (KernelInvocationContext.Current is { } current)
        {
            current.DisplayAs("Polyglot.Interactive has loaded!", "text/markdown");
        }
    }

    private static async Task InitializePolyglotAsync()
    {
        GamificationClient.Configure("http://localhost");
        //GamificationClient.PolyglotFlowId = "b2cae670-dffc-4f00-9585-c4b693a0f5d7";
    }

    public static async Task InitializeJourneyAsync(Kernel kernel)
    {
        if (kernel is CompositeKernel compositeKernel)
        {
            if (compositeKernel.RootKernel.FindKernelByName("csharp") is CSharpKernel csharpKernel)
            {
                csharpKernel.DeferCommand(new SubmitCode($"#r \"{typeof(Journey.Lesson).Assembly.Location}\"", csharpKernel.Name));
                csharpKernel.DeferCommand(new SubmitCode($"using {typeof(Journey.Lesson).Namespace};", csharpKernel.Name));
            }

            //Journey.KernelExtensions.UseProgressiveLearningMiddleware(compositeKernel);
            Journey.Lesson.Clear();
            Journey.Lesson.Mode = Journey.LessonMode.StudentMode;


            var firstNode = await GamificationClient.Current.GetInitialExerciseAsync();
            //var actualFirstNode = firstNode;
            var actualFirstNode = await JourneyHelper.AutoSkipChallengesThatDontRequireASubmission(compositeKernel, firstNode);
            var challenge = actualFirstNode.ToJourneyChallenge();
            await Journey.KernelExtensions.InitializeChallenge(compositeKernel, challenge);
            await Journey.Lesson.StartChallengeAsync(challenge);
            //KernelInvocationContext.Current.Display(firstNode);
            //KernelInvocationContext.Current.Display(actualFirstNode);
            //KernelInvocationContext.Current.Display(challenge);
            //KernelInvocationContext.Current.Display(value: "First challenge started");
        }
        else
        {
            throw new ArgumentException("Not composite kernel");
        }

        if (KernelInvocationContext.Current is { } current)
        {
            current.DisplayAs("Journey has loaded!", "text/markdown");
        }
    }

    private static Task RegisterFormattersAsync()
    {
        Formatter.Register<GameStateReport>((report, writer) =>
        {
            var scoreEmoji = report.AssignmentPoints switch
            {
                var n when (0 <= n && n <= 10) => "🙂",
                var n when (10 < n && n <= 20) => "😊",
                var n when (20 < n && n <= 30) => "🤗",
                var n when (30 < n && n <= 40) => "😍",
                var n when (40 < n && n <= 50) => "🤩",
                _ => "😑"
            };

            var html = div(scoreEmoji);

            //var feedbackDisplay = report.Feedbacks.Count() == 0 ? "display:none" : "";
            //var feedbacks = report.Feedbacks.Select(f =>
            //    tr[style: feedbackDisplay](
            //        // td[style: "width: 50px"]("Feedback"),
            //        td["colspan='8'"](f)
            //    )
            //);

            //var divStyle = "font-size: 2em; display: flex; justify-content: center; align-items: center";
            //var flames = string.Join("", Enumerable.Range(0, (int)report.AssignmentGoldCoins).Select(_ => "🥇"));
            //var html = div[style: "width:800px; border: 1px solid black; padding: 5px"](
            //    h1[style: "margin-left: 10px"]("Report"),
            //    table(
            //        tr(
            //            td[style: "width: 50px"]("Level:"), td[style: "width:150px"](div[style: divStyle](report.CurrentLevel)),
            //            td[style: "width: 50px"]("Exercise Points:"), td[style: "width:150px"](div[style: divStyle](report.ExercisePoints)),
            //            td[style: "width: 50px"]("Assignment Score:"), td[style: "width:150px"](p[style: "font-size:3em"](scoreEmoji)),
            //            td[style: "width: 150px"]("Medals:"), td[style: "width:150px"](p[style: "font-size:3em"](flames))
            //        )
            //    ),
            //    h2[style: ("margin-left: 10px;" + feedbackDisplay)]("Feedbacks"),
            //    table(
            //        feedbacks
            //    )
            //);
            writer.Write(html);
        }, HtmlFormatter.MimeType);

        Formatter.SetPreferredMimeTypesFor(typeof(GameStateReport), HtmlFormatter.MimeType);

        return Task.CompletedTask;
    }
}