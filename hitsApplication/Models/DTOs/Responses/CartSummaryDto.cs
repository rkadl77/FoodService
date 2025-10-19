namespace hitsApplication.Models.DTOs.Responses
{
    public class CartSummaryResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal Total { get; set; }
        public List<CartItemResponse>? Items { get; set; }

        public bool IsEmpty => ItemCount == 0;
        public bool HasItems => ItemCount > 0;

        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Comment { get; set; }
    }
}