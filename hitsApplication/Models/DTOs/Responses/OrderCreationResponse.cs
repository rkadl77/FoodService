namespace hitsApplication.Models.DTOs.Responses
{
    public class OrderCreationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorCode { get; set; }
    }
}
