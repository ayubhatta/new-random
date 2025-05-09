namespace BookHavenLibrary.Models
{
    public class Announcement
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int CreatedBy { get; set; } // Admin FK
        public bool IsActive { get; set; }
    }
}
