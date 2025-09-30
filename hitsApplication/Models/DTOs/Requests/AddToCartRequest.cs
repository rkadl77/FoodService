namespace hitsApplication.Models.DTOs.Requests
{
    public class AddToCartRequest
    {
        public int DishId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }
}