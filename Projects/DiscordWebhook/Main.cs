using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Model;

namespace SBot.Projects.DiscordWebhook;

public class Main : CPHInlineBase
{
    private static readonly HttpClient _httpClient = new();
    private static readonly DefaultContractResolver _contractResolver = new() { NamingStrategy = new SnakeCaseNamingStrategy() };

    private async Task<bool> SendWebhookMessage(string webhookURL, DiscordWebhookMessage webhookMessage, string successMessage)
    {

        var stringMessage = JsonConvert.SerializeObject(webhookMessage, new JsonSerializerSettings
        {
            ContractResolver = _contractResolver
        });

        var content = new StringContent(stringMessage, Encoding.UTF8, "application/json");

        HttpResponseMessage res = await _httpClient.PostAsync(webhookURL, content);

        try
        {
            if (!res.IsSuccessStatusCode)
            {
                CPH.SendMessage($"Failed to send request (status code was {(int)res.StatusCode}).", true, false);
                return false;
            }

            CPH.SendMessage(successMessage, true, false);
            return true;
        }
        catch (Exception e)
        {
            CPH.LogError(e.Message);
            return false;
        }
    }

    public bool SendNoteWebhook()
    {
        if (!CPH.TryGetArg("WebhookURL", out string url) || !CPH.TryGetArg("userName", out string username) || !CPH.TryGetArg("rawInput", out string rawInput))
        {
            return false;
        }

        string timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        TwitchUserInfoEx userInfo = CPH.TwitchGetExtendedUserInfoByLogin(username);

        DiscordWebhookMessage webhookMessage = new()
        {
            Username = "[Twitch] Stream Notes",
            AvatarUrl = "https://files.catbox.moe/bjkwl0.png",
            Embeds = [
                new DiscordEmbed() 
                {
                    Author = new() {
                        Name = userInfo.UserName,
                        IconUrl = userInfo.ProfileImageUrl
                    },
                    Color = 12180702,
                    Description = rawInput,
                    Timestamp = timestamp
                }
            ]
        };

        return SendWebhookMessage(url, webhookMessage, "NOTED").GetAwaiter().GetResult();
    }

    public bool SendGoLiveWebhook()
    {
        if (!CPH.TryGetArg("WebhookURL", out string url))
        {
            return false;
        }

        string timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        TwitchUserInfoEx userInfo = CPH.TwitchGetExtendedUserInfoByLogin("empoleonics");

        DiscordWebhookMessage webhookMessage = new()
        {
            Username = "[Twitch] Empoleon's live!",
            AvatarUrl = "https://files.catbox.moe/bjkwl0.png",
            Content = "<@&1413750797463851140> https://twitch.tv/empoleonics",
            Embeds = [
                new DiscordEmbed() 
                {
                    Color = 1519702,
                    Title = "I'm live!",
                    Url = "https://twitch.tv/empoleonics",
                    Description = userInfo.ChannelTitle,
                    Fields = [ new EmbedField() { Name = "Live with" , Value = userInfo.Game, Inline = false}],
                    Thumbnail = new EmbedThumbnail() { Url = userInfo.ProfileImageUrl },
                    Image = new EmbedImage() { Url = "https://static-cdn.jtvnw.net/previews-ttv/live_user_empoleonics-640x360.jpg"},
                    Timestamp = timestamp
                }
            ]
        };

        return SendWebhookMessage(url, webhookMessage, "Live embed sent!").GetAwaiter().GetResult();
    }
}
