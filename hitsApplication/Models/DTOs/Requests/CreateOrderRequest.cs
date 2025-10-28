namespace hitsApplication.Models.DTOs.Requests
{
    public class CreateOrderRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }
}
