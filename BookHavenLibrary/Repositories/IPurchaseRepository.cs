using BookHavenLibrary.Models;

namespace BookHavenLibrary.Repositories
{
    public interface IPurchaseRepository
    {
        Task AddAsync(Purchase purchase);
        Task<bool> HasUserPurchasedBook(int userId, int bookId);
        Task<List<Purchase>> GetPurchasesByUserAsync(int userId);

        Task<List<Purchase>> GetAllPurchasesAsync();

    }
}
