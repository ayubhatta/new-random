using BookHavenLibrary.Data;
using BookHavenLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace BookHavenLibrary.Repositories
{
    public class PurchaseRepository : IPurchaseRepository
    {
        private readonly AppDbContext _context;

        public PurchaseRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Purchase purchase)
        {
            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasUserPurchasedBook(int userId, int bookId)
        {
            return await _context.Purchases.AnyAsync(p => p.UserId == userId && p.BookId == bookId);
        }

        public async Task<List<Purchase>> GetPurchasesByUserAsync(int userId)
        {
            return await _context.Purchases
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }
    }
}
