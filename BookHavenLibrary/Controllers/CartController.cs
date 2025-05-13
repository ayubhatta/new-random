    using BookHavenLibrary.Models;
    using BookHavenLibrary.Repositories;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Security.Claims;

    namespace BookHavenLibrary.Controllers
    {
        [Route("api/[controller]")]
        [ApiController]
        //[Authorize(Roles = "member")]
        public class CartController : ControllerBase
        {
            private readonly ICartRepository _cartRepo;
            private readonly IPurchaseRepository _purchaseRepo;

            public CartController(ICartRepository cartRepo, IPurchaseRepository purchaseRepo)
            {
                _cartRepo = cartRepo;
                _purchaseRepo = purchaseRepo;
            }

            private int GetUserId()
            {
                return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            }

            [HttpGet]
            public async Task<IActionResult> GetCart()
            {
                var userId = GetUserId();
                var cart = await _cartRepo.GetCartByUserIdAsync(userId);
                if (cart == null)
                    return NotFound(new { success = false, message = "No cart found." });

                var items = await _cartRepo.GetCartItemsAsync(cart.Id);

                return Ok(new
                {
                    success = true,
                    message = "Cart fetched successfully.",
                    Cart = new
                    {
                        cart.Id,
                        cart.UserId,
                        cart.IsPaymentDone,
                        cart.CreatedAt,
                        cart.UpdatedAt,
                        cartItems = items.Select(ci => new
                        {
                            ci.Id,
                            ci.Quantity,
                            BookId = ci.BookId,
                            book = new
                            {
                                ci.Book.Title,
                                ci.Book.AuthorName,
                                ci.Book.Price,
                                ci.Book.CoverImageUrl
                            }
                        })
                    }
                });
            }



        [HttpPost("add")]
        public async Task<IActionResult> AddToCart(int bookId, int quantity = 1)
        {
            var userId = GetUserId();
            var success = await _cartRepo.AddToCartAsync(userId, bookId, quantity);

            if (!success)
                return StatusCode(500, new { success = false, message = "Failed to add book to cart." });

            return Ok(new { success = true, message = "Book added to cart." });
        }



        [HttpPut("update/{itemId}")]
        public async Task<IActionResult> UpdateCartItem(int itemId, int quantity)
        {
            if (quantity < 1) return BadRequest(new {success = false,  message = "Quantity must be at least 1." });

            var item = await _cartRepo.GetCartItemByIdAsync(itemId);
            if (item == null) return NotFound(new { success = false, message = "Item not found." });
            
            item.Quantity = quantity;
            item.AddedAt = DateTime.UtcNow;

            await _cartRepo.UpdateCartItemAsync(item);
            return Ok(new { success = true, message = "Cart item updated." });
        }

        [HttpDelete("remove/{itemId}")]
        public async Task<IActionResult> RemoveCartItem(int itemId)
        {
            await _cartRepo.RemoveCartItemAsync(itemId);
            return Ok(new { success = true, message = "Cart item removed." });
        }

        [HttpDelete("cancel")]
        public async Task<IActionResult> CancelCart()
        {
            var userId = GetUserId();
            await _cartRepo.CancelCartAsync(userId);
            return Ok(new { success = true, message = "Cart canceled." });
        }

        [HttpGet("paid")]
        //[Authorize(Roles = "admin")] // Optional
        public async Task<IActionResult> GetPaidPurchases()
        {
            var purchases = await _purchaseRepo.GetAllPurchasesAsync();

            var grouped = purchases
                .GroupBy(p => p.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Purchases = g.Select(p => new
                    {
                        p.Id,
                        p.BookId,
                        p.PurchaseDate,
                        book = new
                        {
                            p.Book.Title,
                            p.Book.AuthorName,
                            p.Book.Price,
                            p.Book.CoverImageUrl
                        }
                    }).ToList()
                });

            return Ok(new
            {
                success = true,
                message = "Purchase history fetched successfully.",
                users = grouped
            });
        }

    }
}
