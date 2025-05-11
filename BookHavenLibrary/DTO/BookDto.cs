namespace BookHavenLibrary.DTO
{
    public class BookDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string AuthorName { get; set; } = "";
        public string PublisherName { get; set; } = "";
        public string Isbn { get; set; } = "";
        public decimal Price { get; set; }
        public string Format { get; set; } = "";
        public string Language { get; set; } = "";
        public DateTime PublicationDate { get; set; }
        public int PageCount { get; set; }
        public bool IsBestseller { get; set; }
        public bool IsAwardWinner { get; set; }
        public bool IsNewRelease { get; set; }
        public bool NewArrival { get; set; }
        public bool CommingSoon { get; set; }
        public string CoverImageUrl { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public InventoryDto Inventory { get; set; } = new();
    }

}
