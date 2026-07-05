using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Emby.Plugin.Inscura.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Emby.Plugin.Inscura.Providers
{
    internal static class InscuraMapping
    {
        private static readonly string[] RatingKeys = { "rating", "imdb_rating", "tmdb_rating", "themoviedb_rating" };
        private static readonly string[] CriticRatingKeys = { "critic_rating", "tomatometer", "rottentomatoes_rating" };
        private static readonly string[] OfficialRatingKeys = { "content_rating", "certification", "mpaa", "official_rating" };
        private static readonly string[] ReleaseDateKeys = { "release_date", "premiered" };
        private static readonly string[] OriginalTitleKeys = { "original_title", "originaltitle" };
        private static readonly string[] SortTitleKeys = { "sort_title", "sorttitle" };
        private static readonly string[] RuntimeKeys = { "runtime_minutes", "runtime" };

        public static RemoteSearchResult ToSearchResult(ApiMediaListItem item, string providerName, InscuraApiClient client)
        {
            var result = new RemoteSearchResult
            {
                Name = FirstNonEmpty(item.Title, item.Code, TrimExtension(item.FileName), item.RelativePath),
                OriginalTitle = FirstNonEmpty(item.Title, item.Code),
                ImageUrl = client.AddQueryTokenIfNeeded(GetFirstAssetUrl(item.Assets, "poster")),
                SearchProviderName = providerName,
                DisambiguationComment = BuildSearchDisambiguation(item)
            };

            result.ProviderIds[Plugin.ProviderId] = item.Id.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(item.Code))
            {
                result.ProviderIds[Plugin.CodeProviderId] = item.Code.Trim();
            }

            return result;
        }

        public static RemoteSearchResult ToActorSearchResult(ApiActorSearchItem item, string providerName, InscuraApiClient client)
        {
            var birthday = GetFirstSearchMeta(item, "birthday");
            var result = new RemoteSearchResult
            {
                Name = item.Name == null ? string.Empty : item.Name.Trim(),
                Overview = GetActorOverview(item),
                PremiereDate = ParseDate(birthday),
                ProductionYear = ParseYear(birthday),
                ImageUrl = client.AddQueryTokenIfNeeded(GetFirstAssetUrl(item.Assets, "avatar")),
                SearchProviderName = providerName
            };

            result.ProviderIds[Plugin.PersonProviderId] = item.Id.ToString(CultureInfo.InvariantCulture);
            return result;
        }

        public static RemoteSearchResult ToActorSearchResult(ApiActorDetail detail, string providerName, InscuraApiClient client)
        {
            var birthday = GetMeta(detail, "birthday");
            var result = new RemoteSearchResult
            {
                Name = detail.Name == null ? string.Empty : detail.Name.Trim(),
                Overview = GetActorOverview(detail),
                PremiereDate = ParseDate(birthday),
                ProductionYear = ParseYear(birthday),
                ImageUrl = client.AddQueryTokenIfNeeded(GetFirstAssetUrl(detail.Assets, "avatar")),
                SearchProviderName = providerName
            };

            result.ProviderIds[Plugin.PersonProviderId] = detail.Id.ToString(CultureInfo.InvariantCulture);
            return result;
        }

        public static Movie ToMovie(ApiMediaDetail detail)
        {
            var title = GetMeta(detail, "title");
            var code = GetMeta(detail, "code");
            var releaseDate = FirstMeta(detail, ReleaseDateKeys);
            var movie = new Movie
            {
                Name = FirstNonEmpty(title, code, TrimExtension(detail.FileName), detail.RelativePath),
                OriginalTitle = FirstNonEmpty(FirstMeta(detail, OriginalTitleKeys), title, code),
                SortName = FirstMeta(detail, SortTitleKeys),
                Overview = FirstMeta(detail, "description", "plot", "overview", "summary"),
                Tagline = FirstMeta(detail, "tagline"),
                ProductionYear = ParseYear(releaseDate),
                PremiereDate = ParseDate(releaseDate),
                OfficialRating = FirstMeta(detail, OfficialRatingKeys),
                CustomRating = FirstMeta(detail, OfficialRatingKeys),
                CommunityRating = ParseFloat(FirstMeta(detail, RatingKeys)),
                CriticRating = ParseCriticRating(FirstMeta(detail, CriticRatingKeys)),
                RunTimeTicks = GetRuntimeTicks(detail),
                Container = NormalizeContainer(detail.Ext),
                Size = detail.SizeBytes
            };

            if (detail.Width.HasValue && detail.Width.Value > 0)
            {
                movie.Width = detail.Width.Value;
            }

            if (detail.Height.HasValue && detail.Height.Value > 0)
            {
                movie.Height = detail.Height.Value;
            }

            if (detail.Bitrate.HasValue && detail.Bitrate.Value > 0 && detail.Bitrate.Value <= int.MaxValue)
            {
                movie.TotalBitrate = (int)detail.Bitrate.Value;
            }

            movie.ProviderIds[Plugin.ProviderId] = detail.Id.ToString(CultureInfo.InvariantCulture);
            AddProviderId(movie.ProviderIds, Plugin.CodeProviderId, code);
            AddProviderId(movie.ProviderIds, MetadataProviders.Imdb.ToString(), FirstMeta(detail, "imdb_id", "imdb"));
            AddProviderId(movie.ProviderIds, MetadataProviders.Tmdb.ToString(), FirstMeta(detail, "tmdb_id", "themoviedb_id", "tmdb"));
            AddProviderId(movie.ProviderIds, Plugin.WikidataProviderId, FirstMeta(detail, "wikidata_id", "wikidata"));

            movie.Genres = GetTermNames(detail, "genre").ToArray();
            movie.Studios = GetStudios(detail).ToArray();
            movie.ProductionLocations = GetProductionLocations(detail).ToArray();
            movie.Tags = GetTags(detail, code).ToArray();

            var collections = GetTermNames(detail, "collection").ToArray();
            if (collections.Length > 0)
            {
                movie.SetCollections(collections);
            }

            return movie;
        }

        public static Person ToPerson(ApiActorDetail detail)
        {
            var birthday = GetMeta(detail, "birthday");
            var person = new Person
            {
                Name = detail.Name == null ? string.Empty : detail.Name.Trim(),
                Overview = GetActorOverview(detail),
                PremiereDate = ParseDate(birthday),
                ProductionYear = ParseYear(birthday),
                ProductionLocations = GetActorLocations(detail).ToArray(),
                Tags = GetTermNames(detail, "actor_tag").ToArray()
            };

            person.ProviderIds[Plugin.PersonProviderId] = detail.Id.ToString(CultureInfo.InvariantCulture);
            return person;
        }

        public static IEnumerable<PersonInfo> GetPeople(ApiMediaDetail detail, InscuraApiClient client)
        {
            foreach (var credit in detail.Credits.Cast.OrderBy(credit => credit.SortOrder))
            {
                if (string.IsNullOrWhiteSpace(credit.ActorName))
                {
                    continue;
                }

                var person = new PersonInfo
                {
                    Name = credit.ActorName.Trim(),
                    Role = JoinRoleNames(credit.Roles),
                    Type = PersonType.Actor,
                    ImageUrl = client.AddQueryTokenIfNeeded(credit.ActorAvatar == null ? string.Empty : credit.ActorAvatar.Url)
                };
                person.ProviderIds[Plugin.PersonProviderId] = credit.ActorId.ToString(CultureInfo.InvariantCulture);
                yield return person;
            }

            foreach (var credit in detail.Credits.Crew.OrderBy(credit => credit.SortOrder))
            {
                if (string.IsNullOrWhiteSpace(credit.ActorName))
                {
                    continue;
                }

                var roles = credit.Roles.Select(role => role.Name).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()).ToArray();
                var type = MapPersonType(roles);
                if (!type.HasValue)
                {
                    continue;
                }

                var person = new PersonInfo
                {
                    Name = credit.ActorName.Trim(),
                    Role = string.Join(", ", roles),
                    Type = type.Value,
                    ImageUrl = client.AddQueryTokenIfNeeded(credit.ActorAvatar == null ? string.Empty : credit.ActorAvatar.Url)
                };
                person.ProviderIds[Plugin.PersonProviderId] = credit.ActorId.ToString(CultureInfo.InvariantCulture);
                yield return person;
            }
        }

        public static string[] GetYouTubeTrailers(ApiMediaDetail detail)
        {
            var urls = new List<string>();
            foreach (var asset in detail.Assets.Where(asset => string.Equals(asset.Kind, "trailer", StringComparison.OrdinalIgnoreCase)))
            {
                AddIfYouTube(urls, asset.RemoteUrl);
                AddIfYouTube(urls, asset.SourceUrl);
                AddIfYouTube(urls, asset.Url);
            }

            AddTrailerMetaIfYouTube(detail, urls);
            return urls.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public static MediaStream[] GetMediaStreams(ApiMediaDetail detail)
        {
            var streams = new List<MediaStream>();
            foreach (var video in detail.Streams.Video.OrderBy(stream => stream.StreamIndex))
            {
                streams.Add(new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Index = video.StreamIndex,
                    Codec = FirstNonEmpty(video.Codec),
                    Width = video.Width,
                    Height = video.Height,
                    PixelFormat = video.PixFmt,
                    AverageFrameRate = video.AvgFrameRate.HasValue ? (float?)video.AvgFrameRate.Value : null,
                    RealFrameRate = video.AvgFrameRate.HasValue ? (float?)video.AvgFrameRate.Value : null,
                    BitRate = video.BitRate,
                    BitDepth = video.BitDepth,
                    ColorPrimaries = video.ColorPrimaries,
                    ColorTransfer = video.ColorTrc,
                    ColorSpace = video.ColorSpace
                });
            }

            foreach (var audio in detail.Streams.Audio.OrderBy(stream => stream.StreamIndex))
            {
                streams.Add(new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Index = audio.StreamIndex,
                    Codec = FirstNonEmpty(audio.CodecLabel, audio.Codec),
                    Language = audio.Language,
                    Title = audio.Title,
                    Channels = audio.Channels,
                    SampleRate = audio.SampleRate,
                    BitRate = audio.BitRate,
                    BitDepth = audio.BitDepth,
                    IsDefault = audio.IsDefault,
                    IsForced = audio.IsForced
                });
            }

            foreach (var subtitle in detail.Streams.Subtitle.OrderBy(stream => stream.StreamIndex))
            {
                streams.Add(new MediaStream
                {
                    Type = MediaStreamType.Subtitle,
                    Index = subtitle.StreamIndex,
                    Codec = FirstNonEmpty(subtitle.CodecLabel, subtitle.Codec),
                    Language = subtitle.Language,
                    Title = subtitle.Title,
                    IsDefault = subtitle.IsDefault,
                    IsForced = subtitle.IsForced,
                    IsHearingImpaired = subtitle.IsHearingImpaired
                });
            }

            return streams.ToArray();
        }

        public static ImageType? MapImageType(ApiMediaAsset asset, bool includePreviewAsThumb, bool includeGalleryBackdrops)
        {
            if (string.IsNullOrWhiteSpace(asset.Kind))
            {
                return null;
            }

            switch (asset.Kind.Trim().ToLowerInvariant())
            {
                case "poster":
                    return ImageType.Primary;
                case "fanart":
                    return includeGalleryBackdrops ? ImageType.Backdrop : (ImageType?)null;
                case "screenshot":
                    return includeGalleryBackdrops ? ImageType.Screenshot : (ImageType?)null;
                case "keyart":
                case "clearart":
                    return ImageType.Art;
                case "landscape":
                    return ImageType.Thumb;
                case "preview":
                    return includePreviewAsThumb ? ImageType.Thumb : (ImageType?)null;
                case "banner":
                    return ImageType.Banner;
                case "clearlogo":
                    return ImageType.Logo;
                case "discart":
                    return ImageType.Disc;
                default:
                    return null;
            }
        }

        public static ImageType? MapActorImageType(ApiMediaAsset asset)
        {
            if (string.IsNullOrWhiteSpace(asset.Kind))
            {
                return null;
            }

            switch (asset.Kind.Trim().ToLowerInvariant())
            {
                case "avatar":
                case "photo":
                    return ImageType.Primary;
                default:
                    return null;
            }
        }

        public static IReadOnlyList<string> GetSearchQueries(string? path, string? name, Func<string, string?>? parseName)
        {
            var queries = new List<string>();
            var fileName = GetFileName(path);
            var fileNameWithoutExtension = TrimExtension(fileName);
            AddQuery(queries, fileNameWithoutExtension);
            AddQuery(queries, ExtractSearchCode(fileNameWithoutExtension));
            AddQuery(queries, fileName);
            AddQuery(queries, path);
            AddQuery(queries, ExtractSearchCode(name));

            if (!string.IsNullOrWhiteSpace(name) && parseName != null)
            {
                AddQuery(queries, parseName(name));
            }

            AddQuery(queries, name);
            return queries;
        }

        public static string GetMeta(ApiMediaDetail detail, string key)
        {
            return GetDictionaryValue(detail.Metas, key);
        }

        public static string GetMeta(ApiActorDetail detail, string key)
        {
            return GetDictionaryValue(detail.Metas, key);
        }

        public static string FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }

        private static string BuildSearchDisambiguation(ApiMediaListItem item)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(item.Code))
            {
                parts.Add(item.Code.Trim());
            }

            if (!string.IsNullOrWhiteSpace(item.RelativePath))
            {
                parts.Add(item.RelativePath.Trim());
            }
            else if (!string.IsNullOrWhiteSpace(item.FileName))
            {
                parts.Add(item.FileName.Trim());
            }

            if (item.DurationMs.HasValue && item.DurationMs.Value > 0)
            {
                parts.Add(TimeSpan.FromMilliseconds(item.DurationMs.Value).ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture));
            }

            return string.Join(" · ", parts);
        }

        private static string GetDictionaryValue(IDictionary<string, string?> values, string key)
        {
            foreach (var pair in values)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value == null ? string.Empty : pair.Value.Trim();
                }
            }

            return string.Empty;
        }

        private static string FirstMeta(ApiMediaDetail detail, params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = GetMeta(detail, key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string FirstMeta(ApiMediaDetail detail, IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                var value = GetMeta(detail, key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static IEnumerable<string> GetStudios(ApiMediaDetail detail)
        {
            foreach (var value in GetTermNames(detail, "studio", "label", "maker"))
            {
                yield return value;
            }

            foreach (var value in SplitValues(FirstMeta(detail, "studio", "maker", "label", "production_company", "production_companies")))
            {
                yield return value;
            }
        }

        private static IEnumerable<string> GetProductionLocations(ApiMediaDetail detail)
        {
            foreach (var value in GetTermNames(detail, "country", "location", "production_country"))
            {
                yield return value;
            }

            foreach (var value in SplitValues(FirstMeta(detail, "country", "production_country", "production_countries")))
            {
                yield return value;
            }
        }

        private static IEnumerable<string> GetActorLocations(ApiActorDetail detail)
        {
            foreach (var value in GetTermNames(detail, "nationality", "country"))
            {
                yield return value;
            }

            foreach (var value in SplitValues(FirstNonEmpty(GetMeta(detail, "nationality"), GetMeta(detail, "country"))))
            {
                yield return value;
            }
        }

        private static IEnumerable<string> GetTags(ApiMediaDetail detail, string code)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddTag(set, code);
            foreach (var value in GetTermNames(detail, "tag", "series"))
            {
                AddTag(set, value);
            }

            foreach (var value in SplitValues(FirstMeta(detail, "release_flags", "keywords")))
            {
                AddTag(set, value);
            }

            return set;
        }

        private static void AddTag(ISet<string> tags, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                tags.Add(value.Trim());
            }
        }

        private static IEnumerable<string> GetTermNames(ApiMediaDetail detail, params string[] names)
        {
            return GetTermNames(detail.Terms, names);
        }

        private static IEnumerable<string> GetTermNames(ApiActorDetail detail, params string[] names)
        {
            return GetTermNames(detail.Terms, names);
        }

        private static IEnumerable<string> GetTermNames(IDictionary<string, List<ApiTerm>> terms, params string[] names)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in names)
            {
                foreach (var pair in terms)
                {
                    if (!string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    foreach (var term in pair.Value)
                    {
                        if (!string.IsNullOrWhiteSpace(term.Name) && set.Add(term.Name.Trim()))
                        {
                            yield return term.Name.Trim();
                        }
                    }
                }
            }
        }

        private static string GetFirstSearchMeta(ApiActorSearchItem item, string key)
        {
            foreach (var pair in item.Metas)
            {
                if (!string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return pair.Value.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
            }

            return string.Empty;
        }

        private static string GetActorOverview(ApiActorSearchItem item)
        {
            return FirstNonEmpty(GetFirstSearchMeta(item, "description"), GetFirstSearchMeta(item, "简介")).Trim();
        }

        private static string GetActorOverview(ApiActorDetail detail)
        {
            return FirstNonEmpty(GetMeta(detail, "description"), GetMeta(detail, "简介")).Trim();
        }

        private static string GetFirstAssetUrl(IEnumerable<ApiMediaAsset> assets, string kind)
        {
            var asset = assets.FirstOrDefault(item => string.Equals(item.Kind, kind, StringComparison.OrdinalIgnoreCase));
            return asset == null ? string.Empty : asset.Url ?? string.Empty;
        }

        private static string TrimExtension(string? fileName)
        {
            return string.IsNullOrWhiteSpace(fileName) ? string.Empty : Path.GetFileNameWithoutExtension(fileName.Trim());
        }

        private static string GetFileName(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var normalized = path.Trim().Replace('\\', '/');
            var separatorIndex = normalized.LastIndexOf('/');
            return separatorIndex >= 0 ? normalized.Substring(separatorIndex + 1) : normalized;
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

        private static string ExtractSearchCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var match = Regex.Match(value, "[A-Za-z]{2,10}[-_ ]?\\d{2,8}", RegexOptions.Compiled);
            return match.Success
                ? match.Value.Replace("_", "-").Replace(" ", "-").ToUpperInvariant()
                : string.Empty;
        }

        private static int? ParseYear(string? value)
        {
            var date = ParseDate(value);
            return date.HasValue ? (int?)date.Value.Year : null;
        }

        private static DateTimeOffset? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            DateTimeOffset date;
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date))
            {
                return date.ToUniversalTime();
            }

            return null;
        }

        private static float? ParseFloat(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            float result;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ? (float?)result : null;
        }

        private static float? ParseCriticRating(string? value)
        {
            var parsed = ParseFloat(value);
            if (!parsed.HasValue)
            {
                return null;
            }

            return parsed.Value <= 10 ? parsed.Value * 10 : parsed.Value;
        }

        private static long? GetRuntimeTicks(ApiMediaDetail detail)
        {
            var runtime = FirstMeta(detail, RuntimeKeys);
            double minutes;
            if (!string.IsNullOrWhiteSpace(runtime) && double.TryParse(runtime, NumberStyles.Float, CultureInfo.InvariantCulture, out minutes) && minutes > 0)
            {
                return TimeSpan.FromMinutes(minutes).Ticks;
            }

            return detail.DurationMs.HasValue && detail.DurationMs.Value > 0
                ? detail.DurationMs.Value * TimeSpan.TicksPerMillisecond
                : (long?)null;
        }

        private static string NormalizeContainer(string? ext)
        {
            return string.IsNullOrWhiteSpace(ext) ? string.Empty : ext.Trim().TrimStart('.').ToLowerInvariant();
        }

        private static void AddProviderId(ProviderIdDictionary providerIds, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                providerIds[key] = value.Trim();
            }
        }

        private static IEnumerable<string> SplitValues(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield break;
            }

            foreach (var part in value.Split(new[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    yield return trimmed;
                }
            }
        }

        private static string JoinRoleNames(IEnumerable<ApiRole> roles)
        {
            return string.Join(", ", roles.Select(role => role.Name).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()));
        }

        private static PersonType? MapPersonType(IEnumerable<string> roles)
        {
            foreach (var role in roles)
            {
                var normalized = role.Trim().ToLowerInvariant();
                if (normalized.IndexOf("director", StringComparison.Ordinal) >= 0 || normalized.IndexOf("导演", StringComparison.Ordinal) >= 0)
                {
                    return PersonType.Director;
                }

                if (normalized.IndexOf("writer", StringComparison.Ordinal) >= 0 || normalized.IndexOf("screenplay", StringComparison.Ordinal) >= 0 || normalized.IndexOf("编剧", StringComparison.Ordinal) >= 0)
                {
                    return PersonType.Writer;
                }

                if (normalized.IndexOf("producer", StringComparison.Ordinal) >= 0 || normalized.IndexOf("制片", StringComparison.Ordinal) >= 0)
                {
                    return PersonType.Producer;
                }

                if (normalized.IndexOf("composer", StringComparison.Ordinal) >= 0 || normalized.IndexOf("music", StringComparison.Ordinal) >= 0 || normalized.IndexOf("音乐", StringComparison.Ordinal) >= 0)
                {
                    return PersonType.Composer;
                }
            }

            return null;
        }

        private static void AddTrailerMetaIfYouTube(ApiMediaDetail detail, ICollection<string> urls)
        {
            var trailer = GetMeta(detail, "trailer");
            if (string.IsNullOrWhiteSpace(trailer))
            {
                return;
            }

            foreach (Match match in Regex.Matches(trailer, "https?://[^\\\"'\\s<>]+"))
            {
                AddIfYouTube(urls, match.Value);
            }
        }

        private static void AddIfYouTube(ICollection<string> urls, string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            Uri uri;
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out uri))
            {
                return;
            }

            if (uri.Host.IndexOf("youtube.com", StringComparison.OrdinalIgnoreCase) >= 0 || uri.Host.IndexOf("youtu.be", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                urls.Add(url.Trim());
            }
        }
    }
}
