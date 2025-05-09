using BookHavenLibrary.DTO;
using BookHavenLibrary.Models;
using BookHavenLibrary.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace BookHavenLibrary.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnnouncementController : ControllerBase
    {
        private readonly IAnnouncementRepository _announcementRepository;
        private readonly UserManager<User> _userManager;

        public AnnouncementController(IAnnouncementRepository announcementRepository, UserManager<User> userManager)
        {
            _announcementRepository = announcementRepository;
            _userManager = userManager;
        }

        // GET: api/Announcement
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var announcements = await _announcementRepository.GetAllAsync();
            if (announcements == null || !announcements.Any())
                return NotFound(new { success = false, message = "No announcements found." });

            return Ok(new { success = true, data = announcements });
        }

        // GET: api/Announcement/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var announcement = await _announcementRepository.GetByIdAsync(id);
            if (announcement == null)
                return NotFound(new { success = false, message = "Announcement not found." });

            return Ok(new { success = true, data = announcement });
        }

        [Authorize(Roles ="admin")]
        // POST: api/Announcement
        [HttpPost]
        public async Task<IActionResult> Create(AnnouncementDto newAnnouncementDTO)
        {
            // Get the current user
            var currentUser = await _userManager.GetUserAsync(User);

            // Check if the user is an Admin
            if (currentUser == null || !await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                return Unauthorized(new { success = false, message = "Only admin users can create an announcement." });
            }

            if (newAnnouncementDTO == null)
                return BadRequest(new { success = false, message = "Invalid data." });

            if (string.IsNullOrEmpty(newAnnouncementDTO.Title) || string.IsNullOrEmpty(newAnnouncementDTO.Content))
                return BadRequest(new { success = false, message = "Title and Content are required." });

            if (newAnnouncementDTO.StartDate >= newAnnouncementDTO.EndDate)
                return BadRequest(new { success = false, message = "Start Date must be earlier than End Date." });

            var announcement = new Announcement
            {
                Title = newAnnouncementDTO.Title,
                Content = newAnnouncementDTO.Content,
                StartDate = newAnnouncementDTO.StartDate,
                EndDate = newAnnouncementDTO.EndDate,
                IsActive = newAnnouncementDTO.IsActive = true, // default to true if not provided
                CreatedBy = currentUser.Id // Assign current user's ID as creator
            };

            await _announcementRepository.AddAsync(announcement);

            return Ok(new { success = true, message = "Announcement created successfully.", data = announcement });
        }


        [Authorize(Roles = "admin")]
        // PUT: api/Announcement/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, AnnouncementDto updatedAnnouncementDTO)
        {
            // Get the current user
            var currentUser = await _userManager.GetUserAsync(User);

            // Check if the user is an Admin
            if (currentUser == null || !await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                return Unauthorized(new { success = false, message = "Only admin users can update an announcement." });
            }

            if (updatedAnnouncementDTO == null)
                return BadRequest(new { success = false, message = "Invalid data." });

            if (string.IsNullOrEmpty(updatedAnnouncementDTO.Title) || string.IsNullOrEmpty(updatedAnnouncementDTO.Content))
                return BadRequest(new { success = false, message = "Title and Content are required." });

            if (updatedAnnouncementDTO.StartDate >= updatedAnnouncementDTO.EndDate)
                return BadRequest(new { success = false, message = "Start Date must be earlier than End Date." });

            var existingAnnouncement = await _announcementRepository.GetByIdAsync(id);
            if (existingAnnouncement == null)
                return NotFound(new { success = false, message = "Announcement not found." });

            existingAnnouncement.Title = updatedAnnouncementDTO.Title;
            existingAnnouncement.Content = updatedAnnouncementDTO.Content;
            existingAnnouncement.StartDate = updatedAnnouncementDTO.StartDate;
            existingAnnouncement.EndDate = updatedAnnouncementDTO.EndDate;
            existingAnnouncement.IsActive = updatedAnnouncementDTO.IsActive = true;

            await _announcementRepository.UpdateAsync(existingAnnouncement);
            return Ok(new { success = true, message = "Announcement updated successfully.", data = existingAnnouncement });
        }


        // DELETE: api/Announcement/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Get the current user
            var currentUser = await _userManager.GetUserAsync(User);

            // Check if the user is an Admin
            if (currentUser == null || !await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                return Unauthorized(new { success = false, message = "Only admin users can delete an announcement." });
            }

            var announcement = await _announcementRepository.GetByIdAsync(id);
            if (announcement == null)
                return NotFound(new { success = false, message = "Announcement not found." });

            await _announcementRepository.DeleteAsync(announcement);
            return Ok(new { success = true, message = "Announcement deleted successfully." });
        }
    }
}
