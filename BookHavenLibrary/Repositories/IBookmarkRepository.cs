using BookHavenLibrary.Models;

namespace BookHavenLibrary.Repositories.Interfaces
{
    public interface IBookmarkRepository
    {
        Task<IEnumerable<Bookmark>> GetAllAsync();
        Task<Bookmark?> GetByIdAsync(int id);
        Task AddAsync(Bookmark bookmark);
        Task UpdateAsync(Bookmark bookmark);
        Task DeleteAsync(Bookmark bookmark);
        Task<IEnumerable<Bookmark>> GetByUserIdAsync(int userId);
    }
}
