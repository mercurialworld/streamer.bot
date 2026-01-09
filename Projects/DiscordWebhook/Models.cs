using System;

public class DiscordWebhookMessage
{
    public string Username { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DiscordEmbed[] Embeds { get; set; }
}

public class DiscordEmbed
{
    public long Color { get; set; }
    public EmbedAuthor Author { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public string Description { get; set; }
    public EmbedField[] Fields { get; set; }
    public EmbedThumbnail Thumbnail { get; set; }
    public EmbedImage Image { get; set; }
    public EmbedFooter Footer { get; set; }
    public string Timestamp { get; set; }
}

public class EmbedAuthor
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string IconUrl { get; set; }
}

public class EmbedField
{
    public string Name { get; set; }
    public string Value { get; set; }
    public bool Inline { get; set; } = false;
}

public class EmbedThumbnail
{
    public string Url { get; set; }
}

public class EmbedImage
{
    public string Url { get; set; }
}

public class EmbedFooter
{
    public string Text { get; set; }
    public string IconUrl { get; set; }
}