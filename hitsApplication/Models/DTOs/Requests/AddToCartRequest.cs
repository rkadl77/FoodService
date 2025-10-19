namespace hitsApplication.Models.DTOs.Requests
{
    public class AddToCartRequest
    {
        public Guid DishId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Comment { get; set; }
        public bool CreateOrder { get; set; } = false;
    }
}