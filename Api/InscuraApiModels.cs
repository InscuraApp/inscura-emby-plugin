using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.Plugin.Inscura.Api
{
    [DataContract]
    public class ApiEnvelope<T>
    {
        [DataMember(Name = "data")]
        public T? Data { get; set; }
    }

    [DataContract]
    public class ApiLibrarySummary
    {
        [DataMember(Name = "libraryId")]
        public string? LibraryId { get; set; }

        [DataMember(Name = "libraryName")]
        public string? LibraryName { get; set; }

        [DataMember(Name = "baseUrl")]
        public string? BaseUrl { get; set; }
    }

    [DataContract]
    public class ApiMediaListItem
    {
        [DataMember(Name = "id")]
        public long Id { get; set; }

        [DataMember(Name = "durationMs")]
        public long? DurationMs { get; set; }

        [DataMember(Name = "width")]
        public int? Width { get; set; }

        [DataMember(Name = "height")]
        public int? Height { get; set; }

        [DataMember(Name = "title")]
        public string? Title { get; set; }

        [DataMember(Name = "code")]
        public string? Code { get; set; }

        [DataMember(Name = "rating")]
        public double? Rating { get; set; }

        [DataMember(Name = "relativePath")]
        public string? RelativePath { get; set; }

        [DataMember(Name = "fileName")]
        public string? FileName { get; set; }

        [DataMember(Name = "assets")]
        public List<ApiMediaAsset> Assets { get; set; } = new List<ApiMediaAsset>();
    }

    [DataContract]
    public class ApiMediaDetail
    {
        [DataMember(Name = "id")]
        public long Id { get; set; }

        [DataMember(Name = "sourceType")]
        public string? SourceType { get; set; }

        [DataMember(Name = "ext")]
        public string? Ext { get; set; }

        [DataMember(Name = "sizeBytes")]
        public long SizeBytes { get; set; }

        [DataMember(Name = "createdTime")]
        public long? CreatedTime { get; set; }

        [DataMember(Name = "modifiedTime")]
        public long? ModifiedTime { get; set; }

        [DataMember(Name = "durationMs")]
        public long? DurationMs { get; set; }

        [DataMember(Name = "bitrate")]
        public long? Bitrate { get; set; }

        [DataMember(Name = "avgFps")]
        public double? AvgFps { get; set; }

        [DataMember(Name = "width")]
        public int? Width { get; set; }

        [DataMember(Name = "height")]
        public int? Height { get; set; }

        [DataMember(Name = "codecVideo")]
        public string? CodecVideo { get; set; }

        [DataMember(Name = "codecAudio")]
        public string? CodecAudio { get; set; }

        [DataMember(Name = "relativePath")]
        public string? RelativePath { get; set; }

        [DataMember(Name = "fileName")]
        public string? FileName { get; set; }

        [DataMember(Name = "metas")]
        public Dictionary<string, string?> Metas { get; set; } = new Dictionary<string, string?>();

        [DataMember(Name = "terms")]
        public Dictionary<string, List<ApiTerm>> Terms { get; set; } = new Dictionary<string, List<ApiTerm>>();

        [DataMember(Name = "credits")]
        public ApiCredits Credits { get; set; } = new ApiCredits();

        [DataMember(Name = "streams")]
        public ApiStreams Streams { get; set; } = new ApiStreams();

        [DataMember(Name = "assets")]
        public List<ApiMediaAsset> Assets { get; set; } = new List<ApiMediaAsset>();
    }

    [DataContract]
    public class ApiMediaAsset
    {
        [DataMember(Name = "kind")]
        public string? Kind { get; set; }

        [DataMember(Name = "index")]
        public int Index { get; set; }

        [DataMember(Name = "url")]
        public string? Url { get; set; }

        [DataMember(Name = "sourceUrl")]
        public string? SourceUrl { get; set; }

        [DataMember(Name = "remoteUrl")]
        public string? RemoteUrl { get; set; }
    }

    [DataContract]
    public class ApiTerm
    {
        [DataMember(Name = "name")]
        public string? Name { get; set; }

        [DataMember(Name = "typeName")]
        public string? TypeName { get; set; }

        [DataMember(Name = "description")]
        public string? Description { get; set; }
    }

    [DataContract]
    public class ApiCredits
    {
        [DataMember(Name = "cast")]
        public List<ApiCredit> Cast { get; set; } = new List<ApiCredit>();

        [DataMember(Name = "crew")]
        public List<ApiCredit> Crew { get; set; } = new List<ApiCredit>();
    }

    [DataContract]
    public class ApiCredit
    {
        [DataMember(Name = "actorId")]
        public long ActorId { get; set; }

        [DataMember(Name = "actorName")]
        public string? ActorName { get; set; }

        [DataMember(Name = "creditType")]
        public string? CreditType { get; set; }

        [DataMember(Name = "sortOrder")]
        public int SortOrder { get; set; }

        [DataMember(Name = "roles")]
        public List<ApiRole> Roles { get; set; } = new List<ApiRole>();

        [DataMember(Name = "actorAvatar")]
        public ApiMediaAsset? ActorAvatar { get; set; }
    }

    [DataContract]
    public class ApiRole
    {
        [DataMember(Name = "name")]
        public string? Name { get; set; }
    }

    [DataContract]
    public class ApiStreams
    {
        [DataMember(Name = "video")]
        public List<ApiVideoStream> Video { get; set; } = new List<ApiVideoStream>();

        [DataMember(Name = "audio")]
        public List<ApiAudioStream> Audio { get; set; } = new List<ApiAudioStream>();

        [DataMember(Name = "subtitle")]
        public List<ApiSubtitleStream> Subtitle { get; set; } = new List<ApiSubtitleStream>();
    }

    [DataContract]
    public class ApiVideoStream
    {
        [DataMember(Name = "streamIndex")]
        public int StreamIndex { get; set; }

        [DataMember(Name = "codec")]
        public string? Codec { get; set; }

        [DataMember(Name = "width")]
        public int? Width { get; set; }

        [DataMember(Name = "height")]
        public int? Height { get; set; }

        [DataMember(Name = "pixFmt")]
        public string? PixFmt { get; set; }

        [DataMember(Name = "avgFrameRate")]
        public double? AvgFrameRate { get; set; }

        [DataMember(Name = "bitRate")]
        public int? BitRate { get; set; }

        [DataMember(Name = "bitDepth")]
        public int? BitDepth { get; set; }

        [DataMember(Name = "colorPrimaries")]
        public string? ColorPrimaries { get; set; }

        [DataMember(Name = "colorTrc")]
        public string? ColorTrc { get; set; }

        [DataMember(Name = "colorSpace")]
        public string? ColorSpace { get; set; }

        [DataMember(Name = "hdrFormat")]
        public string? HdrFormat { get; set; }
    }

    [DataContract]
    public class ApiAudioStream
    {
        [DataMember(Name = "streamIndex")]
        public int StreamIndex { get; set; }

        [DataMember(Name = "codec")]
        public string? Codec { get; set; }

        [DataMember(Name = "codecLabel")]
        public string? CodecLabel { get; set; }

        [DataMember(Name = "channels")]
        public int? Channels { get; set; }

        [DataMember(Name = "sampleRate")]
        public int? SampleRate { get; set; }

        [DataMember(Name = "bitRate")]
        public int? BitRate { get; set; }

        [DataMember(Name = "bitDepth")]
        public int? BitDepth { get; set; }

        [DataMember(Name = "language")]
        public string? Language { get; set; }

        [DataMember(Name = "title")]
        public string? Title { get; set; }

        [DataMember(Name = "isDefault")]
        public bool IsDefault { get; set; }

        [DataMember(Name = "isForced")]
        public bool IsForced { get; set; }
    }

    [DataContract]
    public class ApiSubtitleStream
    {
        [DataMember(Name = "streamIndex")]
        public int StreamIndex { get; set; }

        [DataMember(Name = "codec")]
        public string? Codec { get; set; }

        [DataMember(Name = "codecLabel")]
        public string? CodecLabel { get; set; }

        [DataMember(Name = "language")]
        public string? Language { get; set; }

        [DataMember(Name = "title")]
        public string? Title { get; set; }

        [DataMember(Name = "isDefault")]
        public bool IsDefault { get; set; }

        [DataMember(Name = "isForced")]
        public bool IsForced { get; set; }

        [DataMember(Name = "isHearingImpaired")]
        public bool IsHearingImpaired { get; set; }
    }

    [DataContract]
    public class ApiActorSearchItem
    {
        [DataMember(Name = "id")]
        public long Id { get; set; }

        [DataMember(Name = "name")]
        public string? Name { get; set; }

        [DataMember(Name = "metas")]
        public Dictionary<string, List<string>> Metas { get; set; } = new Dictionary<string, List<string>>();

        [DataMember(Name = "assets")]
        public List<ApiMediaAsset> Assets { get; set; } = new List<ApiMediaAsset>();
    }

    [DataContract]
    public class ApiActorDetail
    {
        [DataMember(Name = "id")]
        public long Id { get; set; }

        [DataMember(Name = "name")]
        public string? Name { get; set; }

        [DataMember(Name = "metas")]
        public Dictionary<string, string?> Metas { get; set; } = new Dictionary<string, string?>();

        [DataMember(Name = "terms")]
        public Dictionary<string, List<ApiTerm>> Terms { get; set; } = new Dictionary<string, List<ApiTerm>>();

        [DataMember(Name = "assets")]
        public List<ApiMediaAsset> Assets { get; set; } = new List<ApiMediaAsset>();
    }
}
