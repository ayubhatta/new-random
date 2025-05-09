namespace BookHavenLibrary.DTO
{
    public class ReviewDto
    {
        public int BookId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = null!;
    }

}
