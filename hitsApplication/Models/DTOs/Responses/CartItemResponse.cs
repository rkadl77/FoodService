namespace hitsApplication.Models.DTOs.Responses
{
    public class CartItemResponse
    {
        public string DishId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Subtotal => Price * Quantity;
    }
}