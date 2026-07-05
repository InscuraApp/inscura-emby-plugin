using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Plugin.Inscura.Api;
using Emby.Plugin.Inscura.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Emby.Plugin.Inscura.Providers
{
    internal static class InscuraMovieImages
    {
        public static IReadOnlyList<RemoteImageInfo> GetRemoteImages(ApiMediaDetail detail, InscuraApiClient client, PluginConfiguration configuration, string providerName)
        {
            var images = new List<RemoteImageInfo>();

            AddImages(images, detail, client, providerName, ImageType.Primary, GetAssets(detail, "poster"));

            if (configuration.IncludeGalleryBackdrops)
            {
                AddImages(images, detail, client, providerName, ImageType.Backdrop, GetBackdropAssets(detail));
            }

            AddImages(images, detail, client, providerName, ImageType.Thumb, GetAssets(detail, "landscape"));
            if (configuration.IncludePreviewAsThumb)
            {
                AddImages(images, detail, client, providerName, ImageType.Thumb, GetAssets(detail, "preview"));
            }

            AddImages(images, detail, client, providerName, ImageType.Banner, GetAssets(detail, "banner"));
            AddImages(images, detail, client, providerName, ImageType.Logo, GetAssets(detail, "clearlogo"));
            AddImages(images, detail, client, providerName, ImageType.Art, GetAssets(detail, "clearart").Concat(GetAssets(detail, "keyart")));
            AddImages(images, detail, client, providerName, ImageType.Disc, GetAssets(detail, "discart"));

            return images;
        }

        public static IReadOnlyList<RemoteImageInfo> GetRemoteImages(ApiMediaDetail detail, InscuraApiClient client, PluginConfiguration configuration, string providerName, ImageType imageType)
        {
            return GetRemoteImages(detail, client, configuration, providerName)
                .Where(image => image.Type == imageType)
                .ToArray();
        }

        private static IEnumerable<ApiMediaAsset> GetBackdropAssets(ApiMediaDetail detail)
        {
            var fanart = GetAssets(detail, "fanart").ToList();
            if (fanart.Count > 0)
            {
                yield return fanart[0];
            }

            foreach (var asset in GetAssets(detail, "preview"))
            {
                yield return asset;
            }

            for (var index = 1; index < fanart.Count; index++)
            {
                yield return fanart[index];
            }

            if (fanart.Count == 0)
            {
                foreach (var asset in GetAssets(detail, "screenshot"))
                {
                    yield return asset;
                }
            }
        }

        private static IEnumerable<ApiMediaAsset> GetAssets(ApiMediaDetail detail, string kind)
        {
            return detail.Assets.Where(asset => string.Equals(asset.Kind, kind, StringComparison.OrdinalIgnoreCase));
        }

        private static void AddImages(ICollection<RemoteImageInfo> images, ApiMediaDetail detail, InscuraApiClient client, string providerName, ImageType type, IEnumerable<ApiMediaAsset> assets)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var asset in assets)
            {
                if (string.IsNullOrWhiteSpace(asset.Url))
                {
                    continue;
                }

                var key = GetAssetKey(asset);
                if (!string.IsNullOrWhiteSpace(key) && !seen.Add(key))
                {
                    continue;
                }

                var url = client.AddQueryTokenIfNeeded(asset.Url);
                images.Add(new RemoteImageInfo
                {
                    ProviderName = providerName,
                    Url = url,
                    ThumbnailUrl = url,
                    Type = type,
                    Language = string.Empty,
                    Width = ShouldUseMediaSize(type) ? detail.Width : null,
                    Height = ShouldUseMediaSize(type) ? detail.Height : null
                });
            }
        }

        private static bool ShouldUseMediaSize(ImageType type)
        {
            return type == ImageType.Backdrop || type == ImageType.Thumb || type == ImageType.Screenshot;
        }

        private static string GetAssetKey(ApiMediaAsset asset)
        {
            return FirstNonEmpty(asset.RemoteUrl, asset.SourceUrl, asset.Url).Trim();
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }
    }
}
