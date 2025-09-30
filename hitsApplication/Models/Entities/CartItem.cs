namespace hitsApplication.Models.Entities
{
    public class CartItem
    {
        public int DishId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal Subtotal => Price * Quantity;
    }
}