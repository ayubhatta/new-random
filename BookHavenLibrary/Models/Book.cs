namespace BookHavenLibrary.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string ISBN { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string AuthorName { get; set; } = null!;
        public string PublisherName { get; set; } = null!;
        public decimal Price { get; set; }
        public string Format { get; set; } = null!; // Enum
        public string Language { get; set; } = null!;
        public DateTime PublicationDate { get; set; }
        public int PageCount { get; set; }
        public bool IsBestseller { get; set; }
        public bool IsAwardWinner { get; set; }
        public bool IsNewRelease { get; set; }
        public string CoverImageUrl { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }

        public virtual Inventory Inventory { get; set; } // One-to-one relationship
        public ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();

    }
}
