namespace hitsApplication.Models.DTOs.Responses
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty; 
        public string UserId { get; set; } = string.Empty; 
        public string? Message { get; set; }
        public string? Error { get; set; }
    }
}