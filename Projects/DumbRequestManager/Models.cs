public class MapInfo
{
    public string Id { get; set; }
    public string Ranked { get; set; }
    public MapVotes Votes { get; set; }
    public MapMetadata Metadata { get; set; }
    public string Cover { get; set; }
    public long LastPublish { get; set; }

}

public class MapVotes
{
    public int Up { get; set; }
    public int Down { get; set; }
    public int Score { get; set; }
}

public class MapMetadata
{
    public string Artist { get; set; }
    public string Title { get; set; }
    public string Mapper { get; set; }
    public int Duration { get; set; }
    public int Tempo { get; set; }
}

public class MapDiffShort
{
    public string Name { get; set; }
    public string Nps { get; set; }
    public int Njs { get; set; }
}