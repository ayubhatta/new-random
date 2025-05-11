using BookHavenLibrary.Data;
using BookHavenLibrary.Models;
using BookHavenLibrary.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookHavenLibrary.Repositories
{
    public class BookmarkRepository : IBookmarkRepository
    {
        private readonly AppDbContext _context;

        public BookmarkRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Bookmark>> GetAllAsync()
        {
            return await _context.Bookmarks.ToListAsync();
        }

        public async Task<Bookmark?> GetByIdAsync(int id)
        {
            return await _context.Bookmarks.FindAsync(id);
        }

        public async Task AddAsync(Bookmark bookmark)
        {
            await _context.Bookmarks.AddAsync(bookmark);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Bookmark bookmark)
        {
            _context.Bookmarks.Update(bookmark);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Bookmark bookmark)
        {
            _context.Bookmarks.Remove(bookmark);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Bookmark>> GetByUserIdAsync(int userId) // ✅ New method
        {
            return await _context.Bookmarks
                .Where(b => b.UserId == userId)
                .ToListAsync();
        }

    }
}
