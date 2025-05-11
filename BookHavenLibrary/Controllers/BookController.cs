using BookHavenLibrary.Models;
using BookHavenLibrary.DTOs;
using BookHavenLibrary.Repositories;
using Microsoft.AspNetCore.Mvc;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json.Serialization;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization;


namespace BookHavenLibrary.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BookController : ControllerBase
    {
        private readonly IBookRepository _bookRepository;
        private readonly IInventoryRepository _inventoryRepository;
        private const int MaxImageWidth = 1024;  // Max width for resizing imagesn 
        private readonly ILogger<BookController> _logger;


        public BookController(IBookRepository bookRepository, IInventoryRepository inventoryRepository ,ILogger<BookController> logger)
        {
            _bookRepository = bookRepository;
            _inventoryRepository = inventoryRepository;
            _logger = logger;
        }


        public class ImgBBResponse
        {
            public ImgBBData Data { get; set; }
            public bool Success { get; set; }
            public int Status { get; set; }
        }

        public class ImgBBData
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string UrlViewer { get; set; }
            [JsonPropertyName("url")]
            public string Url { get; set; }
            public string DisplayUrl { get; set; }
            public string Width { get; set; }
            public string Height { get; set; }
            public string Size { get; set; }
            public string Time { get; set; }
            public string Expiration { get; set; }
            public ImgBBImage Image { get; set; }
            public ImgBBImage Thumb { get; set; }
            public ImgBBImage Medium { get; set; }
            public string DeleteUrl { get; set; }
        }

        public class ImgBBImage
        {
            public string Filename { get; set; }
            public string Name { get; set; }
            public string Mime { get; set; }
            public string Extension { get; set; }
            public string Url { get; set; }
        }


        // Method to resize images if they exceed a certain size
        private async Task<byte[]> ResizeImage(IFormFile image, int maxWidth = MaxImageWidth)
        {
            using var imageStream = image.OpenReadStream();
            using var imageToResize = SixLabors.ImageSharp.Image.Load(imageStream);

            // Resize the image to the max width while maintaining the aspect ratio
            if (imageToResize.Width > maxWidth)
            {
                imageToResize.Mutate(x => x.Resize(maxWidth, 0)); // Resize keeping aspect ratio
            }

            using var outputStream = new MemoryStream();
            imageToResize.Save(outputStream, new JpegEncoder()); // Save as JPEG
            return outputStream.ToArray();
        }

        // Upload image to ImgBB and get URL
        private async Task<string> UploadToImgBB(IFormFile file)
        {
            string apiKey = "a8c93b04cdaa8cc053ddb80b9509d5f7";  // Replace with your actual API key
            string url = $"https://api.imgbb.com/1/upload?key={apiKey}";

            using var client = new HttpClient();
            using var content = new MultipartFormDataContent();

            // Create a StreamContent for the file
            var imageContent = new StreamContent(file.OpenReadStream());
            content.Add(imageContent, "image", file.FileName);

            // Post the request to ImgBB API
            var response = await client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"ImgBB upload failed: {await response.Content.ReadAsStringAsync()}");
                return null;
            }

            // Deserialize the response body
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("ImgBB Response: {ResponseBody}", responseBody);

            try
            {
                // Manually parse the response body using JsonDocument
                var jsonResponse = JsonDocument.Parse(responseBody);

                // Extract the URL from the response
                var imgUrl = jsonResponse.RootElement
                    .GetProperty("data")
                    .GetProperty("url")
                    .GetString();

                // Check if the URL exists and return it
                if (!string.IsNullOrEmpty(imgUrl))
                {
                    _logger.LogInformation("ImgBB Image URL: {ImgUrl}", imgUrl);
                    return imgUrl;
                }
                else
                {
                    _logger.LogError("ImgBB response does not contain a valid URL.");
                    return null;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Failed to parse ImgBB response: {ex.Message}");
                return null;
            }
        }


        [HttpGet("GetAllBooks")]
       
        public async Task<IActionResult> GetAllBooks([FromQuery] int? draw = 1, [FromQuery] int? start = 0, [FromQuery] int? length = 10, [FromQuery] string search = "")
        {
            try
            {
                // Fetch books and join with Inventory
                var books = await _bookRepository.GetAllAsync(); // Get list of all books
                var query = books.AsQueryable();

                // Apply search filter (modify fields based on your Book model)
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(b =>
                        b.Title.Contains(search) ||
                        b.AuthorName.Contains(search) ||
                        b.PublisherName.Contains(search) ||
                        b.ISBN.Contains(search) ||
                        b.Language.Contains(search));
                }

                // Join with Inventory data
                var booksWithInventory = query.Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.Description,
                    b.AuthorName,
                    b.PublisherName,
                    b.ISBN,
                    b.Price,
                    b.Format,
                    b.Language,
                    b.PublicationDate,
                    b.PageCount,
                    b.IsBestseller,
                    b.IsAwardWinner,
                    b.IsNewRelease,
                    b.NewArrival,
                    b.CommingSoon,
                    b.CoverImageUrl,
                    b.CreatedAt,
                    b.UpdatedAt,
                    b.IsActive,
                    Inventory = new
                    {
                        b.Inventory.QuantityInStock,
                        b.Inventory.ReorderThreshold,
                        b.Inventory.IsAvailable
                    }
                }).ToList();

                // Total count (after filtering)
                var totalCount = query.Count();

                // Apply pagination
                var paginatedData = booksWithInventory
                    .Skip(start.GetValueOrDefault())
                    .Take(length.GetValueOrDefault())
                    .ToList();

                var response = new
                {
                    draw = draw.GetValueOrDefault(),
                    recordsTotal = totalCount,
                    recordsFiltered = totalCount,
                    data = paginatedData
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", details = ex.Message });
            }
        }





        [HttpGet("{id}")]
        
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                // Fetch the book by id and include Inventory data
                var book = await _bookRepository.GetByIdAsync(id);
                if (book == null)
                    return NotFound(new { success = false, message = "Book not found." });

                // Include Inventory data in the response
                var bookWithInventory = new
                {
                    book.Id,
                    book.Title,
                    book.Description,
                    book.AuthorName,
                    book.PublisherName,
                    book.ISBN,
                    book.Price,
                    book.Format,
                    book.Language,
                    book.PublicationDate,
                    book.PageCount,
                    book.IsBestseller,
                    book.IsAwardWinner,
                    book.IsNewRelease,
                    book.NewArrival,
                    book.CommingSoon,
                    book.CoverImageUrl,
                    book.CreatedAt,
                    book.UpdatedAt,
                    book.IsActive,
                    Inventory = new
                    {
                        book.Inventory.QuantityInStock,
                        book.Inventory.ReorderThreshold,
                        book.Inventory.IsAvailable
                    }
                };

                return Ok(new { success = true, message = "Book retrieved successfully.", data = bookWithInventory });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }




        [HttpPost("create")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromForm] BookCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid data submitted." });

                // Validate image upload
                if (dto.CoverImage == null)
                {
                    return BadRequest(new { success = false, message = "Cover image is required." });
                }

                // Resize the image (optional step depending on your requirement)
                var resizedImageBytes = await ResizeImage(dto.CoverImage);

                // Upload the image to ImgBB and get the URL
                var imgBBUrl = await UploadToImgBB(dto.CoverImage);
                if (string.IsNullOrEmpty(imgBBUrl))
                {
                    return StatusCode(500, new { success = false, message = "Failed to upload cover image to ImgBB." });
                }

                // Create the book object
                var book = new Book
                {
                    ISBN = dto.ISBN,
                    Title = dto.Title,
                    Description = dto.Description,
                    AuthorName = dto.AuthorName,
                    PublisherName = dto.PublisherName,
                    Price = dto.Price,
                    Format = dto.Format,
                    Language = dto.Language,
                    PublicationDate = dto.PublicationDate.Kind == DateTimeKind.Utc
                      ? dto.PublicationDate
                      : dto.PublicationDate.ToUniversalTime(),
                    PageCount = dto.PageCount,
                    IsBestseller = dto.IsBestseller,
                    IsAwardWinner = dto.IsAwardWinner,
                    IsNewRelease = dto.IsNewRelease,
                    NewArrival = dto.NewArrival,
                    CommingSoon = dto.CommingSoon,
                    CoverImageUrl = imgBBUrl, // Store the ImgBB URL directly in the Book model
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = dto.IsActive
                };

                // Save the book to the database
                var result = await _bookRepository.CreateAsync(book);

                if (result == null)
                {
                    return BadRequest(new { success = false, message = "Failed to create book." });
                }

                // Create associated inventory record
                var inventory = new Inventory
                {
                    BookId = result.Id,
                    QuantityInStock = dto.Quantity,
                    LastStockedDate = DateTime.UtcNow,
                    ReorderThreshold = dto.ReorderThreshold,
                    IsAvailable = dto.Quantity > 0
                };

                await _inventoryRepository.CreateAsync(inventory);

                return CreatedAtAction(nameof(GetById), new { id = book.Id }, new { success = true, message = "Book created successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating the book.");
                return StatusCode(500, new { success = false, message = "Internal server error." });
            }
        }



        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, [FromForm] BookUpdateDto dto)
        {
            try
            {
                var book = await _bookRepository.GetByIdAsync(id);
                if (book == null)
                    return NotFound(new { success = false, message = "Book not found." });

                // Update only provided fields
                if (!string.IsNullOrWhiteSpace(dto.Title))
                    book.Title = dto.Title;

                if (!string.IsNullOrWhiteSpace(dto.Description))
                    book.Description = dto.Description;

                if (!string.IsNullOrWhiteSpace(dto.AuthorName))
                    book.AuthorName = dto.AuthorName;

                if (!string.IsNullOrWhiteSpace(dto.PublisherName))
                    book.PublisherName = dto.PublisherName;

                if (dto.Price.HasValue)
                    book.Price = dto.Price.Value;

                if (!string.IsNullOrWhiteSpace(dto.Format))
                    book.Format = dto.Format;

                if (!string.IsNullOrWhiteSpace(dto.Language))
                    book.Language = dto.Language;

                if (dto.PublicationDate.HasValue)
                {
                    book.PublicationDate = dto.PublicationDate.Value.Kind == DateTimeKind.Utc
                        ? dto.PublicationDate.Value
                        : dto.PublicationDate.Value.ToUniversalTime();
                }

                if (dto.PageCount.HasValue)
                    book.PageCount = dto.PageCount.Value;

                if (dto.IsBestseller.HasValue)
                    book.IsBestseller = dto.IsBestseller.Value;

                if (dto.IsAwardWinner.HasValue)
                    book.IsAwardWinner = dto.IsAwardWinner.Value;

                if (dto.IsNewRelease.HasValue)
                    book.IsNewRelease = dto.IsNewRelease.Value;

                if (dto.NewArrival.HasValue)
                    book.NewArrival = dto.NewArrival.Value;

                if (dto.CommingSoon.HasValue)
                    book.CommingSoon = dto.CommingSoon.Value;

                if (dto.IsActive.HasValue)
                    book.IsActive = dto.IsActive.Value;

                book.UpdatedAt = DateTime.UtcNow;

                // Handle new cover image
                if (dto.CoverImage != null)
                {
                    var imgBBUrl = await UploadToImgBB(dto.CoverImage);
                    if (string.IsNullOrEmpty(imgBBUrl))
                        return StatusCode(500, new { success = false, message = "Failed to upload cover image to ImgBB." });

                    book.CoverImageUrl = imgBBUrl;
                }

                // Update inventory if needed
                var inventory = await _inventoryRepository.GetByBookIdAsync(book.Id);
                if (inventory != null)
                {
                    bool inventoryUpdated = false;

                    if (dto.Quantity.HasValue)
                    {
                        inventory.QuantityInStock = dto.Quantity.Value;
                        inventory.IsAvailable = dto.Quantity.Value > 0;
                        inventoryUpdated = true;
                    }

                    if (dto.ReorderThreshold.HasValue)
                    {
                        inventory.ReorderThreshold = dto.ReorderThreshold.Value;
                        inventoryUpdated = true;
                    }

                    if (inventoryUpdated)
                    {
                        inventory.LastStockedDate = DateTime.UtcNow;
                        await _inventoryRepository.UpdateAsync(inventory);
                    }
                }

                await _bookRepository.UpdateAsync(book);

                return Ok(new { success = true, message = "Book and inventory updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }




        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var book = await _bookRepository.GetByIdAsync(id);
                if (book == null)
                    return NotFound(new { success = false, message = "Book not found." });

                var deleted = await _bookRepository.DeleteAsync(id);
                if (!deleted)
                    return BadRequest(new { success = false, message = "Failed to delete the book." });

                return Ok(new { success = true, message = "Book deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

    }
}
