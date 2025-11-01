namespace hitsApplication.AuthServices
{
    using hitsApplication.Enums;
    using Microsoft.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
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
                if (string.IsNullOrEmpty(token))
                    return false;

                if (token.StartsWith("Bearer "))
                    token = token.Substring(7);

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true, 
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning("Token expired: {Message}", ex.Message);
                return false;
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                _logger.LogWarning("Token signature invalid: {Message}", ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Token validation failed: {Message}", ex.Message);
                return false;
            }
        }

        public bool IsTokenExpired(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return true;

                if (token.StartsWith("Bearer "))
                    token = token.Substring(7);

                var expiration = GetTokenExpiration(token);
                return expiration.HasValue && expiration.Value < DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token expiration");
                return true;
            }
        }

        public DateTime? GetTokenExpiration(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return null;

                if (token.StartsWith("Bearer "))
                    token = token.Substring(7);

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                return jwtToken.ValidTo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token expiration");
                return null;
            }
        }

        public DateTime? GetTokenIssueDate(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return null;

                if (token.StartsWith("Bearer "))
                    token = token.Substring(7);

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                return jwtToken.ValidFrom;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token issue date");
                return null;
            }
        }

        public TimeSpan? GetTokenRemainingLifetime(string token)
        {
            try
            {
                var expiration = GetTokenExpiration(token);
                if (!expiration.HasValue) return null;

                var remaining = expiration.Value - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating token remaining lifetime");
                return null;
            }
        }

        public TokenStatus GetTokenStatus(string token)
        {
            if (string.IsNullOrEmpty(token))
                return TokenStatus.Missing;

            try
            {
                if (token.StartsWith("Bearer "))
                    token = token.Substring(7);

                if (!IsTokenValid(token))
                {
                    if (IsTokenExpired(token))
                        return TokenStatus.Expired;
                    else
                        return TokenStatus.Invalid;
                }

                return TokenStatus.Valid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining token status");
                return TokenStatus.Invalid;
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