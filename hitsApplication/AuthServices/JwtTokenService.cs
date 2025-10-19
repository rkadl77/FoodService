namespace hitsApplication.AuthServices
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using Microsoft.IdentityModel.Tokens;
    using System.Text;
    using System.Text.Json;

    public class JwtTokenService : IJwtTokenService
    {
        private readonly string _jwtSecret;
        private readonly ILogger<JwtTokenService> _logger;

        public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
        {
            _jwtSecret = configuration["Jwt:Secret"];
            _logger = logger;

            if (string.IsNullOrEmpty(_jwtSecret))
            {
                throw new Exception("JWT Secret is not configured in appsettings.json");
            }
        }

        public string GetUserIdFromToken(string token)
        {
            try
            {
                if (token.StartsWith("Bearer "))
                    token = token.Substring(7);

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                          ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value
                          ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                          ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "id")?.Value
                          ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                _logger.LogInformation("Extracted user ID from token: {UserId}", userId);
                return userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting user ID from token");
                return null;
            }
        }

        public Dictionary<string, string> GetAllClaims(string token)
        {
            try
            {
                if (token.StartsWith("Bearer "))
                    token = token.Substring(7);

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                return jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting claims from token");
                return new Dictionary<string, string>();
            }
        }

        public bool IsTokenValid(string token)
        {
            try
            {
                if (token.StartsWith("Bearer "))
                    token = token.Substring(7);

                var handler = new JwtSecurityTokenHandler();

                handler.ReadJwtToken(token);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetUserIdFromTokenManual(string token)
        {
            try
            {
                if (token.StartsWith("Bearer "))
                    token = token.Substring(7);

                var parts = token.Split('.');
                if (parts.Length != 3) return null;

                var payload = parts[1];

                while (payload.Length % 4 != 0)
                    payload += '=';

                var payloadBytes = Convert.FromBase64String(payload);
                var payloadJson = Encoding.UTF8.GetString(payloadBytes);

                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson);

                if (data.ContainsKey("sub")) return data["sub"].ToString();
                if (data.ContainsKey("user_id")) return data["user_id"].ToString();
                if (data.ContainsKey("userId")) return data["userId"].ToString();
                if (data.ContainsKey("id")) return data["id"].ToString();

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
