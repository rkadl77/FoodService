namespace hitsApplication.Models.DTOs.Requests
{
    public class UpdateQuantityRequest
    {
        public int DishId { get; set; }
        public int Quantity { get; set; }
    }
}