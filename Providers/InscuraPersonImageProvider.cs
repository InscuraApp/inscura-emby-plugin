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
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugin.Inscura.Providers
{
    public class InscuraPersonImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly InscuraApiClient _apiClient;

        public InscuraPersonImageProvider(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
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
            return item is Person && Configuration.EnablePersonProvider && Configuration.EnableImageProvider;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            if (!Configuration.EnablePersonProvider || !Configuration.EnableImageProvider || !(item is Person))
            {
                return new RemoteImageInfo[0];
            }

            var person = (Person)item;
            var detail = await ResolveDetailAsync(person, cancellationToken).ConfigureAwait(false);
            if (detail == null)
            {
                return new RemoteImageInfo[0];
            }

            return detail.Assets
                .Select(ToRemoteImage)
                .Where(image => image != null)
                .Cast<RemoteImageInfo>()
                .ToArray();
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _apiClient.GetImageResponseAsync(url, cancellationToken);
        }

        private async Task<ApiActorDetail?> ResolveDetailAsync(Person person, CancellationToken cancellationToken)
        {
            string value;
            long id;
            if (person.ProviderIds.TryGetValue(Plugin.PersonProviderId, out value) && long.TryParse(value, out id) && id > 0)
            {
                return await _apiClient.GetActorAsync(id, cancellationToken).ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(person.Name))
            {
                return null;
            }

            var results = await _apiClient.SearchActorsAsync(person.Name, 1, cancellationToken).ConfigureAwait(false);
            var first = results.FirstOrDefault();
            return first == null ? null : await _apiClient.GetActorAsync(first.Id, cancellationToken).ConfigureAwait(false);
        }

        private RemoteImageInfo? ToRemoteImage(ApiMediaAsset asset)
        {
            var type = InscuraMapping.MapActorImageType(asset);
            if (!type.HasValue || string.IsNullOrWhiteSpace(asset.Url))
            {
                return null;
            }

            return new RemoteImageInfo
            {
                ProviderName = Name,
                Url = _apiClient.AddQueryTokenIfNeeded(asset.Url),
                ThumbnailUrl = _apiClient.AddQueryTokenIfNeeded(asset.Url),
                Type = type.Value,
                Language = string.Empty
            };
        }

        private static PluginConfiguration Configuration
        {
            get { return Plugin.Instance == null ? new PluginConfiguration() : Plugin.Instance.Configuration; }
        }
    }
}
