using BookHavenLibrary.Models;

namespace BookHavenLibrary.Repositories
{
    public interface IDiscountRepository
    {
        Task<IEnumerable<Discount>> GetAllAsync();
        Task<Discount?> GetByIdAsync(int id);
        Task AddAsync(Discount discount);
        Task UpdateAsync(Discount discount);
        Task DeleteAsync(int id);
        Task<IEnumerable<Discount>> GetExpiredDiscountsAsync();
        Task<Discount?> GetByBookIdAsync(int bookId);

    }
}
