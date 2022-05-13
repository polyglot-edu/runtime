using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Polyglot.Gamification;

public class GamificationClient
{
    public const string DefaultServerUri = "http://localhost/";
    public static GamificationClient Current { get; private set; }

    private readonly HttpClient _client;
    public Uri ServerUri { get; init; }

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

    public async Task<PolyglotNode> GetInitialExerciseAsync(string polyglotFlowId) => await GetInitialExerciseAsync(polyglotFlowId, CancellationToken.None);
    public async Task<PolyglotNode> GetInitialExerciseAsync(string polyglotFlowId, CancellationToken cancellationToken)
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
            throw new HttpRequestException($"Failed to get initial exercise. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return content.ToObject<PolyglotNode>();
    }

    public async Task<PolyglotNode> GetNextExerciseAsync(string polyglotFlowId, IEnumerable<string> satisfiedConditions) => await GetNextExerciseAsync(polyglotFlowId, satisfiedConditions, CancellationToken.None);
    public async Task<PolyglotNode> GetNextExerciseAsync(string polyglotFlowId, IEnumerable<string> satisfiedConditions, CancellationToken cancellationToken)
    {
        const string requestUri = "/api/execution/next";
        var requestBody = new
        {
            flowId = polyglotFlowId,
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