using hitsApplication.Enums;
namespace hitsApplication.AuthServices
{
    public interface IJwtTokenService
    {
        string GetUserIdFromToken(string token);
        Dictionary<string, string> GetAllClaims(string token);
        bool IsTokenValid(string token);
        bool IsTokenExpired(string token);
        DateTime? GetTokenExpiration(string token);
        DateTime? GetTokenIssueDate(string token);
        TimeSpan? GetTokenRemainingLifetime(string token);
        TokenStatus GetTokenStatus(string token);
    }
}