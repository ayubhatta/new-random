using BookHavenLibrary.Data;
using BookHavenLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace BookHavenLibrary.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly AppDbContext _context;

        public ReviewRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Review>> GetAllAsync() =>
            await _context.Reviews.ToListAsync();

        public async Task<Review?> GetByIdAsync(int id) =>
            await _context.Reviews.FindAsync(id);

        public async Task<List<Review>> GetByBookIdAsync(int bookId) =>
            await _context.Reviews.Where(r => r.BookId == bookId).ToListAsync();

        public async Task AddAsync(Review review)
        {
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Review review)
        {
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }
        }

    }

}
