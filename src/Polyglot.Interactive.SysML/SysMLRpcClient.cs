using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Polyglot.Interactive.SysML;

public sealed class SysMLRpcClient : ISysMLKernel, IDisposable
{
    private ISysMLKernel JsonRpcServer { get; set; }
    private readonly JsonMessageFormatter jsonFormatter;
    private readonly NewLineDelimitedMessageHandler messageHandler;

    public SysMLRpcClient(Stream toServerStream, Stream fromServerStream)
    {
        jsonFormatter = new JsonMessageFormatter();
        jsonFormatter.JsonSerializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
        jsonFormatter.JsonSerializer.Converters.Add(new StringEnumConverter());
        messageHandler = new NewLineDelimitedMessageHandler(toServerStream, fromServerStream, jsonFormatter);
        JsonRpcServer = JsonRpc.Attach<ISysMLKernel>(messageHandler);
    }

    public Task<SysMLInteractiveResult> EvalAsync(string input)
    {
        return JsonRpcServer.EvalAsync(input);
    }

    public Task<string> GetSvgAsync(IEnumerable<string> names, IEnumerable<string> views = null, IEnumerable<string> styles = null, IEnumerable<string> help = null)
    {
        views = views ?? Enumerable.Empty<string>();
        styles = styles ?? Enumerable.Empty<string>();
        help = help ?? Enumerable.Empty<string>();
        return JsonRpcServer.GetSvgAsync(names, views, styles, help);
    }

    public void Dispose()
    {
        jsonFormatter.Dispose();
        _ = messageHandler.DisposeAsync();
    }
}