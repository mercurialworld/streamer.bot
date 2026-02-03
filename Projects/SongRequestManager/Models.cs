using System;
using System.Collections.Generic;

public class SongLinkResponse
{
    public string EntityUniqueId { get; set; }
    public string UserCountry { get; set; }
    public string PageUrl { get; set; }
    public Dictionary<string, PlatformEntry> LinksByPlatform { get; set; }
    public Dictionary<string, PlatformEntityEntry> EntitiesByUniqueId { get; set; }
}

public class PlatformEntry
{
    public string EntityUniqueId { get; set; }
    public string Url { get; set; }
    public string NativeAppUriMobile { get; set; } = String.Empty;
    public string NativeAppUriDesktop { get; set; } = String.Empty;
}

public class PlatformEntityEntry
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Title { get; set; }
    public string ArtistName { get; set; }
    public string ThumbnailUrl { get; set; }
    public int ThumbnailWidth { get; set; }
    public int ThumbnailHeight { get; set; }
    public string ApiProvider { get; set; }
    public string[] Platforms { get; set; }
}

public class CiderPlayNextBody
{
    public string Type { get; set; }
    public string Id { get; set; }
}