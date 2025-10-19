namespace hitsApplication.AuthServices
{
    public interface IJwtTokenService
    {
        string GetUserIdFromToken(string token);
        Dictionary<string, string> GetAllClaims(string token);
        bool IsTokenValid(string token);
    }
}
