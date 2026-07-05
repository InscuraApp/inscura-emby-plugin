using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Plugin.Inscura.Providers
{
    public class InscuraMovieExternalId : IExternalId
    {
        public string Name
        {
            get { return Plugin.PluginName; }
        }

        public string Key
        {
            get { return Plugin.ProviderId; }
        }

        public string UrlFormatString
        {
            get { return null; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Movie;
        }
    }

    public class InscuraCodeExternalId : IExternalId
    {
        public string Name
        {
            get { return "Inscura Code"; }
        }

        public string Key
        {
            get { return Plugin.CodeProviderId; }
        }

        public string UrlFormatString
        {
            get { return null; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Movie;
        }
    }

    public class InscuraPersonExternalId : IExternalId
    {
        public string Name
        {
            get { return "Inscura Actor"; }
        }

        public string Key
        {
            get { return Plugin.PersonProviderId; }
        }

        public string UrlFormatString
        {
            get { return null; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Person;
        }
    }
}
