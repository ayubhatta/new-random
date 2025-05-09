using BookHavenLibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookHavenLibrary.Repositories.Interfaces
{
    public interface IAnnouncementRepository
    {
        Task<List<Announcement>> GetAllAsync();
        Task<Announcement> GetByIdAsync(int id);
        Task AddAsync(Announcement announcement);
        Task UpdateAsync(Announcement announcement);
        Task DeleteAsync(Announcement announcement);
    }
}
