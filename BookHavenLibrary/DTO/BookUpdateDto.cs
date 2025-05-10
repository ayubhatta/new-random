using System.ComponentModel.DataAnnotations;

namespace BookHavenLibrary.DTOs
{
    public class BookUpdateDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? AuthorName { get; set; }
        public string? PublisherName { get; set; }
        public decimal? Price { get; set; }
        public string? Format { get; set; }
        public string? Language { get; set; }
        public DateTime? PublicationDate { get; set; }
        public int? PageCount { get; set; }
        public bool? IsBestseller { get; set; }
        public bool? IsAwardWinner { get; set; }
        public bool? IsNewRelease { get; set; }
        public bool? NewArrival { get; set; }
        public bool? CommingSoon { get; set; }
        public bool? IsActive { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or greater.")]
        public int? Quantity { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Reorder threshold must be 0 or greater.")]
        public int? ReorderThreshold { get; set; }

        public IFormFile? CoverImage { get; set; }
    }
}
