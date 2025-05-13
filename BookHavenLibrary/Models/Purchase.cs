namespace BookHavenLibrary.Models
{
    public class Purchase
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public Book Book { get; set; } = null!;

        public DateTime PurchaseDate { get; set; }
    }
}
