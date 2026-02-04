using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Streamer.bot.Plugin.Interface;

namespace SBot.Projects.SongRequestManager;

public class Main : CPHInlineBase
{
    private string SONGLINK_API = "https://api.song.link/v1-alpha.1/links?userCountry=CA&url=";
    private string CIDER_QUEUE_LINK = "http://localhost:10767/api/v1/playback/play-next";

    private static readonly HttpClient _httpClient = new();
    private static readonly DefaultContractResolver _contractResolver = new() { NamingStrategy = new CamelCaseNamingStrategy() };

    public bool SendBotMessage(string message, string replyTo = null)
    {
        CPH.SetArgument("message", message);

        if (!string.IsNullOrEmpty(replyTo))
        {
            CPH.SetArgument("replyToMessage", replyTo);
        }

        return CPH.RunActionById("5e4a052b-2e68-4107-95b1-8f1c3db06697");
    }

    private bool TryGetValidLink(string redeemMessage, ref string link)
    {
        if (Regex.Matches(redeemMessage, @"^https?://(?:(?:soundcloud|(?:music\.)?youtube|open\.spotify|music\.apple)\.com|youtu\.be)").Count > 0) 
        {
            link = redeemMessage;
            return true;
        }

        else if (redeemMessage.StartsWith("spotify:track:"))
        {   
            // surely this will work 4Head
            link = "https://open.spotify.com/track/" + redeemMessage.Substring(14);
            return true;
        }

        return false;
    }

    private bool GetSongLinkResponse(string streamingServiceUrl, ref SongLinkResponse slRes)
    {
        HttpResponseMessage res = _httpClient
            .GetAsync(SONGLINK_API + streamingServiceUrl)
            .GetAwaiter()
            .GetResult();
        
        try
        {
            if (!res.IsSuccessStatusCode)
            {
                // songlink not worky
                CPH.LogError($"Unable to reach Song.Link servers: {res.ReasonPhrase}");
                return false;
            }

            // songlink worky
            string content = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            SongLinkResponse songLinkResponse = JsonConvert.DeserializeObject<SongLinkResponse>(content, new JsonSerializerSettings
            {
                ContractResolver = _contractResolver
            });

            slRes = songLinkResponse;
            return true;
        }
        catch (Exception e)
        {
            CPH.LogError(e.Message);
            return false;
        }
    }

    // [TODO] refactor aaaaaaaaaaaaa
    private PlatformEntityEntry ExtractAppleMusicSongInfo(SongLinkResponse slRes)
    {

        if (!slRes.LinksByPlatform.TryGetValue("appleMusic", out PlatformEntry appleMusicPlatformEntryMaybe))
        {
            return null;
        }

        if (!slRes.EntitiesByUniqueId.TryGetValue(appleMusicPlatformEntryMaybe.EntityUniqueId, out PlatformEntityEntry platformEntity))
        {
            return null;
        }

        return platformEntity;
    }

    private bool AddSongToQueue(string appleMusicSongID)
    {
        CiderPlayNextBody ciderPlayNext = new()
        {
            Type = "songs",
            Id = appleMusicSongID
        };

        var stringMessage = JsonConvert.SerializeObject(ciderPlayNext, new JsonSerializerSettings
        {
            ContractResolver = _contractResolver
        });

        var content = new StringContent(stringMessage, Encoding.UTF8, "application/json");

        HttpResponseMessage addSongRes = _httpClient
            .PostAsync(CIDER_QUEUE_LINK, content)
            .GetAwaiter().GetResult();

        // for some reason it returns OK even if the song fails? 
        // [TODO] add another guardrail of some sort maybe by getting the queue?
        try
        {
            if (!addSongRes.IsSuccessStatusCode)
            {
                // songlink not worky
                CPH.LogError($"Unable to reach Cider: {addSongRes.ReasonPhrase}");
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            CPH.LogError(e.Message);
            return false;
        }
    }

    private bool RefundPoints()
    {
        // refund points immediately
        CPH.TryGetArg("rewardId",out string rewardId);
        CPH.TryGetArg("redemptionId",out string redemptionId);

        CPH.TwitchRedemptionCancel(rewardId, redemptionId); 
        
        return false;
    }

    public bool HandleRequest()
    {
        if (!CPH.TryGetArg("rawInput", out string rawInput) || !CPH.TryGetArg("userName", out string userName))
        {
            return false;
        }

        string requestLink = string.Empty;

        // if not url just deny it lol
        if (!TryGetValidLink(rawInput, ref requestLink))
        {
            SendBotMessage("Song searching hasn't been implemented yet, sorry! Try again with a link to the song on a streaming service. ^^;");
            return RefundPoints();
        }

        // if url or uri do the song link thing
        SongLinkResponse slRes = null;

        if (!GetSongLinkResponse(CPH.UrlEncode(requestLink), ref slRes))
        {
            SendBotMessage("Unable to reach the Song.Link API");
            return RefundPoints();
        }

        // TODO: if it's a youtube url or not the english name, search for title using raw AM API
        // https://cider.sh/docs/client/rpc#apiv1amapi
        PlatformEntityEntry amEntity = ExtractAppleMusicSongInfo(slRes);

        if (amEntity == null)
        {
            SendBotMessage("Cannot find Apple Music entry (you might need to ask streamer to manually add it)");
            return false;
        }

        if (AddSongToQueue(amEntity.Id))
        {
            SendBotMessage($"@{userName} {amEntity.Title} by {amEntity.ArtistName} (most likely) added to queue!");
            return true;
        }

        return false;
    }
}
