using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Polyglot.Gamification;

public record NextExerciseResponse(
    string ctx,
    string platform
);

public class GamificationClient
{
    public const string DefaultServerUri = "http://localhost/";
    public static string PolyglotFlowId { get; set; }   // TODO: improve this one
    public static GamificationClient Current { get; private set; }

    private readonly HttpClient _client;
    public Uri ServerUri { get; init; }
    public string CtxId { get; set; }

    private GamificationClient(Uri serverUrl, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(serverUrl);
        ArgumentNullException.ThrowIfNull(httpClient);

        ServerUri = serverUrl;
        _client = httpClient;
    }

    public static void Reset()
    {
        Current?._client.Dispose();
        Current = null;
    }

    public static void Configure(string serverUri = DefaultServerUri, Func<HttpClient> clientFactory = null)
    {
        Configure(new Uri(serverUri), clientFactory);
    }

    public static void Configure(Uri serverUri, Func<HttpClient> clientFactory = null)
    {
        Reset();
        serverUri ??= new Uri(DefaultServerUri);
        Current = new GamificationClient(serverUri, clientFactory?.Invoke() ?? new HttpClient());
    }

    public async Task<bool> SendCommand(string cmd) => await SendCommand(cmd, CtxId, CancellationToken.None);
    public async Task<bool> SendCommand(string cmd, CancellationToken cancellationToken) => await SendCommand(cmd, CtxId, cancellationToken);
    public async Task<bool> SendCommand(string cmd, string contextId, CancellationToken cancellationToken)
    {
        const string requestUri = "/api/execution/cmd";
        var requestBody = new
        {
            ctxId = contextId,
            cmd = cmd
        };

        using var actualRequestBody = requestBody.ToBody();
        var response = await _client.PostAsync(new Uri(ServerUri, requestUri), actualRequestBody, cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return false;
        }

        return true;
    }

    public async Task RequestNewContextAsync() => await RequestNewContextAsync(PolyglotFlowId, CancellationToken.None);
    public async Task RequestNewContextAsync(string polyglotFlowId) => await RequestNewContextAsync(polyglotFlowId, CancellationToken.None);
    public async Task RequestNewContextAsync(string polyglotFlowId, CancellationToken cancellationToken)
    {
        const string requestUri = "/api/execution/first";
        var requestBody = new
        {
            flowId = polyglotFlowId
        };

        using var actualRequestBody = requestBody.ToBody();
        var response = await _client.PostAsync(new Uri(ServerUri, requestUri), actualRequestBody, cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException($"Failed to get context id. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var output = content.ToObject<NextExerciseResponse>();

        CtxId = output.ctx;
    }

    public async Task<PolyglotNode> GetActualNodeAsync() => await GetActualNodeAsync(CancellationToken.None);
    public async Task<PolyglotNode> GetActualNodeAsync(CancellationToken cancellationToken)
    {
        const string requestUri = "/api/execution/actual";
        var requestBody = new
        {
            ctxId = CtxId
        };

        using var actualRequestBody = requestBody.ToBody();
        var response = await _client.PostAsync(new Uri(ServerUri, requestUri), actualRequestBody, cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException($"Failed to get actual node. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return content.ToObject<PolyglotNode>();
    }

    public async Task<PolyglotNode> GetNextExerciseAsync(IEnumerable<string> satisfiedConditions) => await GetNextExerciseAsync(CtxId, satisfiedConditions, CancellationToken.None);
    public async Task<PolyglotNode> GetNextExerciseAsync(string contextId, IEnumerable<string> satisfiedConditions) => await GetNextExerciseAsync(contextId, satisfiedConditions, CancellationToken.None);
    public async Task<PolyglotNode> GetNextExerciseAsync(string contextId, IEnumerable<string> satisfiedConditions, CancellationToken cancellationToken)
    {
        const string requestUri = "/api/execution/next";
        var requestBody = new
        {
            ctxId = contextId,
            satisfiedConditions
        };

        using var actualRequestBody = requestBody.ToBody();
        var response = await _client.PostAsync(new Uri(ServerUri, requestUri), actualRequestBody, cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException($"Failed to get next exercise. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return content.ToObject<PolyglotNode>();
    }
}