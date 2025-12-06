using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SBot.Projects.DumbRequestManager;

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
    public static bool IsValidTwitchLogin(string possiblyTwitchLogin)
    {
        return Regex.Matches(possiblyTwitchLogin, @"^@?[a-zA-Z0-9_]{1,25}$").Count > 0;
    }
}
