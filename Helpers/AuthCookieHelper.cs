namespace ProjectFlow.Api.Helpers;

public static class AuthCookieHelper
{
    public const string AccessTokenCookie = "accessToken";
    public const string RefreshTokenCookie = "refreshToken";

    public static void SetAuthCookies(
        HttpResponse response,
        string accessToken,
        string refreshToken,
        int accessTokenExpirationMinutes,
        int refreshTokenExpirationDays,
        bool isProduction)
    {
        response.Cookies.Append(
            AccessTokenCookie,
            accessToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = isProduction,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = "/",
                MaxAge = TimeSpan.FromMinutes(accessTokenExpirationMinutes)
            });

        response.Cookies.Append(
            RefreshTokenCookie,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = isProduction,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = "/",
                MaxAge = TimeSpan.FromDays(refreshTokenExpirationDays)
            });
    }

    public static void ClearAuthCookies(
        HttpResponse response,
        bool isProduction)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        };

        response.Cookies.Delete(AccessTokenCookie, options);
        response.Cookies.Delete(RefreshTokenCookie, options);
    }
}