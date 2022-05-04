using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;
using System.Linq;
using System.Threading.Tasks;

using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Polyglot.Interactive.SysML;

public class KernelExtension : IKernelExtension
{
    public Task OnLoadAsync(Kernel kernel)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        // kernel will be disposed by .NET Interactive when it is unloaded
        (Kernel.Root as CompositeKernel)?.Add(new SysMLKernel());
#pragma warning restore CA2000 // Dispose objects before losing scope

        return RegisterFormattersAsync();
    }

    public static Task RegisterFormattersAsync()
    {
        Formatter.Register<SysMLSvg>((value, writer) =>
        {
            var html = div(new HtmlString(value.Svg));
            writer.Write(html);
        }, HtmlFormatter.MimeType);

        Formatter.Register<SysMLInteractiveResult>((value, writer) =>
        {
            var html = div(new HtmlString($"done with {value.Warnings?.Count()} warnings."));
            writer.Write(html);
        }, HtmlFormatter.MimeType);

        return Task.CompletedTask;
    }
}