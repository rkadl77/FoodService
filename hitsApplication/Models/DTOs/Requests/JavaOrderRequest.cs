namespace hitsApplication.Models.DTOs.Requests
{
    public class JavaOrderRequest
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Guid UserId { get; set; }
        public int ItemCount { get; set; }
        public double Total { get; set; }
        public List<JavaOrderItem> Items { get; set; } = new();
        public bool IsEmpty { get; set; }
        public bool HasItems { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string PaymentMethod { get; set; }
        public string Comment { get; set; }
    }
}
