using BookHavenLibrary.Data;
using BookHavenLibrary.DTO;
using BookHavenLibrary.Models;
using BookHavenLibrary.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace BookHavenLibrary.Controllers
{
    [Authorize(Roles = "admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBookRepository _bookRepository;
        private readonly AppDbContext _context;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ICategoryRepository categoryRepository,IBookRepository bookRepository ,AppDbContext appDbContext,ILogger<CategoryController> logger)
        {
            _categoryRepository = categoryRepository;
            _bookRepository = bookRepository;
            _context = appDbContext;
            _logger = logger;
        }

        // Get all categories
        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                var categories = await _categoryRepository.GetAllAsync();
                var categoriesDto = categories.Select(c => new CategoryDto
                {
                    Name = c.Name,
                    Description = c.Description
                });

                return Ok(new { success = true, data = categoriesDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching categories.");
                return StatusCode(500, new { success = false, message = "Internal server error." });
            }
        }

        // Get category by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                    return NotFound(new { success = false, message = "Category not found." });

                var categoryDto = new CategoryDto
                {
                    Name = category.Name,
                    Description = category.Description
                };

                return Ok(new { success = true, data = categoryDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching category.");
                return StatusCode(500, new { success = false, message = "Internal server error." });
            }
        }

        // Create a new category
        [HttpPost]
        public async Task<IActionResult> CreateCategory(CategoryDto categoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid data submitted." });

                var category = new Category
                {
                    Name = categoryDto.Name,
                    Description = categoryDto.Description
                };

                var result = await _categoryRepository.CreateAsync(category);

                var resultDto = new CategoryDto
                {
                    Name = result.Name,
                    Description = result.Description
                };

                return CreatedAtAction(nameof(GetCategoryById), new { id = result.Id }, new { success = true, message = "Category created successfully!", data = resultDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating category.");
                return StatusCode(500, new { success = false, message = "Internal server error." });
            }
        }

        // Update an existing category
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateDto categoryDto)
        {
            try
            {
                var existingCategory = await _categoryRepository.GetByIdAsync(id);
                if (existingCategory == null)
                    return NotFound(new { success = false, message = "Category not found." });

                existingCategory.Name = categoryDto.Name;
                existingCategory.Description = categoryDto.Description;

                var updatedCategory = await _categoryRepository.UpdateAsync(existingCategory);

                var updatedCategoryDto = new CategoryDto
                {
                    Name = updatedCategory.Name,
                    Description = updatedCategory.Description
                };

                return Ok(new { success = true, message = "Category updated successfully!", data = updatedCategoryDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating category.");
                return StatusCode(500, new { success = false, message = "Internal server error." });
            }
        }

        // Delete a category
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var result = await _categoryRepository.DeleteAsync(id);
                if (!result)
                    return NotFound(new { success = false, message = "Category not found." });

                return Ok(new { success = true, message = "Category deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting category.");
                return StatusCode(500, new { success = false, message = "Internal server error." });
            }
        }

        [HttpPost("assign-categories/{bookId}")]
        public async Task<IActionResult> AssignCategoriesToBook(int bookId, [FromBody] List<int> categoryIds)
        {
            try
            {
                var book = await _bookRepository.GetByIdAsync(bookId);
                if (book == null)
                    return NotFound(new { success = false, message = "Book not found." });

                var categories = await _categoryRepository.GetAllAsync();
                var validCategories = categories.Where(c => categoryIds.Contains(c.Id)).ToList();

                if (validCategories.Count != categoryIds.Count)
                    return BadRequest(new { success = false, message = "Some categories are invalid." });

                // Create book-category associations
                var bookCategories = validCategories.Select(c => new BookCategory
                {
                    BookId = book.Id,
                    CategoryId = c.Id
                }).ToList();

                // Add associations to the database
                await _context.BookCategories.AddRangeAsync(bookCategories);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Categories assigned to the book successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while assigning categories to book.");
                return StatusCode(500, new { success = false, message = "Internal server error." });
            }
        }


        [HttpGet("category/{categoryId}/books")]
        public async Task<IActionResult> GetBooksByCategory(int categoryId)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(categoryId);
                if (category == null)
                    return NotFound(new { success = false, message = "Category not found." });

                var books = await _context.BookCategories
                    .Where(bc => bc.CategoryId == categoryId)
                    .Select(bc => bc.Book)
                    .ToListAsync();

                return Ok(new { success = true, data = books });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching books by category.");
                return StatusCode(500, new { success = false, message = "Internal server error." });
            }
        }
    }
}
