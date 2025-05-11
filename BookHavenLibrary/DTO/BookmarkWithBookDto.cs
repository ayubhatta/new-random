namespace BookHavenLibrary.DTO
{
    public class BookmarkWithBookDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public BookDto Book { get; set; } = new();
    }

}
