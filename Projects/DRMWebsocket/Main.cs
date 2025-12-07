using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;


namespace SBot.Projects.DRMWebsocket;

public class Main : CPHInlineBase
{
    public string TTS_VOICE = "BS TTS";

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

            CPH.SendMessage($"Queue is {(data ? "open" : "closed")}!", true, false);
            return true;
        }
        else
        {
            // sure, okay, yeah, we can do this i guess
            var songData = ((JObject)serializedMessage.Data).ToObject<DRMSongData>();

            switch (serializedMessage.EventType)
            {
                case "pressedBan":
                    CPH.SendMessage($"{songData.BsrKey} is now banned from being requested.", true, false);
                    CPH.SetArgument("action", "BanSong");
                    CPH.SetArgument("requestUser", songData.User);
                    break;
                case "pressedLink":
                    CPH.SendMessage($"{songData.Artist} - {songData.Title} (mapped by {songData.Mapper}) https://beatsaver.com/maps/{songData.BsrKey}", true, false);
                    break;
                case "pressedPlay":
                    string requestedMapType = songData.IsWip ? "WIP" : $"request ({songData.Title} [{songData.BsrKey}])";
                    CPH.SendMessage($"@{songData.User} your {requestedMapType} is up next!", true, false);
                    break;
                case "pressedPoke":
                    CPH.SendMessage($"@{songData.User} your request is coming up!", true, false);
                    break;
                case "pressedSkip":
                    CPH.SendMessage($"{songData.BsrKey} has been skipped.", true, false);
                    break;
                default:
                    break;
            }
            return true;
        }
    }
}
