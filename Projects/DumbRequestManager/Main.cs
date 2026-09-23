using System;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Enums;
using WebSocketSharp;

namespace SBot.Projects.DumbRequestManager;

public class Main : CPHInlineBase
{
    private static readonly DefaultContractResolver _contractResolver = new() { NamingStrategy = new CamelCaseNamingStrategy() };
    private readonly WebClient _client = new();

    public bool SendBotMessage(string message, string replyTo = null)
    {
        CPH.SetArgument("message", message);

        if (!string.IsNullOrEmpty(replyTo))
        {
            CPH.SetArgument("replyToMessage", replyTo);
        }

        return CPH.RunActionById("5e4a052b-2e68-4107-95b1-8f1c3db06697");
    }

    private bool IsInTwitchGroup(string userName, string group)
    {
        return CPH.UserInGroup(userName, Platform.Twitch, group);
    }
    
    public bool RequestBSRCheck(string rawMsg, out string bsrCode)
    {
        bsrCode = null;

        RequestArgs reqArgs = Helpers.NormalizeMessage(rawMsg);

        if (reqArgs.BsrKey.IsNullOrEmpty())
        {
            SendBotMessage(BotMessages.HOWTO);
            return false;
        }
        else
        {
            bsrCode = reqArgs.BsrKey;
        }

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
            SendBotMessage("You must be VIP or Moderator to use this command.");
            return false;
        }

        RequestArgs reqArgs = Helpers.NormalizeMessage(rawMsg);

        if (reqArgs.BsrKey.IsNullOrEmpty())
        {
            SendBotMessage("Map ID is either missing or invalid!");
            return false;
        }
        else
        {
            bsrCode = reqArgs.BsrKey;

            if (!reqArgs.Requester.IsNullOrEmpty())
            {
                originalRequester = reqArgs.Requester;
            }
        }

        return true;
    }

    public bool RemoveBSRCheck(string rawMsg)
    {
        if (!CPH.TryGetArg("userName", out string userName) || !CPH.TryGetArg("rawInput", out string rawInput))
        {
            return false;
        }

        var userInfo = CPH.TwitchGetUserInfoByLogin(userName);

        // wrong permissions
        if (!userInfo.IsModerator) 
        {
            SendBotMessage("You must be a Moderator to use this command.");
            return false;
        }

        RequestArgs reqArgs = Helpers.NormalizeMessage(rawMsg);

        if (reqArgs.BsrKey.IsNullOrEmpty())
        {
            SendBotMessage("Map ID is either missing or invalid!");
            return false;
        }
        else
        {
            CPH.SetArgument("bsr", reqArgs.BsrKey);
        }

        return true;
    }

    public bool OopsBSRCheck()
    {
        if (!CPH.TryGetArg("userName", out string userName) || !CPH.TryGetArg("rawInput", out string rawInput))
        {
            return false;
        }

        // without any arguments
        if (string.IsNullOrEmpty(rawInput))
        {
            return true;
        }

        var args = rawInput.Split(' ');
        string firstArgument = args[0];

        // the actual check
        if (!Helpers.IsValidHex(firstArgument))
        {
            SendBotMessage("Invalid BSR code!");
            return false;
        } 


        CPH.SetArgument("CmdArgs", firstArgument);
        return true;
    }

    public bool GetRequestInfo(string bsrCode, string userName, bool IsModAdd = false)
    {
        string messageToSpeak = $"{userName} {(IsModAdd ? "modadded" : "requested")} ";
        string bsrSplit = string.Join(" ", bsrCode.Split());

        HttpResponseMessage res = _client.GetRequestSync($"https://theblackparrot.me/bs/bsr-filter/index.php?hash={bsrCode}&format=json");

        try
        {
            if (!res.IsSuccessStatusCode)
            {
                CPH.TtsSpeak(BotMessages.TTS_VOICE, $"Couldn't get map {bsrSplit}", false);
                return false;
            }

            // Get the response data
            string content = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            MapInfo parsed = JsonConvert.DeserializeObject<MapInfo>(content);

            // [TODO] move to its own function

            messageToSpeak += $"bsr {bsrSplit}";

            if (!IsModAdd)
            {
                messageToSpeak += $" {parsed.Metadata.Title} by {parsed.Metadata.Artist} mapped by {parsed.Metadata.Mapper}";
            }

            CPH.TtsSpeak(BotMessages.TTS_VOICE, messageToSpeak.ToString(), false);

            return true;
        }
        
        catch (Exception e)
        {

            CPH.TtsSpeak(BotMessages.TTS_VOICE, $"Couldn't get map {bsrSplit}", false);
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
                CPH.TtsSpeak(BotMessages.TTS_VOICE, $"untrusted user {userName} lost the 50/50");
                SendBotMessage("Error adding request.", messageId);
                return false;
            }
        }

        // TODO: have a strike system for this; rn it's being given manually
        if (IsInTwitchGroup(userName, "reqbanned")) 
        {
            CPH.TtsSpeak(BotMessages.TTS_VOICE, $"request-banned user {userName} tried requesting something");
            SendBotMessage("Error adding request.", messageId);
            return false;
        }

        // if someone tries to request a funny they get timed out for 15 seconds
        if (Array.Exists(Helpers.funnyBeatSaberMapsToRequestToEverySingleStreamerOnTwitchEverIBetEverySingleOneOfThemWillEnjoyThem, x => x == bsrCode))
        {
            var userInfo = CPH.TwitchGetUserInfoByLogin(userName);
            if (!userInfo.IsVip && !userInfo.IsModerator)
            {
                SendBotMessage($"@{userName} You've been timed out for 15 seconds. Please don't request an overdone map and try again.");
                CPH.TtsSpeak(BotMessages.TTS_VOICE, $"{userName} got themselves timed out for a little bit");
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
            !GetRequestInfo(bsrCode, userName) ||
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
            !GetRequestInfo(bsrCode, userName, true)
        )
        {
            return false;
        }


        CPH.SetArgument("bsr", bsrCode);
        CPH.SetArgument("originalRequester", originalRequester);

        return true;
    }

    public bool BSRNDCheck()
    {
        if (!CPH.TryGetArg("userName", out string userName) || !CPH.TryGetArg("randomKey", out string bsrCode))
        {
            return false;
        }

        if (
            !GetRequestInfo(bsrCode, $"{userName} via redeem")
        )
        {
            return false;
        }

        CPH.SetArgument("bsr", bsrCode);
        CPH.SetArgument("bsrndRequester", $"{userName}@BSRND");

        return true;
    }
}
