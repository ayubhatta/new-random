using BookHavenLibrary.Models;
using BookHavenLibrary.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BookHavenLibrary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiscountFilterController : ControllerBase
    {
        private readonly IDiscountRepository _discountRepository;
        private readonly IBookRepository _bookRepository;

        public DiscountFilterController(IDiscountRepository discountRepository, IBookRepository bookRepository)
        {
            _discountRepository = discountRepository;
            _bookRepository = bookRepository;
        }

        // GET: api/DiscountFilter
        [HttpGet]
        public async Task<IActionResult> GetAllDiscountedBooks()
        {
            var discounts = await _discountRepository.GetAllAsync();
            var now = DateTime.UtcNow;

            var activeDiscounts = discounts
                .Where(d => d.IsActive && d.OnSale && d.StartDate <= now && d.EndDate >= now)
                .ToList();

            var discountedBooks = new List<object>();

            foreach (var discount in activeDiscounts)
            {
                var book = await _bookRepository.GetByIdAsync(discount.BookId);
                if (book == null) continue;

                var discountedPrice = book.Price - (book.Price * discount.DiscountPercentage / 100);

                discountedBooks.Add(new
                {
                    book.Id,
                    book.Title,
                    book.AuthorName,
                    book.Price,
                    book.Format,
                    DiscountedPrice = Math.Round(discountedPrice, 2),
                    discount.DiscountPercentage,
                    discount.StartDate,
                    discount.EndDate,
                    book.CoverImageUrl,
                    book.Inventory.QuantityInStock
                });
            }

            return Ok(new { success = true, data = discountedBooks });
        }

        // GET: api/DiscountFilter/5
        [HttpGet("{bookId}")]
        public async Task<IActionResult> GetDiscountedBookById(int bookId)
        {
            var now = DateTime.UtcNow;
            var discount = await _discountRepository.GetByBookIdAsync(bookId);

            if (discount == null || !discount.IsActive || !discount.OnSale || discount.StartDate > now || discount.EndDate < now)
            {
                return NotFound(new { success = false, message = "No active discount found for this book." });
            }

            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null || book.Inventory == null)
            {
                return NotFound(new { success = false, message = "Book not found or inventory missing." });
            }

            var discountedPrice = book.Price - (book.Price * discount.DiscountPercentage / 100);

            var result = new
            {
                book.Id,
                book.Title,
                book.AuthorName,
                book.Price,
                book.Format,
                DiscountedPrice = Math.Round(discountedPrice, 2),
                discount.DiscountPercentage,
                discount.StartDate,
                discount.EndDate,
                book.CoverImageUrl,
                book.Inventory.QuantityInStock
            };

            return Ok(new { success = true, data = result });
        }

    }
}
