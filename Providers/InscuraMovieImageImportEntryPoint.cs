using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugin.Inscura.Api;
using Emby.Plugin.Inscura.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugin.Inscura.Providers
{
    public sealed class InscuraMovieImageImportEntryPoint : IServerEntryPoint
    {
        private static readonly ImageType[] ImportTypes =
        {
            ImageType.Primary,
            ImageType.Backdrop,
            ImageType.Thumb,
            ImageType.Banner,
            ImageType.Logo,
            ImageType.Art,
            ImageType.Disc
        };

        private readonly InscuraApiClient _apiClient;
        private readonly IProviderManager _providerManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly InscuraDirectoryService _directoryService = new InscuraDirectoryService();
        private readonly ConcurrentDictionary<long, byte> _running = new ConcurrentDictionary<long, byte>();
        private bool _disposed;

        public InscuraMovieImageImportEntryPoint(IProviderManager providerManager, ILibraryManager libraryManager, IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
        {
            _providerManager = providerManager;
            _libraryManager = libraryManager;
            _logger = logManager.GetLogger("InscuraMovieImageImport");
            _apiClient = new InscuraApiClient(httpClient, jsonSerializer, logManager.GetLogger("InscuraApiClient"));
        }

        public void Run()
        {
            _providerManager.RefreshCompleted += OnRefreshCompleted;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _providerManager.RefreshCompleted -= OnRefreshCompleted;
            _disposed = true;
        }

        private void OnRefreshCompleted(object? sender, GenericEventArgs<RefreshProgressInfo> args)
        {
            if (!(args.Argument.Item is Movie movie))
            {
                return;
            }

            _ = ImportImagesAsync(movie, CancellationToken.None);
        }

        private async Task ImportImagesAsync(Movie movie, CancellationToken cancellationToken)
        {
            if (!Configuration.EnableImageProvider || !Configuration.EnableAutomaticImageImport)
            {
                return;
            }

            if (!TryGetInscuraId(movie.ProviderIds, out var inscuraId))
            {
                return;
            }

            var itemId = movie.InternalId;
            if (!_running.TryAdd(itemId, 0))
            {
                return;
            }

            try
            {
                var detail = await _apiClient.GetMediaAsync(inscuraId, cancellationToken).ConfigureAwait(false);
                if (detail == null)
                {
                    return;
                }

                var libraryOptions = _libraryManager.GetLibraryOptions(movie);
                var images = InscuraMovieImages.GetRemoteImages(detail, _apiClient, Configuration, Plugin.PluginName);
                var savedCount = await SaveMissingImagesAsync(movie, libraryOptions, images, cancellationToken).ConfigureAwait(false);
                if (savedCount > 0)
                {
                    movie.UpdateToRepository(ItemUpdateType.ImageUpdate);
                    _logger.Info("Imported {0} missing Inscura image(s) for movie {1}.", savedCount, movie.Name);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Failed to import missing Inscura images after metadata refresh.", ex);
            }
            finally
            {
                _running.TryRemove(itemId, out _);
            }
        }

        private async Task<int> SaveMissingImagesAsync(Movie movie, LibraryOptions libraryOptions, IReadOnlyList<RemoteImageInfo> images, CancellationToken cancellationToken)
        {
            var savedCount = 0;
            foreach (var type in ImportTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var candidates = images.Where(image => image.Type == type && !string.IsNullOrWhiteSpace(image.Url)).ToArray();
                if (candidates.Length == 0)
                {
                    continue;
                }

                var existingCount = movie.GetImages(type).Count();
                var targetCount = GetTargetCount(movie, type, candidates.Length);
                for (var index = existingCount; index < targetCount; index++)
                {
                    var image = candidates[index];
                    await SaveImageAsync(movie, libraryOptions, image, index, cancellationToken).ConfigureAwait(false);
                    savedCount++;
                }
            }

            return savedCount;
        }

        private async Task SaveImageAsync(Movie movie, LibraryOptions libraryOptions, RemoteImageInfo image, int index, CancellationToken cancellationToken)
        {
            var imageIndex = movie.AllowsMultipleImages(image.Type) ? (int?)index : null;
            await _providerManager.SaveImage(
                movie,
                libraryOptions,
                image.Url,
                image.Type,
                imageIndex,
                new[] { movie.InternalId },
                _directoryService,
                true,
                cancellationToken).ConfigureAwait(false);
        }

        private static int GetTargetCount(Movie movie, ImageType type, int candidateCount)
        {
            if (type == ImageType.Backdrop)
            {
                return candidateCount;
            }

            return movie.AllowsMultipleImages(type) ? candidateCount : Math.Min(candidateCount, 1);
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
