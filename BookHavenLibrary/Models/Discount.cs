namespace BookHavenLibrary.Models
{
    public class Discount
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int UserId { get; set; } // Admin who created it
        public decimal DiscountPercentage { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool OnSale { get; set; }
        public bool IsActive { get; set; }
    }
}
