using MediaBrowser.Model.Plugins;

namespace Emby.Plugin.Inscura.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            ApiBaseUrl = "http://192.168.10.198:28687";
            ApiToken = string.Empty;
            RequestTimeoutSeconds = 15;
            SearchLimit = 10;
            EnableMetadataProvider = true;
            EnableImageProvider = true;
            EnableAutomaticImageImport = true;
            EnablePersonProvider = true;
            EnableYouTubeTrailers = true;
            IncludePreviewAsThumb = true;
            IncludeGalleryBackdrops = true;
            IncludeMediaStreams = true;
        }

        public string ApiBaseUrl { get; set; }

        public string ApiToken { get; set; }

        public int RequestTimeoutSeconds { get; set; }

        public int SearchLimit { get; set; }

        public bool EnableMetadataProvider { get; set; }

        public bool EnableImageProvider { get; set; }

        public bool EnableAutomaticImageImport { get; set; }

        public bool EnablePersonProvider { get; set; }

        public bool EnableYouTubeTrailers { get; set; }

        public bool IncludePreviewAsThumb { get; set; }

        public bool IncludeGalleryBackdrops { get; set; }

        public bool IncludeMediaStreams { get; set; }
    }
}
