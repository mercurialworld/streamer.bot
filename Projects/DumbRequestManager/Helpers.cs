using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SBot.Projects.DumbRequestManager;

public class RequestArgs
{
    public string BsrKey { get; set; } = String.Empty;
    public string Requester { get; set; } = String.Empty;
}

public static class Helpers
{
    /// <summary>
    /// Checks whether a string is valid hexadecimal or not.
    /// </summary>
    /// <param name="possiblyHex">A string that, possibly, can be hexadecimal.</param>
    /// <returns>Whether the string is hexadecimal or not.</returns>
    public static bool IsValidHex(string possiblyHex)
    {
        CultureInfo provider = CultureInfo.InvariantCulture;
        return int.TryParse(possiblyHex, NumberStyles.HexNumber, provider, out int _);
    }

    /// <summary>
    /// Checks whether a string is a valid Twitch login.
    /// </summary>
    /// <param name="possiblyTwitchLogin">A string that, possibly, can be a Twitch login.</param>
    /// <returns>Whether the string is valid as a Twitch login or not.</returns>
    public static bool TryGetUserMention(string possiblyTwitchLogin)
    {
        return Regex.Matches(possiblyTwitchLogin, @"^@?[a-zA-Z0-9_]{1,25}$").Count > 0 || possiblyTwitchLogin.StartsWith("@");
    }
    
    /// <summary>
    /// Normalizes a message into a positional array of strings.
    /// </summary>
    /// <param name="rawMsg">The message, minus the first word (if it's a command)</param>
    /// <returns>An object possibly with a BeatSaver ID and a user.</returns>
    public static RequestArgs NormalizeMessage(string rawMsg)
    {
        RequestArgs req = new();

        if (string.IsNullOrEmpty(rawMsg))
        {
            return req;
        }

        string[] args = rawMsg.Split(' ');
        string firstArg = args[0];

        if (IsValidHex(firstArg))
        {
            req.BsrKey = firstArg;

            if (args.Length > 1)
            {
                string secondArg = args[1];

                if (TryGetUserMention(secondArg))
                {
                    req.Requester = secondArg.TrimStart(['@']);
                }               

            }
        }
        else if (TryGetUserMention(firstArg))
        {
            req.Requester = firstArg.TrimStart(['@']);
        }

        return req;
    }

    // https://github.com/TheBlackParrot-Streaming-Overlays/chat/blob/159b9ef882de066c24d9a8f23a410c812a430a3d/consts.js#L84
    public static string[] funnyBeatSaberMapsToRequestToEverySingleStreamerOnTwitchEverIBetEverySingleOneOfThemWillEnjoyThem =
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
}
