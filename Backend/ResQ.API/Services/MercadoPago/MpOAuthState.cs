using System.Text;

namespace ResQ.API.Services.MercadoPago;

/// <summary>
/// Encodes and decodes the OAuth <c>state</c> parameter that is round-tripped through
/// Mercado Pago during the merchant authorization flow.
/// </summary>
/// <remarks>
/// The state carries two pieces of information: the merchant profile id (so the callback
/// can correlate the response with the correct merchant) and the frontend origin the
/// merchant initiated the flow from. Returning the merchant to that exact origin matters
/// because the session refresh-token cookie is host-only: a merchant who started on
/// <c>http://localhost</c> must be returned to <c>http://localhost</c> (not the public
/// tunnel domain) for their session to be restored on landing.
/// Format: <c>{merchantProfileId}.{base64url(returnOrigin)}</c>; the origin segment is optional.
/// </remarks>
public static class MpOAuthState
{
    /// <summary>
    /// Builds the OAuth <c>state</c> value from the merchant profile id and an optional return origin.
    /// </summary>
    public static string Encode(int merchantProfileId, string? returnOrigin)
    {
        if (string.IsNullOrWhiteSpace(returnOrigin))
            return merchantProfileId.ToString();

        return $"{merchantProfileId}.{Base64UrlEncode(returnOrigin)}";
    }

    /// <summary>
    /// Parses an OAuth <c>state</c> value back into its merchant profile id and return origin.
    /// </summary>
    /// <returns><see langword="true"/> if the merchant profile id was parsed successfully.</returns>
    public static bool TryDecode(string? state, out int merchantProfileId, out string? returnOrigin)
    {
        merchantProfileId = 0;
        returnOrigin      = null;

        if (string.IsNullOrWhiteSpace(state))
            return false;

        var parts = state.Split('.', 2);
        if (!int.TryParse(parts[0], out merchantProfileId))
            return false;

        if (parts.Length == 2 && parts[1].Length > 0)
        {
            try   { returnOrigin = Base64UrlDecode(parts[1]); }
            catch { returnOrigin = null; }
        }

        return true;
    }

    private static string Base64UrlEncode(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string Base64UrlDecode(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        s += (s.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }
}
