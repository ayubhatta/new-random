using BookHavenLibrary.Data;
using BookHavenLibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

public class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _context;

    public InventoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Inventory> CreateAsync(Inventory inventory)
    {
        _context.Inventory.Add(inventory);
        await _context.SaveChangesAsync();
        return inventory;
    }

    public async Task<Inventory> GetByBookIdAsync(int bookId)
    {
        return await _context.Inventory.FirstOrDefaultAsync(i => i.BookId == bookId);
    }

    public async Task<IEnumerable<Inventory>> GetAllAsync()
    {
        return await _context.Inventory.ToListAsync();
    }

    public async Task<bool> UpdateAsync(Inventory inventory)
    {
        _context.Inventory.Update(inventory);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var inventory = await _context.Inventory.FindAsync(id);
        if (inventory == null)
            return false;

        _context.Inventory.Remove(inventory);
        return await _context.SaveChangesAsync() > 0;
    }
}
