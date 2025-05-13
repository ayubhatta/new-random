using BookHavenLibrary.Data;
using BookHavenLibrary.Dto;
using BookHavenLibrary.DTO;
using BookHavenLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;

namespace BookHavenLibrary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly IConfiguration _configuration;
        public OrderController(AppDbContext context, IHubContext<OrderHub> hubContext, IConfiguration configuration)
        {
            _context = context;
            _hubContext = hubContext;
            _configuration = configuration;
        }


        [Authorize(Roles = "member")]
        [HttpPost("place")]
        public async Task<IActionResult> PlaceOrder()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Get the user's cart and items
            var cart = await _context.ShoppingCarts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null) return BadRequest("No cart found.");

            var cartItems = await _context.CartItems.Where(ci => ci.ShoppingCartId == cart.Id).ToListAsync();
            if (!cartItems.Any()) return BadRequest("Your cart is empty.");

            var bookIds = cartItems.Select(ci => ci.BookId).ToList();
            var books = await _context.Books.Where(b => bookIds.Contains(b.Id)).ToListAsync();

            decimal total = cartItems.Sum(ci =>
            {
                var book = books.FirstOrDefault(b => b.Id == ci.BookId);
                return book != null ? book.Price * ci.Quantity : 0;
            });

            decimal discountAmount = await CalculateDiscountAsync(userId, cartItems.Sum(ci => ci.Quantity), total);

            var orderItems = cartItems.Select(ci =>
            {
                var book = books.First(b => b.Id == ci.BookId);
                return new OrderItem
                {
                    BookId = book.Id,
                    Quantity = ci.Quantity,
                    PriceAtOrder = book.Price,
                    Subtotal = book.Price * ci.Quantity
                };
            }).ToList();

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = total,
                DiscountAmount = discountAmount,
                FinalAmount = total - discountAmount,
                ClaimCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                Status = "pending",
                OrderItems = orderItems
            };

            _context.Orders.Add(order);

            // Update inventory stock
            var inventoryItems = await _context.Inventory
                .Where(i => bookIds.Contains(i.BookId))
                .ToListAsync();

            foreach (var ci in cartItems)
            {
                var inventory = inventoryItems.FirstOrDefault(i => i.BookId == ci.BookId);
                if (inventory != null)
                {
                    inventory.QuantityInStock -= ci.Quantity;
                    if (inventory.QuantityInStock < 0) inventory.QuantityInStock = 0; // Optional safeguard
                }
            }

            // Store purchase history
            foreach (var ci in cartItems)
            {
                var alreadyPurchased = await _context.Purchases
                    .AnyAsync(p => p.UserId == userId && p.BookId == ci.BookId);

                if (!alreadyPurchased)
                {
                    var purchase = new Purchase
                    {
                        UserId = userId,
                        BookId = ci.BookId,
                        PurchaseDate = DateTime.UtcNow
                    };
                    _context.Purchases.Add(purchase);
                }
            }


            // Clear the cart after placing the order
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            // Broadcast order
            await _hubContext.Clients.All.SendAsync("OrderPlaced", new
            {
                message = $" New order placed by Member #{userId}.",
                orderId = order.Id,
                books = orderItems.Select(oi => new
                {
                    oi.BookId,
                    Title = books.First(b => b.Id == oi.BookId).Title,
                    Author = books.First(b => b.Id == oi.BookId).AuthorName
                }).ToList()
            });


            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                await SendOrderEmailAsync(order, user.Email);
            }

            return Ok(new
            {
                message = "Order placed successfully.",
                orderId = order.Id,
                claimCode = order.ClaimCode,
                total = order.TotalAmount,
                discount = order.DiscountAmount,
                finalAmount = order.FinalAmount
            });
        }

        // ✅ Cancel Order (with reason)
        [Authorize]
        [HttpPost("cancel")]
        public async Task<IActionResult> CancelOrder(CancelOrderDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.UserId == userId);

            if (order == null)
                return NotFound("Order not found or you do not have permission to cancel it.");

            if (order.Status != "pending")
                return BadRequest("Only pending orders can be canceled.");

            order.Status = "cancelled";
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Order #{order.Id} has been canceled.",
                reason = dto.Reason,
                status = order.Status
            });
        }

        // ✅ Staff Claim Processing
        [Authorize(Roles = "staff")]
        [HttpPost("process-claim")]
        
        public async Task<IActionResult> ProcessClaim([FromQuery] string claimCode)

        {
            if (string.IsNullOrWhiteSpace(claimCode))
                return BadRequest("Claim code is required.");

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.ClaimCode == claimCode && o.Status == "pending");

            if (order == null)
                return NotFound("Invalid or already processed claim code.");

            order.Status = "ready_for_pickup";
            order.ProcessedBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            order.PickupDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Order #{order.Id} processed for pickup.",
                processedBy = order.ProcessedBy,
                pickupDate = order.PickupDate
            });
        }

        [Authorize(Roles = "admin,staff")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                DiscountAmount = o.DiscountAmount,
                FinalAmount = o.FinalAmount,
                ClaimCode = o.ClaimCode,
                Status = o.Status,
                PickupDate = o.PickupDate,
                ProcessedBy = o.ProcessedBy,
                OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    BookId = oi.BookId,
                    BookTitle = oi.Book?.Title,
                    AuthorName = oi.Book?.AuthorName,
                    PriceAtOrder = oi.PriceAtOrder,
                    Quantity = oi.Quantity,
                    Subtotal = oi.Subtotal
                }).ToList()
            });
            return Ok(orderDtos);
        }


        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var orderQuery = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                .AsQueryable();

            Order? order;

            if (userRole == "admin" || userRole == "staff")
            {
                // Admins and staff can access any order
                order = await orderQuery.FirstOrDefaultAsync(o => o.Id == id);
            }
            else
            {
                // Members can only access their own orders
                order = await orderQuery.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            }

            if (order == null)
                return NotFound(new {success = false, message = "No orders found."});

            var orderDto = new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                DiscountAmount = order.DiscountAmount,
                FinalAmount = order.FinalAmount,
                ClaimCode = order.ClaimCode,
                Status = order.Status,
                PickupDate = order.PickupDate,
                ProcessedBy = order.ProcessedBy,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    BookId = oi.BookId,
                    BookTitle = oi.Book?.Title,
                    AuthorName = oi.Book?.AuthorName,
                    PriceAtOrder = oi.PriceAtOrder,
                    Quantity = oi.Quantity,
                    Subtotal = oi.Subtotal
                }).ToList()
            };

            return Ok(orderDto);
        }



        // ✅ Calculate Discount (5% for 5+ books, +10% after 10 completed orders)
        private async Task<decimal> CalculateDiscountAsync(int userId, int itemCount, decimal total)
        {
            decimal discount = 0;

            if (itemCount >= 5)
                discount += 0.05m;

            if (itemCount >= 10)
                discount += 0.10m;

            var successfulOrders = await _context.Orders
                .Where(o => o.UserId == userId && o.Status == "completed")
                .CountAsync();

            return total * discount;
        }


        [HttpPost("send-test")]
        public async Task<IActionResult> SendTestEmail(SendTestEmailDto dto)
        {
            try
            {
                var message = new MailMessage();
                message.From = new MailAddress("lordofvoid0001@gmail.com");
                message.To.Add(dto.ToEmail);
                message.Subject = "📦 BookHaven Order Confirmation - Test Email";
                message.IsBodyHtml = true;

                // Example book table rows
                string booksTable = @"
            <tr>
                <td>Atomic Habits</td>
                <td>James Clear</td>
                <td>Rs 500</td>
                <td>1</td>
                <td>Rs 500</td>
            </tr>
            <tr>
                <td>Clean Code</td>
                <td>Robert C. Martin</td>
                <td>Rs 2000</td>
                <td>1</td>
                <td>Rs 2000</td>
            </tr>";

                string htmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto;'>
                <h2 style='color: #4A90E2;'>📚 BookHaven - Order Confirmation</h2>
                <p>Thank you for your order!</p>

                <h3>🧾 Order Summary</h3>
                <p><strong>Order ID:</strong> #TEST1234</p>
                <p><strong>Claim Code:</strong> ABCD1234</p>
                <p><strong>Order Date:</strong> {DateTime.Now:dd MMM yyyy}</p>

                <table style='width: 100%; border-collapse: collapse; margin-top: 10px;'>
                    <thead>
                        <tr style='background-color: #f2f2f2;'>
                            <th style='padding: 8px; border: 1px solid #ddd;'>Title</th>
                            <th style='padding: 8px; border: 1px solid #ddd;'>Author</th>
                            <th style='padding: 8px; border: 1px solid #ddd;'>Price</th>
                            <th style='padding: 8px; border: 1px solid #ddd;'>Qty</th>
                            <th style='padding: 8px; border: 1px solid #ddd;'>Subtotal</th>
                        </tr>
                    </thead>
                    <tbody>
                        {booksTable}
                    </tbody>
                </table>

                <h3>💰 Billing</h3>
                <p><strong>Total:</strong> Rs 2500</p>
                <p><strong>Discount:</strong> Rs 250</p>
                <p><strong>Final Amount:</strong> <strong>Rs 2250</strong></p>

                <hr />
                <p>📌 Please bring your Membership ID and show your Claim Code at the store to collect your books.</p>
                <p>Thank you for shopping with <strong>BookHaven</strong>!</p>
            </div>";

                message.Body = htmlBody;

                using var smtp = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("lordofvoid0001@gmail.com", "xord qipa bpcf beut"),
                    EnableSsl = true,
                };

                await smtp.SendMailAsync(message);
                return Ok(new { success = true, message = $"✅ Test email sent to {dto.ToEmail}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }



        private async Task SendOrderEmailAsync(Order order, string toEmail)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            string host = smtpSettings["Host"];
            int port = int.Parse(smtpSettings["Port"]);
            string username = smtpSettings["Username"];
            string password = smtpSettings["Password"];
            bool enableSsl = bool.Parse(smtpSettings["EnableSsl"]);

            var message = new MailMessage
            {
                From = new MailAddress(username),
                Subject = "📦 BookHaven Order Confirmation - Order #" + order.Id,
                IsBodyHtml = true
            };

            string booksTableRows = string.Join("", order.OrderItems.Select(i =>
                $"<tr><td>{i.Book.Title}</td><td>{i.Book.AuthorName}</td><td>Rs {i.PriceAtOrder}</td><td>{i.Quantity}</td><td>Rs {i.Subtotal}</td></tr>"
            ));

            string htmlBody = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto;'>
            <h2 style='color: #4A90E2;'>📚 BookHaven - Order Confirmation</h2>
            <p>Thank you for your order!</p>

            <h3>🧾 Order Summary</h3>
            <p><strong>Order ID:</strong> #{order.Id}</p>
            <p><strong>Claim Code:</strong> {order.ClaimCode}</p>
            <p><strong>Order Date:</strong> {order.OrderDate:dd MMM yyyy}</p>

            <table style='width: 100%; border-collapse: collapse; margin-top: 10px;'>
                <thead>
                    <tr style='background-color: #f2f2f2;'>
                        <th style='padding: 8px; border: 1px solid #ddd;'>Title</th>
                        <th style='padding: 8px; border: 1px solid #ddd;'>Author</th>
                        <th style='padding: 8px; border: 1px solid #ddd;'>Price</th>
                        <th style='padding: 8px; border: 1px solid #ddd;'>Qty</th>
                        <th style='padding: 8px; border: 1px solid #ddd;'>Subtotal</th>
                    </tr>
                </thead>
                <tbody>
                    {booksTableRows}
                </tbody>
            </table>

            <h3>💰 Billing</h3>
            <p><strong>Total:</strong> Rs {order.TotalAmount}</p>
            <p><strong>Discount:</strong> Rs {order.DiscountAmount}</p>
            <p><strong>Final Amount:</strong> <strong>Rs {order.FinalAmount}</strong></p>

            <hr />
            <p>📌 Please bring your Membership ID and show your Claim Code at the store to collect your books.</p>
            <p>Thank you for shopping with <strong>BookHaven</strong>!</p>
        </div>
    ";

            message.Body = htmlBody;
            message.To.Add(toEmail);

            using var smtp = new SmtpClient(host)
            {
                Port = port,
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            await smtp.SendMailAsync(message);
        }

        [Authorize(Roles = "staff")]
        [HttpGet("history")]
        public async Task<IActionResult> GetOrderHistory()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var orders = await _context.Orders
                .Where(o => o.UserId == userId && o.Status == "completed")
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var orderHistory = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                DiscountAmount = o.DiscountAmount,
                FinalAmount = o.FinalAmount,
                ClaimCode = o.ClaimCode,
                Status = o.Status,
                PickupDate = o.PickupDate,
                ProcessedBy = o.ProcessedBy,
                OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    BookId = oi.BookId,
                    BookTitle = oi.Book?.Title,
                    AuthorName = oi.Book?.AuthorName,
                    PriceAtOrder = oi.PriceAtOrder,
                    Quantity = oi.Quantity,
                    Subtotal = oi.Subtotal
                }).ToList()
            });

            return Ok(new
            {
                success = true,
                message = "Completed order history retrieved successfully.",
                data = orderHistory
            });
        }



        [Authorize(Roles = "staff")]
        [HttpPost("complete-order")]
        public async Task<IActionResult> CompleteOrder([FromQuery] string claimCode)
        {
            if (string.IsNullOrWhiteSpace(claimCode))
                return BadRequest("Claim code is required.");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(o => o.ClaimCode == claimCode && o.Status == "ready_for_pickup");

            if (order == null)
                return BadRequest("Order is not ready for completion or claim code is invalid.");

            order.Status = "completed";
            order.UpdatedAt = DateTime.UtcNow;

            var cart = await _context.ShoppingCarts
                .FirstOrDefaultAsync(c => c.UserId == order.UserId && !c.IsPaymentDone);

            if (cart != null)
            {
                cart.IsPaymentDone = true;
                cart.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(order.UserId);
            if (user != null)
            {
                await SendCompletionEmailAsync(order, user.Email);
            }

            return Ok(new { success = true, message = $"Order #{order.Id} has been marked as completed." });
        }


        private async Task SendCompletionEmailAsync(Order order, string toEmail)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            string host = smtpSettings["Host"];
            int port = int.Parse(smtpSettings["Port"]);
            string username = smtpSettings["Username"];
            string password = smtpSettings["Password"];
            bool enableSsl = bool.Parse(smtpSettings["EnableSsl"]);

            var message = new MailMessage
            {
                From = new MailAddress(username),
                Subject = "🎉 Your BookHaven Order is Ready for Pickup!",
                IsBodyHtml = true
            };

            // Build a table for the books and order details
            string booksTable = string.Join("", order.OrderItems.Select(i => $@"
        <tr>
            <td>{i.Book.Title}</td>
            <td>{i.Book.AuthorName}</td>
            <td>Rs {i.PriceAtOrder}</td>
        </tr>"));

            string htmlBody = $@"
    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto;'>
        <h2 style='color: #4A90E2;'>🎉 Your BookHaven Order is Ready!</h2>
        <p>Dear Customer,</p>
        <p>We're excited to let you know that your order #{order.Id} has been marked as <strong>completed</strong> and is now ready for pickup.</p>

        <h3>Order Details</h3>
        <p><strong>Claim Code:</strong> {order.ClaimCode}</p>
        <p><strong>Final Amount Paid:</strong> Rs {order.FinalAmount}</p>

        <h3>📚 Books in Your Order</h3>
        <table style='width: 100%; border-collapse: collapse;'>
            <thead>
                <tr style='background-color: #f2f2f2;'>
                    <th style='padding: 8px; border: 1px solid #ddd;'>Book Title</th>
                    <th style='padding: 8px; border: 1px solid #ddd;'>Author</th>
                    <th style='padding: 8px; border: 1px solid #ddd;'>Price</th>
                </tr>
            </thead>
            <tbody>
                {booksTable}
            </tbody>
        </table>

        <hr style='margin-top: 20px; border: 1px solid #ddd;' />
        <p>📌 Please show your <strong>Claim Code</strong> and <strong>Membership ID</strong> at the store to collect your books.</p>
        <p>Thank you for choosing <strong>BookHaven</strong>! We look forward to serving you again soon.</p>

        <footer style='font-size: 12px; text-align: center; margin-top: 20px; color: #aaa;'>
            <p>BookHaven Team | All rights reserved</p>
            <p><em>If you have any questions, feel free to contact us at support@bookhaven.com</em></p>
        </footer>
    </div>";

            message.Body = htmlBody;
            message.To.Add(toEmail);

            using var smtp = new SmtpClient(host)
            {
                Port = port,
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            await smtp.SendMailAsync(message);
        }


    }

}