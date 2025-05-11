using BookHavenLibrary.Models;
using BookHavenLibrary.Repositories;
using BookHavenLibrary.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        [Authorize(Roles = "member")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { success = false, message = "Invalid user token." });

            var bookmarks = await _bookmarkRepository.GetByUserIdAsync(userId);
            return Ok(new { success = true, message = "Bookmarks fetched successfully.", data = bookmarks });
        }

        [Authorize(Roles = "member")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { success = false, message = "Invalid user token." });

            var bookmark = await _bookmarkRepository.GetByIdAsync(id);
            if (bookmark == null || bookmark.UserId != userId)
                return NotFound(new { success = false, message = "Bookmark not found or access denied." });

            return Ok(new { success = true, message = "Bookmark fetched successfully.", data = bookmark });
        }



        [Authorize(Roles = "member")]
        [HttpPost]
        public async Task<IActionResult> Create(int bookId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { success = false, message = "Invalid user token." });

            if (bookId <= 0)
                return BadRequest(new { success = false, message = "Invalid BookId." });

            var user = await _userManager.FindByIdAsync(userIdStr);
            if (user == null)
                return NotFound(new { success = false, message = "User not found." });

            if (!await _userManager.IsInRoleAsync(user, "member"))
                return Forbid("Only members can bookmark.");

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
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, int bookId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { success = false, message = "Invalid user token." });

            var bookmark = await _bookmarkRepository.GetByIdAsync(id);
            if (bookmark == null || bookmark.UserId != userId)
                return NotFound(new { success = false, message = "Bookmark not found or access denied." });

            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
                return NotFound(new { success = false, message = "Book not found." });

            bookmark.BookId = bookId;
            bookmark.CreatedAt = DateTime.UtcNow;

            await _bookmarkRepository.UpdateAsync(bookmark);
            return Ok(new { success = true, message = "Bookmark updated successfully.", data = bookmark });
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
