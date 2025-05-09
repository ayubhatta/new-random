using BookHavenLibrary.DTO;
using BookHavenLibrary.Models;
using BookHavenLibrary.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly IReviewRepository _reviewRepo;
    private readonly ICartRepository _cartRepo;
    private readonly IPurchaseRepository _purchaseRepo;

    public ReviewController(IReviewRepository reviewRepo, ICartRepository cartRepo, IPurchaseRepository purchaseRepo)
    {
        _reviewRepo = reviewRepo;
        _cartRepo = cartRepo;
        _purchaseRepo = purchaseRepo;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var reviews = await _reviewRepo.GetAllAsync();
            return Ok(new { success = true, message = "Successfully fetched all reviews.", data = reviews });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var review = await _reviewRepo.GetByIdAsync(id);
        if (review == null) 
            return NotFound(new {success = false, message = "No review found."});
        return Ok(new { success = true, message = "Successfully fetched revied.", data = review });
    }


    [HttpGet("book/{bookId}")]
    public async Task<IActionResult> GetByBook(int bookId)
    {
        var reviews = await _reviewRepo.GetByBookIdAsync(bookId);
        return Ok(new { success = true, message = "Successfully fetched by book Id.", data = reviews });
    }


    [Authorize(Roles = "member")]
    [HttpPost]
    public async Task<IActionResult> Create(ReviewDto dto)
    {
        var userId = GetUserId();

        var hasPurchasedBook = await _purchaseRepo.HasUserPurchasedBook(userId, dto.BookId);
        if (!hasPurchasedBook)
            return BadRequest(new { success = false, message = "You can only review books you've purchased." });

        var review = new Review
        {
            UserId = userId,
            BookId = dto.BookId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            ReviewDate = DateTime.UtcNow,
            IsVerifiedPurchase = true
        };

        await _reviewRepo.AddAsync(review);
        return Ok(new { success = true, message = "Review added.", data = review });
    }



    [Authorize(Roles = "member")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ReviewDto dto)
    {
        var userId = GetUserId();
        var review = await _reviewRepo.GetByIdAsync(id);
        if (review == null || review.UserId != userId)
            return NotFound(new { success = false, message = "Review not found or not yours." });

        review.Rating = dto.Rating;
        review.Comment = dto.Comment;
        review.ReviewDate = DateTime.UtcNow;

        await _reviewRepo.UpdateAsync(review);
        return Ok(new { success = true, message = "Review updated.", data = review });
    }



    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var review = await _reviewRepo.GetByIdAsync(id);
        if (review == null || review.UserId != userId)
            return NotFound(new { success = false, message = "Review not found or not yours." });

        await _reviewRepo.DeleteAsync(id);
        return Ok(new { success = true, message = "Review deleted." });
    }
}
