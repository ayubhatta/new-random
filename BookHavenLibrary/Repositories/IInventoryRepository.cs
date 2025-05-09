using BookHavenLibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IInventoryRepository
{
    Task<Inventory> CreateAsync(Inventory inventory);
    Task<Inventory> GetByBookIdAsync(int bookId);
    Task<IEnumerable<Inventory>> GetAllAsync();
    Task<bool> UpdateAsync(Inventory inventory);
    Task<bool> DeleteAsync(int id);
}
