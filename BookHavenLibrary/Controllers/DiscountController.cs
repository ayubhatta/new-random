using BookHavenLibrary.Models;
using BookHavenLibrary.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookHavenLibrary.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiscountController : ControllerBase
    {
        private readonly IDiscountRepository _discountRepo;
        private readonly UserManager<User> _userManager;

        public DiscountController(IDiscountRepository discountRepo, UserManager<User> userManager)
        {
            _discountRepo = discountRepo;
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
            return Ok(new {success = true, message = "Discounts fetched successfully", data = discounts });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var discount = await _discountRepo.GetByIdAsync(id);
            if (discount == null)
                return NotFound(new { success = false, message = "No discount found"});
            return Ok(new { success = true, message = "Discounts fetched successfully", data = discount });
        }


        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Create(DiscountDto discountDto)
        {
            if (discountDto.StartDate >= discountDto.EndDate)
                return BadRequest(new { success = false, message = "Start date must be before end date." });

            var discount = new Discount
            {
                BookId = discountDto.BookId,
                DiscountPercentage = discountDto.DiscountPercentage,
                StartDate = discountDto.StartDate,
                EndDate = discountDto.EndDate,
                OnSale = discountDto.OnSale,
                IsActive = discountDto.IsActive,
                UserId = GetUserId()
            };

            await _discountRepo.AddAsync(discount);
            return Ok(new { success = true, message = "Discount created.", data = discount });
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, DiscountDto discountDto)
        {
            var existing = await _discountRepo.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new {success = false, message = "No discount found." });

            existing.BookId = discountDto.BookId;
            existing.DiscountPercentage = discountDto.DiscountPercentage;
            existing.StartDate = discountDto.StartDate;
            existing.EndDate = discountDto.EndDate;
            existing.OnSale = discountDto.OnSale;
            existing.IsActive = discountDto.IsActive;

            await _discountRepo.UpdateAsync(existing);
            return Ok(new { success = true, message = "Discount updated." });
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _discountRepo.DeleteAsync(id);
            return Ok(new { success = true, message = "Discount deleted." });
        }
    }
}
