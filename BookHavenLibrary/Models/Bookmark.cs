namespace BookHavenLibrary.Models
{
    public class Bookmark
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
