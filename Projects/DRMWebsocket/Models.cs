public class DRMWebsocketMessage
{
    public long Timestamp { get; set; }
    public string EventType { get; set; }
    public object Data { get; set; }
}

public class DRMSongData
{
    public string BsrKey { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public bool IsWip { get; set; }
    public string User { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool CensorTitle { get; set; }
    public string SubTitle { get; set; } = string.Empty;
    public bool CensorSubTitle { get; set; }
    public string Artist { get; set; } = string.Empty;
    public bool CensorArtist { get; set; }
    public string Mapper { get; set; } = string.Empty;
    public bool CensorMapper { get; set; }
    public bool MetadataHasSplicedCensor { get; set; }
    public uint Duration { get; set; }
    public uint[] Votes { get; set; }
    public float Rating { get; set; }
    public uint UploadTime { get; set; }
    public uint LastUpdated { get; set; }
    public bool Automapped { get; set; }
    public bool ScoreSaberRanked { get; set; }
    public bool BeatLeaderRanked { get; set; }
    public bool Curated { get; set; }
    public string CuratorName { get; set; } = string.Empty;
    public string[] Playlists { get; set; }
    public int VoteStatus { get; set; }
    public bool UsesChroma { get; set; }
    public bool UsesCinema { get; set; }
    public bool UsesMappingExtensions { get; set; }
    public bool UsesNoodleExtensions { get; set; }
    public bool UsesVivify { get; set; }
    public bool DataIsFromLocalMap { get; set; }
    public bool DataIsFromLocalCache { get; set; }
    public bool DataIsFromBeatSaver { get; set; }
    public bool HasPlayed { get; set; }
    public bool Blacklisted { get; set; }
}

public class DRMSongDiffData
{
    public string Difficulty { get; set; } = null;
    public string Characteristic { get; set; } = null;
    public float NoteJumpSpeed { get; set; }
    public float NotesPerSecond { get; set; }
    public QueuedSongMapMods MapMods { get; set; }
    public float ScoreSaberStars { get; set; }
    public float BeatLeaderStars { get; set; }
}

public class QueuedSongMapMods
{
    public bool Chroma { get; set; }
    public bool Cinema { get; set; }
    public bool MappingExtensions { get; set; }
    public bool NoodleExtensions { get; set; }
    public bool Vivify { get; set; }

}