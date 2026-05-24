namespace BaiTapLon.Api.Services;

public class JwtOptions
{
    public string Issuer { get; set; } = "CineManager";
    public string Audience { get; set; } = "CineManager.Web";
    public string Secret { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 14;
}
