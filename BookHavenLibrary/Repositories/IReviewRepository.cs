using BookHavenLibrary.Models;

public interface IReviewRepository
{
    Task<List<Review>> GetAllAsync();
    Task<Review?> GetByIdAsync(int id);
    Task<List<Review>> GetByBookIdAsync(int bookId);
    Task AddAsync(Review review);
    Task UpdateAsync(Review review);
    Task DeleteAsync(int id);
}
