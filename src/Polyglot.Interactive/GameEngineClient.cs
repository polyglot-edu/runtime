﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.CodeAnalysis;
using Polyglot.Interactive.Contracts;

namespace Polyglot.Interactive
{
    public record GameStateReport(string CurrentLevel, double Points, double GoldCoins, double TimeSpent, double Warning);

    public class GameEngineClient
    {
        private GameStatus _gameStatus;
        private readonly HttpClient _client;
        private DateTime? _lastRun;
        public string GameId { get; }
        public string UserId { get; }
        public string Password { get; }
        public string PlayerId { get; }
        public string ServerUrl { get; }

        private GameEngineClient(string gameId, string userId, string password, string playerId, string serverUrl)
        {
            if (string.IsNullOrWhiteSpace(gameId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(gameId));
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(password));
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(playerId));
            }

            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serverUrl));
            }

            GameId = gameId;
            UserId = userId;
            Password = password;
            PlayerId = playerId;
            ServerUrl = serverUrl;
            _client = new HttpClient();
        }

        public static GameEngineClient Current { get; set; }

        public static void Configure(string gameId, string userId, string password, string playerId, string serverUrl = null)
        {
            Current = new GameEngineClient(gameId, userId, password, playerId,
                string.IsNullOrWhiteSpace(serverUrl) ? DefaultServerUrl : serverUrl);
        }


        public static string DefaultServerUrl { get; } = "https://dev.smartcommunitylab.it/gamification-v3/";

        public async Task<GameStateReport> SubmitActions(KernelCommand command, Kernel kernel,
            List<KernelEvent> events, IReadOnlyDictionary<string, string> newVariables, TimeSpan runTime)
        {

            EnsureAuthentication();

            var callUrl = new Uri(ServerUrl);
            var action = "SubmitCode";

            callUrl = new Uri(callUrl, $"exec/game/{GameId}/action/{action}");
            var bodyObject = new
            {
                gameId = GameId,
                playerId = PlayerId,
                data = new
                {
                    timeSpent = runTime.TotalMilliseconds,
                    timeSinceLastAction = _lastRun is not null ? (DateTime.Now - _lastRun.Value).TotalMilliseconds : 0,
                    success = events.FirstOrDefault(e => e is CommandFailed) is null,
                    warnings = events.OfType<DiagnosticsProduced>().SelectMany(d => d.Diagnostics).Count(d => d.Severity == DiagnosticSeverity.Warning),
                    errors = events.OfType<DiagnosticsProduced>().SelectMany(d => d.Diagnostics).Count(d => d.Severity == DiagnosticSeverity.Error),
                    newVariables = newVariables
                }
            };

            var response = await _client.PostAsync(callUrl, bodyObject.ToBody());

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _lastRun = DateTime.Now;
                return await GetReportAsync();
            }

            var formattedValues = new ImmutableArray<FormattedValue>
            {
                new(PlainTextFormatter.MimeType, response.ReasonPhrase)
            };

            KernelInvocationContext.Current?.Publish(new ErrorProduced(response.ReasonPhrase,
                KernelInvocationContext.Current?.Command, formattedValues));

            return null;
        }

        private void EnsureAuthentication()
        {
            var authToken = Encoding.ASCII.GetBytes($"{UserId}:{Password}");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(authToken));
        }

        public async Task<GameStateReport> GetReportAsync()
        {
            var callUrlStatus = new Uri(ServerUrl);
            callUrlStatus = new Uri(callUrlStatus, $"data/game/{GameId}/player/{PlayerId}");

            var responseStatus = await _client.GetAsync(callUrlStatus);
            if (responseStatus.StatusCode == HttpStatusCode.OK)
            {
                // retrieve the player status from the GET call response and print it

                var contents = await responseStatus.Content.ReadAsStringAsync();

                var gameStatus = contents.ToObject<GameStatus>();

                // report the new status to the player

                _gameStatus = gameStatus;

                var scoring = gameStatus.State.PointConcept.ToDictionary(p => p.Name);

                return new GameStateReport(_gameStatus.CustomData.Level,
                    scoring["points"].Score,
                    scoring["gold coins"].Score,
                    scoring["timeSpent"].Score,
                    scoring["warnings"].Score);
            }

            throw new InvalidCastException(
                $"Failed Game Engine Step, Code: {responseStatus.StatusCode}, Reason: {responseStatus.ReasonPhrase}");
        }
    }

}