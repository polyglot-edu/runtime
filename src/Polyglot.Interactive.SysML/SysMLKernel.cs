using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polyglot.Interactive.SysML;

public class SysMLKernel :
    Kernel,
    IKernelCommandHandler<SubmitCode>
{
    private SysMLRpcClient SysMLRpcClient { get; set; }
    private Process SysMLProcess { get; set; }

    public SysMLKernel() : base("sysml")
    {
        KernelInfo.DisplayName = "SysML";
        KernelInfo.LanguageName = "SysML";
        KernelInfo.LanguageVersion = "2.0";
    }

    private void SetProcessEventHandlers(Process SysMLProcess)
    {
        SysMLProcess.Exited += (o, args) =>
        {
            SysMLRpcClient = null;
        };

        SysMLProcess.ErrorDataReceived += (o, args) =>
        {
            KernelInvocationContext.Current?.Publish(new ErrorProduced(args.Data, KernelInvocationContext.Current?.Command));
        };
        SysMLProcess.BeginErrorReadLine();
    }

    public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
    {
        EnsureSysMLKernelServerIsRunning();

        var result = await SysMLRpcClient.EvalAsync(command.Code);

        var errors = result.SyntaxErrors.Concat(result.SemanticErrors).ToList();

        if (errors.Count > 0)
        {
            var errorMessage = new StringBuilder();
            foreach (var error in errors.Select(e => e.Message))
            {
                errorMessage.AppendLine(error);
            }

            context.Fail(command, message: errorMessage.ToString());
            return;
        }

        var sumbittedItems = result.Content.Select(c => c.Name);
        var svgText = await SysMLRpcClient.GetSvgAsync(sumbittedItems);

        var svg = new SysMLSvg(svgText);

        context.Display(svg, HtmlFormatter.MimeType);
        context.Publish(new ReturnValueProduced(result, command, FormattedValue.CreateManyFromObject(result)));
    }

    private void EnsureSysMLKernelServerIsRunning()
    {
        if (SysMLProcess is null)
        {
            EnsureJavaRuntimeIsInstalled();

            var sysMLJarPath = Environment.GetEnvironmentVariable("SYSML_JAR_PATH");
            if (sysMLJarPath is null)
            {
                throw new RuntimeDependencyMissingException("Environment variable \"SYSML_JAR_PATH\" is not set");
            }

            var psi = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar SysMLKernelServer.jar",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = sysMLJarPath
            };

            SysMLProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
            RegisterForDisposal(() =>
            {
                SysMLRpcClient.Dispose();
                SysMLRpcClient = null;

                SysMLProcess.Kill(true);
                SysMLProcess.Dispose();
                SysMLProcess = null;
            });

            SysMLProcess.Start();
            SetProcessEventHandlers(SysMLProcess);

            SysMLRpcClient = new(SysMLProcess.StandardInput.BaseStream, SysMLProcess.StandardOutput.BaseStream);
        }
    }

    private static void EnsureJavaRuntimeIsInstalled()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = " --version",
                RedirectStandardError = true,
                UseShellExecute = false
            };

            Process pr = Process.Start(psi);
        }
        catch (Exception)
        {
            throw new RuntimeDependencyMissingException("Missing JRE. JRE is required to use the SysML Kernel");
        }
    }
}