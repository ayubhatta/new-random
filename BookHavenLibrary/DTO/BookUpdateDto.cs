using System.ComponentModel.DataAnnotations;

namespace BookHavenLibrary.DTOs
{
    public class BookUpdateDto
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string AuthorName { get; set; } = null!;
        public string PublisherName { get; set; } = null!;
        public decimal Price { get; set; }
        public string Format { get; set; } = null!;
        public string Language { get; set; } = null!;
        public DateTime PublicationDate { get; set; }
        public int PageCount { get; set; }
        public bool IsBestseller { get; set; }
        public bool IsAwardWinner { get; set; }
        public bool IsNewRelease { get; set; }
        public bool NewArrival { get; set; }
        public bool CommingSoon { get; set; }
        public bool IsActive { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or greater.")]
        public int Quantity { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Reorder threshold must be 0 or greater.")]
        public int ReorderThreshold { get; set; } = 5; // Optional: allow user input or default to 5
        public IFormFile CoverImage { get; set; } = null!;
    }
}
