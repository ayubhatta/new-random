using BookHavenLibrary.Models;
using BookHavenLibrary.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookHavenLibrary.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DiscountController : ControllerBase
    {
        private readonly IDiscountRepository _discountRepo;
        private readonly IBookRepository _bookRepo;
        private readonly UserManager<User> _userManager;

        public DiscountController(IDiscountRepository discountRepo, IBookRepository bookRepo, UserManager<User> userManager)
        {
            _discountRepo = discountRepo;
            _bookRepo = bookRepo;
            _userManager = userManager;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var discounts = await _discountRepo.GetAllAsync();
            if (discounts == null || !discounts.Any())
                return NotFound(new { success = false, message = "No discounts found." });
            return Ok(new { success = true, message = "Discounts fetched successfully", data = discounts });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var discount = await _discountRepo.GetByIdAsync(id);
            if (discount == null)
                return NotFound(new { success = false, message = "No discount found" });
            return Ok(new { success = true, message = "Discounts fetched successfully", data = discount });
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create(DiscountDto discountDto)
        {
            try
            {
                // Log incoming data for debugging
                Console.WriteLine($"📥 Incoming discount data: {System.Text.Json.JsonSerializer.Serialize(discountDto)}");

                // Validate date range
                if (discountDto.StartDate >= discountDto.EndDate)
                    return BadRequest(new { success = false, message = "Start date must be before end date." });

                // Validate book exists
                Console.WriteLine("📘 Checking book existence...");
                var book = await _bookRepo.GetByIdAsync(discountDto.BookId);
                if (book == null)
                    return NotFound(new { success = false, message = "Book not found." });

                // Get current user ID from claims
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { success = false, message = "Invalid or missing user token." });

                Console.WriteLine($"👤 Creating discount for userId {userId} and bookId {discountDto.BookId}");

                // Create new discount object
                var discount = new Discount
                {
                    BookId = discountDto.BookId,
                    // Convert from double to decimal safely
                    DiscountPercentage = Convert.ToDecimal(discountDto.DiscountPercentage),
                    StartDate = discountDto.StartDate,
                    EndDate = discountDto.EndDate,
                    OnSale = discountDto.OnSale,
                    IsActive = discountDto.IsActive,
                    UserId = userId
                };

                await _discountRepo.AddAsync(discount);

                Console.WriteLine("✅ Discount saved successfully.");
                return Ok(new { success = true, message = "Discount created successfully.", data = discount });
            }
            catch (Exception ex)
            {
                // Log detailed exception info
                Console.WriteLine("❌ FULL EXCEPTION: " + ex.ToString());
                Console.WriteLine("📦 Payload: " + System.Text.Json.JsonSerializer.Serialize(discountDto));
                Console.WriteLine("👤 Claims: " + string.Join(", ", User.Claims.Select(c => $"{c.Type}:{c.Value}")));

                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while creating the discount. See server logs for details."
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, [FromBody] DiscountDto discountDto)
        {
            try
            {
                var existing = await _discountRepo.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { success = false, message = "No discount found." });

                existing.BookId = discountDto.BookId;
                existing.DiscountPercentage = Convert.ToDecimal(discountDto.DiscountPercentage);
                existing.StartDate = discountDto.StartDate;
                existing.EndDate = discountDto.EndDate;
                existing.OnSale = discountDto.OnSale;
                existing.IsActive = discountDto.IsActive;

                await _discountRepo.UpdateAsync(existing);
                return Ok(new { success = true, message = "Discount updated successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ FULL EXCEPTION: " + ex.ToString());
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating the discount."
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var discount = await _discountRepo.GetByIdAsync(id);
                if (discount == null)
                    return NotFound(new { success = false, message = "Discount not found." });

                await _discountRepo.DeleteAsync(id);
                return Ok(new { success = true, message = "Discount deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ FULL EXCEPTION: " + ex.ToString());
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while deleting the discount."
                });
            }
        }
    }
}