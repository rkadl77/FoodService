namespace hitsApplication.Models.DTOs.Requests
{
    public class UpdateQuantityRequest
    {
        public Guid DishId { get; set; }
        public int Quantity { get; set; }
    }
}