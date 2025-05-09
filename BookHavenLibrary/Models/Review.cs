namespace BookHavenLibrary.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public int Rating { get; set; } // 1 to 5 scale
        public string Comment { get; set; } = null!;
        public DateTime ReviewDate { get; set; }
        public bool IsVerifiedPurchase { get; set; }
    }
}
