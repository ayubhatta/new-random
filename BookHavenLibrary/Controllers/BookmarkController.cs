using BookHavenLibrary.Models;
using BookHavenLibrary.Repositories;
using BookHavenLibrary.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookHavenLibrary.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookmarkController : ControllerBase
    {
        private readonly IBookmarkRepository _bookmarkRepository;
        private readonly UserManager<User> _userManager; // Use the UserManager for Identity framework
        private readonly IBookRepository _bookRepository;

        public BookmarkController(IBookmarkRepository bookmarkRepository, UserManager<User> userManager, IBookRepository bookRepository)
        {
            _bookmarkRepository = bookmarkRepository;
            _userManager = userManager;
            _bookRepository = bookRepository;
        }

        // GET: api/Bookmark
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var bookmarks = await _bookmarkRepository.GetAllAsync();
            return Ok(new { success = true, message = "Bookmarks fetched successfully.", data = bookmarks });
        }

        // GET: api/Bookmark/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var bookmark = await _bookmarkRepository.GetByIdAsync(id);
            if (bookmark == null)
                return NotFound(new { success = false, message = "Bookmark not found." });

            return Ok(new { success = true, message= "Bookmarks fetched successfully.", data = bookmark });
        }


        [Authorize(Roles = "member")]
        // POST: api/Bookmark?userId=1
        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] int bookId)
        {
            if (userId <= 0 || bookId <= 0)
                return BadRequest(new { success = false, message = "Invalid UserId or BookId." });

            // Check if the user exists
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return NotFound(new { success = false, message = "User not found." });

            // Check if the user is a member
            if (!await _userManager.IsInRoleAsync(user, "member"))
                return BadRequest(new { success = false, message = "Only members can bookmark." });

            // Check if the book exists
            var bookExists = await _bookRepository.GetByIdAsync(bookId);
            if (bookExists == null)
                return NotFound(new { success = false, message = "Book not found." });

            var bookmark = new Bookmark
            {
                UserId = userId,
                BookId = bookId,
                CreatedAt = DateTime.UtcNow
            };

            await _bookmarkRepository.AddAsync(bookmark);
            return Ok(new { success = true, message = "Bookmark created successfully.", data = bookmark });
        }


        [Authorize(Roles = "member")]
        // PUT: api/Bookmark/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Bookmark updatedBookmark)
        {
            var existing = await _bookmarkRepository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { success = false, message = "Bookmark not found." });

            if (updatedBookmark.UserId <= 0 || updatedBookmark.BookId <= 0)
                return BadRequest(new { success = false, message = "Invalid UserId or BookId." });

            // Check if the user exists
            var user = await _userManager.FindByIdAsync(updatedBookmark.UserId.ToString());
            if (user == null)
                return NotFound(new { success = false, message = "User not found." });

            // Check if the user is a member
            if (!await _userManager.IsInRoleAsync(user, "member"))
                return BadRequest(new { success = false, message = "Only members can update bookmarks." });

            // Check if the book exists
            var bookExists = await _bookRepository.GetByIdAsync(updatedBookmark.BookId);
            if (bookExists == null)
                return NotFound(new { success = false, message = "Book not found." });

            existing.UserId = updatedBookmark.UserId;
            existing.BookId = updatedBookmark.BookId;
            existing.CreatedAt = updatedBookmark.CreatedAt;

            await _bookmarkRepository.UpdateAsync(existing);
            return Ok(new { success = true, message = "Bookmark updated successfully.", data = existing });
        }

        // DELETE: api/Bookmark/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var bookmark = await _bookmarkRepository.GetByIdAsync(id);
            if (bookmark == null)
                return NotFound(new { success = false, message = "Bookmark not found." });

            await _bookmarkRepository.DeleteAsync(bookmark);
            return Ok(new { success = true, message = "Bookmark deleted successfully." });
        }
    }
}
