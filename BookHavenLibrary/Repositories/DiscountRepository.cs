using BookHavenLibrary.Data;
using BookHavenLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace BookHavenLibrary.Repositories
{
    public class DiscountRepository : IDiscountRepository
    {
        private readonly AppDbContext _context;

        public DiscountRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Discount>> GetAllAsync() =>
            await _context.Discounts.ToListAsync();

        public async Task<Discount?> GetByIdAsync(int id) =>
            await _context.Discounts.FindAsync(id);

        public async Task AddAsync(Discount discount)
        {
            await _context.Discounts.AddAsync(discount);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Discount discount)
        {
            _context.Discounts.Update(discount);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var discount = await GetByIdAsync(id);
            if (discount != null)
            {
                _context.Discounts.Remove(discount);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Discount>> GetExpiredDiscountsAsync()
        {
            return await _context.Discounts
                .Where(d => d.IsActive && d.EndDate <= DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<Discount?> GetByBookIdAsync(int bookId)
        {
            return await _context.Discounts
                .Where(d => d.BookId == bookId)
                .OrderByDescending(d => d.StartDate) // If there are multiple, get the most recent
                .FirstOrDefaultAsync();
        }
    }
}
