using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugin.Inscura.Api;
using Emby.Plugin.Inscura.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugin.Inscura.Providers
{
    public class InscuraMovieProvider : IRemoteMetadataProvider<Movie, MovieInfo>, IHasOrder
    {
        private readonly InscuraApiClient _apiClient;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;

        public InscuraMovieProvider(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILibraryManager libraryManager, ILogManager logManager)
        {
            _apiClient = new InscuraApiClient(httpClient, jsonSerializer, logManager.GetLogger("InscuraApiClient"));
            _libraryManager = libraryManager;
            _logger = logManager.GetLogger("InscuraMovieProvider");
        }

        public string Name
        {
            get { return Plugin.PluginName; }
        }

        public int Order
        {
            get { return 0; }
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            if (!Configuration.EnableMetadataProvider)
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
                var results = await _apiClient.SearchMediaAsync(query, Configuration.SearchLimit, cancellationToken).ConfigureAwait(false);
                if (results.Count > 0)
                {
                    return results.Select(item => InscuraMapping.ToSearchResult(item, Name, _apiClient)).ToArray();
                }
            }

            return new RemoteSearchResult[0];
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>
            {
                Provider = Name,
                ResultLanguage = info.MetadataLanguage
            };

            if (!Configuration.EnableMetadataProvider)
            {
                return result;
            }

            var detail = await ResolveMediaAsync(info, cancellationToken).ConfigureAwait(false);
            if (detail == null)
            {
                return result;
            }

            var movie = InscuraMapping.ToMovie(detail);
            if (Configuration.EnableYouTubeTrailers)
            {
                movie.RemoteTrailers = InscuraMapping.GetYouTubeTrailers(detail);
            }

            result.HasMetadata = true;
            result.QueriedById = TryGetInscuraId(info.ProviderIds, out _);
            result.SearchImageUrl = _apiClient.AddQueryTokenIfNeeded(detail.Assets.FirstOrDefault(asset => string.Equals(asset.Kind, "poster", StringComparison.OrdinalIgnoreCase))?.Url);
            result.Item = movie;
            result.People = InscuraMapping.GetPeople(detail, _apiClient).ToList();
            if (Configuration.IncludeMediaStreams)
            {
                result.MediaStreams = InscuraMapping.GetMediaStreams(detail);
            }

            return result;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _apiClient.GetImageResponseAsync(url, cancellationToken);
        }

        private async Task<RemoteSearchResult?> TryGetSearchResultByProviderId(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            long id;
            if (!TryGetInscuraId(searchInfo.ProviderIds, out id))
            {
                return null;
            }

            var detail = await _apiClient.GetMediaAsync(id, cancellationToken).ConfigureAwait(false);
            if (detail == null)
            {
                return null;
            }

            var item = new ApiMediaListItem
            {
                Id = detail.Id,
                Title = InscuraMapping.GetMeta(detail, "title"),
                Code = InscuraMapping.GetMeta(detail, "code"),
                DurationMs = detail.DurationMs,
                Width = detail.Width,
                Height = detail.Height,
                FileName = detail.FileName,
                RelativePath = detail.RelativePath,
                Assets = detail.Assets
            };
            return InscuraMapping.ToSearchResult(item, Name, _apiClient);
        }

        private async Task<ApiMediaDetail?> ResolveMediaAsync(MovieInfo info, CancellationToken cancellationToken)
        {
            long id;
            if (TryGetInscuraId(info.ProviderIds, out id))
            {
                return await _apiClient.GetMediaAsync(id, cancellationToken).ConfigureAwait(false);
            }

            var queries = GetSearchQueries(info);
            if (queries.Count == 0)
            {
                return null;
            }

            foreach (var query in queries)
            {
                var results = await _apiClient.SearchMediaAsync(query, 1, cancellationToken).ConfigureAwait(false);
                var first = results.FirstOrDefault();
                if (first != null)
                {
                    return await _apiClient.GetMediaAsync(first.Id, cancellationToken).ConfigureAwait(false);
                }
            }

            return null;
        }

        private IReadOnlyList<string> GetSearchQueries(MovieInfo info)
        {
            var queries = InscuraMapping.GetSearchQueries(info.Path, info.Name, name => _libraryManager.ParseName(name).Name);
            if (queries.Count == 0)
            {
                _logger.Debug("No Inscura search query can be derived for movie lookup.");
            }

            return queries;
        }

        private static bool TryGetInscuraId(IReadOnlyDictionary<string, string> providerIds, out long id)
        {
            id = 0;
            string value;
            return providerIds.TryGetValue(Plugin.ProviderId, out value)
                && long.TryParse(value, out id)
                && id > 0;
        }

        private static PluginConfiguration Configuration
        {
            get { return Plugin.Instance == null ? new PluginConfiguration() : Plugin.Instance.Configuration; }
        }
    }
}
