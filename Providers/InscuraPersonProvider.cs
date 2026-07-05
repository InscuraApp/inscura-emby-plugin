using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugin.Inscura.Api;
using Emby.Plugin.Inscura.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugin.Inscura.Providers
{
    public class InscuraPersonProvider : IRemoteMetadataProvider<Person, PersonLookupInfo>, IHasOrder
    {
        private readonly InscuraApiClient _apiClient;
        private readonly ILogger _logger;

        public InscuraPersonProvider(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
        {
            _apiClient = new InscuraApiClient(httpClient, jsonSerializer, logManager.GetLogger("InscuraApiClient"));
            _logger = logManager.GetLogger("InscuraPersonProvider");
        }

        public string Name
        {
            get { return Plugin.PluginName; }
        }

        public int Order
        {
            get { return 0; }
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            if (!Configuration.EnablePersonProvider)
            {
                return new RemoteSearchResult[0];
            }

            var byId = await TryGetSearchResultByProviderId(searchInfo, cancellationToken).ConfigureAwait(false);
            if (byId != null)
            {
                return new[] { byId };
            }

            var queries = GetSearchQueries(searchInfo);
            if (queries.Count == 0)
            {
                return new RemoteSearchResult[0];
            }

            foreach (var query in queries)
            {
                var results = await _apiClient.SearchActorsAsync(query, Configuration.SearchLimit, cancellationToken).ConfigureAwait(false);
                if (results.Count > 0)
                {
                    return results.Select(item => InscuraMapping.ToActorSearchResult(item, Name, _apiClient)).ToArray();
                }
            }

            return new RemoteSearchResult[0];
        }

        public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Person>
            {
                Provider = Name,
                ResultLanguage = info.MetadataLanguage
            };

            if (!Configuration.EnablePersonProvider)
            {
                return result;
            }

            var detail = await ResolveActorAsync(info, cancellationToken).ConfigureAwait(false);
            if (detail == null)
            {
                return result;
            }

            result.HasMetadata = true;
            result.Item = InscuraMapping.ToPerson(detail);
            result.QueriedById = TryGetInscuraActorId(info.ProviderIds, out _);
            return result;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _apiClient.GetImageResponseAsync(url, cancellationToken);
        }

        private async Task<RemoteSearchResult?> TryGetSearchResultByProviderId(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            long id;
            if (!TryGetInscuraActorId(searchInfo.ProviderIds, out id))
            {
                return null;
            }

            var detail = await _apiClient.GetActorAsync(id, cancellationToken).ConfigureAwait(false);
            return detail == null ? null : InscuraMapping.ToActorSearchResult(detail, Name, _apiClient);
        }

        private async Task<ApiActorDetail?> ResolveActorAsync(PersonLookupInfo info, CancellationToken cancellationToken)
        {
            long id;
            if (TryGetInscuraActorId(info.ProviderIds, out id))
            {
                return await _apiClient.GetActorAsync(id, cancellationToken).ConfigureAwait(false);
            }

            var queries = GetSearchQueries(info);
            if (queries.Count == 0)
            {
                return null;
            }

            foreach (var query in queries)
            {
                var results = await _apiClient.SearchActorsAsync(query, 1, cancellationToken).ConfigureAwait(false);
                var first = results.FirstOrDefault();
                if (first != null)
                {
                    return await _apiClient.GetActorAsync(first.Id, cancellationToken).ConfigureAwait(false);
                }
            }

            return null;
        }

        private IReadOnlyList<string> GetSearchQueries(PersonLookupInfo info)
        {
            var queries = new List<string>();
            AddQuery(queries, info.Name);
            if (queries.Count == 0)
            {
                _logger.Debug("No Inscura search query can be derived for person lookup.");
            }

            return queries;
        }

        private static void AddQuery(ICollection<string> queries, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var query = value.Trim();
            if (!queries.Contains(query, StringComparer.OrdinalIgnoreCase))
            {
                queries.Add(query);
            }
        }

        private static bool TryGetInscuraActorId(IReadOnlyDictionary<string, string> providerIds, out long id)
        {
            id = 0;
            string value;
            return providerIds.TryGetValue(Plugin.PersonProviderId, out value)
                && long.TryParse(value, out id)
                && id > 0;
        }

        private static PluginConfiguration Configuration
        {
            get { return Plugin.Instance == null ? new PluginConfiguration() : Plugin.Instance.Configuration; }
        }
    }
}
