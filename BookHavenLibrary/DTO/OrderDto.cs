using BookHavenLibrary.DTO;

public class OrderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string ClaimCode { get; set; }
    public string Status { get; set; }
    public DateTime? PickupDate { get; set; }
    public int? ProcessedBy { get; set; }
    public List<OrderItemDto> OrderItems { get; set; }
}