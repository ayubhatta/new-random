using Microsoft.AspNetCore.Identity;

namespace BookHavenLibrary.Models
{
    public class User : IdentityUser<int> // Use int as PK type
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Address { get; set; } = null!;
        public DateTime DateJoined { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; }
    }
}
