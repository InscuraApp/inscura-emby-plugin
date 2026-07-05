using System;
using System.Collections.Generic;
using Emby.Plugin.Inscura.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugin.Inscura
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public const string PluginName = "Inscura";
        public const string ProviderId = "Inscura";
        public const string PersonProviderId = "InscuraActor";
        public const string CodeProviderId = "InscuraCode";
        public const string WikidataProviderId = "Wikidata";

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static Plugin? Instance { get; private set; }

        public override string Name
        {
            get { return PluginName; }
        }

        public override string Description
        {
            get { return "Imports movie metadata, people, artwork, streams, and YouTube trailers from the Inscura local API."; }
        }

        public override Guid Id
        {
            get { return Guid.Parse("477ACB51-8E08-4664-8F1E-F56391796CF1"); }
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = PluginName,
                    DisplayName = PluginName,
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html",
                    IsMainConfigPage = true
                },
                new PluginPageInfo
                {
                    Name = "InscuraConfigurationPageJs",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.js"
                }
            };
        }
    }
}
