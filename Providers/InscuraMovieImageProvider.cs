using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugin.Inscura.Api;
using Emby.Plugin.Inscura.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugin.Inscura.Providers
{
    public class InscuraMovieImageProvider : IRemoteImageProvider, IRemoteImageProviderWithOptions, IHasOrder
    {
        private readonly InscuraApiClient _apiClient;

        public InscuraMovieImageProvider(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
        {
            _apiClient = new InscuraApiClient(httpClient, jsonSerializer, logManager.GetLogger("InscuraApiClient"));
        }

        public string Name
        {
            get { return Plugin.PluginName; }
        }

        public int Order
        {
            get { return 0; }
        }

        public bool Supports(BaseItem item)
        {
            return item is Movie && Configuration.EnableImageProvider;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[]
            {
                ImageType.Primary,
                ImageType.Backdrop,
                ImageType.Screenshot,
                ImageType.Thumb,
                ImageType.Banner,
                ImageType.Logo,
                ImageType.Art,
                ImageType.Disc
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            if (!Configuration.EnableImageProvider || !(item is Movie))
            {
                return new RemoteImageInfo[0];
            }

            var movie = (Movie)item;
            var detail = await ResolveDetailAsync(movie, cancellationToken).ConfigureAwait(false);
            if (detail == null)
            {
                return new RemoteImageInfo[0];
            }

            return InscuraMovieImages.GetRemoteImages(detail, _apiClient, Configuration, Name);
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(RemoteImageFetchOptions options, CancellationToken cancellationToken)
        {
            if (!Configuration.EnableImageProvider || !(options.Item is Movie))
            {
                return new RemoteImageInfo[0];
            }

            var movie = (Movie)options.Item;
            var detail = await ResolveDetailAsync(movie, cancellationToken).ConfigureAwait(false);
            if (detail == null)
            {
                return new RemoteImageInfo[0];
            }

            return InscuraMovieImages.GetRemoteImages(detail, _apiClient, Configuration, Name);
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _apiClient.GetImageResponseAsync(url, cancellationToken);
        }

        private async Task<ApiMediaDetail?> ResolveDetailAsync(Movie movie, CancellationToken cancellationToken)
        {
            string value;
            long id;
            if (movie.ProviderIds.TryGetValue(Plugin.ProviderId, out value) && long.TryParse(value, out id) && id > 0)
            {
                return await _apiClient.GetMediaAsync(id, cancellationToken).ConfigureAwait(false);
            }

            foreach (var query in InscuraMapping.GetSearchQueries(movie.Path, movie.Name, null))
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

        private static PluginConfiguration Configuration
        {
            get { return Plugin.Instance == null ? new PluginConfiguration() : Plugin.Instance.Configuration; }
        }
    }
}
