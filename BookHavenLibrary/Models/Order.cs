namespace BookHavenLibrary.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public bool DiscountApplied { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string Status { get; set; } = "pending"; // Enum: 'pending', 'ready_for_pickup', 'completed', 'cancelled'
        public string ClaimCode { get; set; } = null!;
        public DateTime? UpdatedAt { get; set; }

        public DateTime? PickupDate { get; set; }
        public int? ProcessedBy { get; set; } // Admin FK
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    }
}