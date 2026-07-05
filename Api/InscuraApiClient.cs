using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugin.Inscura.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugin.Inscura.Api
{
    public class InscuraApiClient
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger;

        public InscuraApiClient(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogger logger)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _logger = logger;
        }

        public async Task<ApiLibrarySummary?> GetLibraryAsync(CancellationToken cancellationToken)
        {
            return await GetAsync<ApiLibrarySummary>("api/v1/library", cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<ApiMediaListItem>> SearchMediaAsync(string query, int limit, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new ApiMediaListItem[0];
            }

            var path = "api/v1/media/search?q=" + Uri.EscapeDataString(query.Trim()) + "&limit=" + ClampLimit(limit).ToString(CultureInfo.InvariantCulture);
            var results = await GetAsync<List<ApiMediaListItem>>(path, cancellationToken).ConfigureAwait(false);
            return results ?? new List<ApiMediaListItem>();
        }

        public async Task<ApiMediaDetail?> GetMediaAsync(long id, CancellationToken cancellationToken)
        {
            return await GetAsync<ApiMediaDetail>("api/v1/media/" + id.ToString(CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<ApiActorSearchItem>> SearchActorsAsync(string query, int limit, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new ApiActorSearchItem[0];
            }

            var path = "api/v1/actors/search?q=" + Uri.EscapeDataString(query.Trim()) + "&limit=" + ClampLimit(limit).ToString(CultureInfo.InvariantCulture);
            var results = await GetAsync<List<ApiActorSearchItem>>(path, cancellationToken).ConfigureAwait(false);
            return results ?? new List<ApiActorSearchItem>();
        }

        public async Task<ApiActorDetail?> GetActorAsync(long id, CancellationToken cancellationToken)
        {
            return await GetAsync<ApiActorDetail>("api/v1/actors/" + id.ToString(CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
        }

        public Task<HttpResponseInfo> GetImageResponseAsync(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(CreateRequest(url, cancellationToken, false));
        }

        public string AddQueryTokenIfNeeded(string? url)
        {
            var configuration = GetConfiguration();
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(configuration.ApiToken) || !IsInscuraApiUrl(url, configuration))
            {
                return url ?? string.Empty;
            }

            var separator = url.IndexOf("?", StringComparison.Ordinal) >= 0 ? "&" : "?";
            return url + separator + "token=" + Uri.EscapeDataString(configuration.ApiToken.Trim());
        }

        private async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
        {
            var configuration = GetConfiguration();
            var url = BuildUri(path, configuration).ToString();

            try
            {
                using (var response = await _httpClient.GetResponse(CreateRequest(url, cancellationToken, true)).ConfigureAwait(false))
                {
                    if (response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.MultipleChoices)
                    {
                        _logger.Warn("Inscura API request failed: " + response.StatusCode + " " + path);
                        return default(T);
                    }

                    if (response.Content == null)
                    {
                        return default(T);
                    }

                    var envelope = _jsonSerializer.DeserializeFromStream<ApiEnvelope<T>>(response.Content);
                    return envelope == null ? default(T) : envelope.Data;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Warn("Inscura API request failed: " + path + " " + ex.Message);
                return default(T);
            }
        }

        private HttpRequestOptions CreateRequest(string url, CancellationToken cancellationToken, bool json)
        {
            var configuration = GetConfiguration();
            var request = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                TimeoutMs = ClampTimeout(configuration.RequestTimeoutSeconds) * 1000,
                AcceptHeader = json ? "application/json" : null,
                EnableHttpCompression = true,
                ThrowOnErrorResponse = false,
                LogErrorResponseBody = false
            };

            if (!string.IsNullOrWhiteSpace(configuration.ApiToken) && IsInscuraApiUrl(url, configuration))
            {
                request.RequestHeaders["Authorization"] = "Bearer " + configuration.ApiToken.Trim();
            }

            return request;
        }

        private static Uri BuildUri(string path, PluginConfiguration configuration)
        {
            var baseUrl = string.IsNullOrWhiteSpace(configuration.ApiBaseUrl)
                ? "http://192.168.10.198:28687"
                : configuration.ApiBaseUrl.Trim();

            if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
            {
                baseUrl += "/";
            }

            return new Uri(new Uri(baseUrl), path);
        }

        private static bool IsInscuraApiUrl(string url, PluginConfiguration configuration)
        {
            Uri assetUri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out assetUri))
            {
                return false;
            }

            var baseUrl = string.IsNullOrWhiteSpace(configuration.ApiBaseUrl)
                ? "http://192.168.10.198:28687"
                : configuration.ApiBaseUrl.Trim();

            Uri apiUri;
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out apiUri))
            {
                return false;
            }

            return string.Equals(assetUri.Scheme, apiUri.Scheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(assetUri.Host, apiUri.Host, StringComparison.OrdinalIgnoreCase)
                && assetUri.Port == apiUri.Port;
        }

        private static int ClampLimit(int limit)
        {
            if (limit <= 0)
            {
                return 10;
            }

            if (limit > 50)
            {
                return 50;
            }

            return limit;
        }

        private static int ClampTimeout(int seconds)
        {
            if (seconds < 3)
            {
                return 3;
            }

            if (seconds > 120)
            {
                return 120;
            }

            return seconds;
        }

        private static PluginConfiguration GetConfiguration()
        {
            return Plugin.Instance == null ? new PluginConfiguration() : Plugin.Instance.Configuration;
        }
    }
}
