using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;


namespace SBot.Projects.DRMWebsocket;

public class Main : CPHInlineBase
{
    public string TTS_VOICE = "BS TTS";

    public bool SendBotMessage(string message, string replyTo = null)
    {
        CPH.SetArgument("message", message);

        if (!string.IsNullOrEmpty(replyTo))
        {
            CPH.SetArgument("replyToMessage", replyTo);
        }

        return CPH.RunActionById("5e4a052b-2e68-4107-95b1-8f1c3db06697");
    }

    public bool ParseWebsocketMessage()
    {
        if (!CPH.TryGetArg("message", out string message))
        {
            return false;
        }

        DRMWebsocketMessage serializedMessage = JsonConvert.DeserializeObject<DRMWebsocketMessage>(message);

        if (serializedMessage.EventType.Equals("queueOpen"))
        {
            var data = (bool)serializedMessage.Data;

            SendBotMessage($"Queue is {(data ? "open" : "closed")}!");
            return true;
        }
        else
        {
            // sure, okay, yeah, we can do this i guess
            var songData = ((JObject)serializedMessage.Data).ToObject<DRMSongData>();

            switch (serializedMessage.EventType)
            {
                case "pressedBan":
                    SendBotMessage($"{songData.BsrKey} is now banned from being requested.");
                    CPH.SetArgument("action", "BanSong");
                    CPH.SetArgument("requestUser", songData.User);
                    break;
                case "pressedLink":
                    SendBotMessage($"{songData.Artist} - {songData.Title} (mapped by {songData.Mapper}) https://beatsaver.com/maps/{songData.BsrKey}");
                    break;
                case "pressedPlay":
                    string requestedMapType = songData.IsWip ? "WIP" : $"request ({songData.Title} [{songData.BsrKey}])";
                    SendBotMessage($"@{songData.User} your {requestedMapType} is up next!");
                    break;
                case "pressedPoke":
                    SendBotMessage($"@{songData.User} your request is coming up!");
                    break;
                case "pressedSkip":
                    SendBotMessage($"{songData.BsrKey} has been skipped.");
                    break;
                default:
                    break;
            }
            return true;
        }
    }
}
