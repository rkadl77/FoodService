namespace hitsApplication.Models.DTOs.Requests
{
    public class JavaOrderItem
    {
        public Guid DishId { get; set; }    
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int Quantity { get; set; }      
    }
}
