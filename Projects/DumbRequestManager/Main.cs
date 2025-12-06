using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Enums;

namespace SBot.Projects.DumbRequestManager;

public class Main : CPHInlineBase
{
    public string TTS_VOICE = "BS TTS";
    public string HOWTO = "To request music, in your Internet browser of choice, navigate to https://beatsaver.com and search for the song you want to see me play. Press the ! icon to copy a song code to your clipboard that you can paste here in chat!";
    public string SEARCH_HOWTO => $"Search is disabled! {HOWTO}";

    // https://github.com/TheBlackParrot-Streaming-Overlays/chat/blob/159b9ef882de066c24d9a8f23a410c812a430a3d/consts.js#L84
    private string[] funnyBeatSaberMapsToRequestToEverySingleStreamerOnTwitchEverIBetEverySingleOneOfThemWillEnjoyThem =
    [
        "25f",
        "6136",
        "7269",
        "5f22",
        "ffb6",
        "110db",
        "103d8",
        "d1cc",
        "b",
        "1a209",
        "c32d",
        "922f",
        "871a",
        "10c9b",
        "1e99",
        "1eb9",
        "2a121",
        "24188",
        "46d4",
        "24b58",
        "557f",
        "1f89a",
        "335c",
        "e621",
        "2c2f4",
        "11cf8",
        "21ef9",
        "ff9",
        "3b608",
        "cffd",
        "10dcc",
        "376da",
        "1f7c9",
        "108ee",
        "352b3",
        "352b7",
        "21d9",
        "4e8d",
        "148e9",
        "15af0",
        "20291",
        "11b28",
        "fd07",
        "1a524",
        "34b8c",
        "16a58",
        "6777",
        "1db5d"
    ];

    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    public void Init()
    {
        // Ensure we are working with a clean slate
        _httpClient.DefaultRequestHeaders.Clear();
    }

    public void Dispose()
    {
        // Free up allocations
        _httpClient.Dispose();
    }

    private bool IsInTwitchGroup(string userName, string group)
    {
        return CPH.UserInGroup(userName, Platform.Twitch, group);
    }

    private bool TryGetUserMention(string possiblyUser)
    {
        // can the string resolve to a valid username, or does it start with an @ (for intl name)?      
        return Helpers.IsValidTwitchLogin(possiblyUser) || possiblyUser.StartsWith("@");
    }
    
    public bool RequestBSRCheck(string rawMsg, out string bsrCode)
    {
        bsrCode = null;
        
        // bsr without any arguments
        if (string.IsNullOrEmpty(rawMsg))
        {
            CPH.SendMessage(HOWTO, true, false);
            return false;
        }

        string firstArgument = rawMsg.Split(' ')[0];

        // bsr with valid user mention instead of bsr
        if (TryGetUserMention(firstArgument))
        {
            CPH.SendMessage($"{firstArgument} {HOWTO}", true, false);
            return false;
        }

        // the actual check
        if (!Helpers.IsValidHex(firstArgument))
        {
            CPH.SendMessage(SEARCH_HOWTO, true, false);
            return false;
        } 
        
        // ok we're good
        bsrCode = firstArgument;
        return true;
    }

    public bool ModaddBSRCheck(string rawMsg, string commandInvoker, out string bsrCode, out string originalRequester)
    {
        bsrCode = null;
        originalRequester = commandInvoker;

        var userInfo = CPH.TwitchGetUserInfoByLogin(commandInvoker);

        // wrong permissions
        if (!userInfo.IsVip && !userInfo.IsModerator) 
        {
            CPH.SendMessage("You must be VIP or Moderator to use this command.", true, false);
            return false;
        }

        // bsr without any arguments
        if (string.IsNullOrEmpty(rawMsg))
        {
            CPH.SendMessage("You're missing a code!", true, false);
            return false;
        }

        var args = rawMsg.Split(' ');
        string firstArgument = args[0];
        string secondArgument = args[1];

        // the actual check
        if (!Helpers.IsValidHex(firstArgument))
        {
            CPH.SendMessage("Invalid BSR code! Format is !modadd <code> [username]", true, false);
            return false;
        } 

        if (TryGetUserMention(secondArgument))
        {
            originalRequester = secondArgument;
        }

        return true;
    }


    public bool SendRequestInfo(string bsrCode)
    {
        HttpResponseMessage res = 
            _httpClient
                .GetAsync($"https://theblackparrot.me/bs/bsr-filter/index.php?hash={bsrCode}")
                .GetAwaiter()
                .GetResult();
        try
        {
            if (!res.IsSuccessStatusCode)
            {
                CPH.TtsSpeak(TTS_VOICE, "Error in querying map", false);
                return false;
            }

            // Get the response data
            string content = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            // Send as message
            CPH.SendMessage(content, true, false);

            return true;
        }
        catch (Exception e)
        {
            CPH.LogError(e.Message);
            return false;
        }
    }

    public bool SpeakRequestInfo(string bsrCode, string userName, bool IsModAdd = false)
    {
        string messageToSpeak = $"{userName} {(IsModAdd ? "modadded" : "requested")}";

        HttpResponseMessage res = 
            _httpClient.GetAsync($"https://api.beatsaver.com/maps/id/{bsrCode}")   
                .GetAwaiter()
                .GetResult();
        try
        {
            if (!res.IsSuccessStatusCode)
            {
                CPH.TtsSpeak(TTS_VOICE, "Error in querying map", false);
                return false;
            }

            // Get the response data
            string content = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            JObject parsed = JObject.Parse(content);
            messageToSpeak += $"bsr {bsrCode} {(string)parsed.SelectToken("metadata.songName")} by {(string)parsed.SelectToken("metadata.songAuthorName")} mapped by {(string)parsed.SelectToken("metadata.levelAuthorName")}";

            CPH.TtsSpeak(TTS_VOICE, messageToSpeak.ToString(), false);

            return true;
        }
        
        catch (Exception e)
        {
            CPH.LogError(e.Message);
            return false;
        }

    }

    public bool FilterUserGroups(string bsrCode, string userName)
    {
        CPH.TryGetArg("msgId", out string messageId);

        // it's very easy to put yourself in the untrusted group! 
        // just request anything in the funny beat saber maps array
        if (IsInTwitchGroup(userName, "untrusted"))
        {
            // you now get to gamble with whether your request goes through or not
            var rand = new Random();

            // it's a 60% chance of it not going through
            var chance = rand.NextDouble();
            if (chance < 0.6f)
            {
                // and the best part is that you won't even know you're untrusted!
                // mess with the bull and get the horns lmfao
                CPH.TtsSpeak(TTS_VOICE, $"untrusted user {userName} lost the 50/50");
                CPH.TwitchReplyToMessage("Error adding request.", messageId, true, false);
                return false;
            }
        }

        // TODO: have a strike system for this; rn it's being given manually
        if (IsInTwitchGroup(userName, "reqbanned")) 
        {
            CPH.TtsSpeak(TTS_VOICE, $"request-banned user {userName} tried requesting something");
            CPH.TwitchReplyToMessage("Error adding request.", messageId, true, false);
            return false;
        }

        // if someone tries to request a funny they get timed out for 15 seconds
        if (Array.Exists(funnyBeatSaberMapsToRequestToEverySingleStreamerOnTwitchEverIBetEverySingleOneOfThemWillEnjoyThem, x => x == bsrCode))
        {
            var userInfo = CPH.TwitchGetUserInfoByLogin(userName);
            if (!userInfo.IsVip && !userInfo.IsModerator)
            {
                CPH.SendMessage($"@{userName} You've been timed out for 15 seconds. Please don't request an overdone map and try again.", true, false);
                CPH.TtsSpeak(TTS_VOICE, $"{userName} got themselves timed out for a little bit");
                CPH.TwitchTimeoutUser(userName, 15, "requested funny map seriously");
                
                // i no longer trust you to have good requests now
                CPH.AddUserToGroup(userName, Platform.Twitch, "untrusted");
                return false;
            }
        }

        return true;
    }

    public bool RegularRequestCheck()
    {   
        if (!CPH.TryGetArg("userName", out string userName) || !CPH.TryGetArg("rawInput", out string rawInput))
        {
            return false;
        }

        if (
            !RequestBSRCheck(rawInput, out var bsrCode) ||
            !SendRequestInfo(bsrCode) || !SpeakRequestInfo(bsrCode, userName) ||
            !FilterUserGroups(bsrCode, userName)
        )
        {
            return false;
        }

        CPH.SetArgument("bsr", bsrCode);
        CPH.SetArgument("userName", userName);

        return true;
    }

    public bool ModAddCheck()
    {
        if (!CPH.TryGetArg("userName", out string userName) || !CPH.TryGetArg("rawInput", out string rawInput))
        {
            return false;
        }

        if (
            !ModaddBSRCheck(rawInput, userName, out var bsrCode, out var originalRequester) ||
            !SpeakRequestInfo(bsrCode, userName, true)
        )
        {
            return false;
        }


        CPH.SetArgument("bsr", bsrCode);
        CPH.SetArgument("requester", originalRequester);

        return true;
    }
}
