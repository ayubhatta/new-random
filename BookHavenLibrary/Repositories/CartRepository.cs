using BookHavenLibrary.Data;
using BookHavenLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace BookHavenLibrary.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _context;
        private readonly IPurchaseRepository _purchaseRepo;

        public CartRepository(AppDbContext context, IPurchaseRepository purchaseRepo)
        {
            _context = context;
            _purchaseRepo = purchaseRepo;
        }

        public async Task<ShoppingCart> GetCartByUserIdAsync(int userId)
        {
            return await _context.ShoppingCarts
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsPaymentDone);
        }

        public async Task<IEnumerable<CartItem>> GetCartItemsAsync(int cartId)
        {
            return await _context.CartItems
                .Where(ci => ci.ShoppingCartId == cartId)
                .ToListAsync();
        }

        public async Task<CartItem> GetCartItemByIdAsync(int id)
        {
            return await _context.CartItems.FindAsync(id);
        }

        public async Task<bool> AddToCartAsync(int userId, int bookId, int quantity)
        {
            var cart = await GetCartByUserIdAsync(userId);
            if (cart == null)
            {
                cart = new ShoppingCart
                {
                    UserId = userId,
                    IsPaymentDone = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.ShoppingCarts.Add(cart);
                await _context.SaveChangesAsync(); // Save to get generated cart.Id
            }

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.ShoppingCartId == cart.Id && ci.BookId == bookId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    ShoppingCartId = cart.Id,
                    BookId = bookId,
                    Quantity = quantity,
                    AddedAt = DateTime.UtcNow
                };
                _context.CartItems.Add(cartItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task UpdateCartItemAsync(CartItem item)
        {
            _context.CartItems.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveCartItemAsync(int itemId)
        {
            var item = await _context.CartItems.FindAsync(itemId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task CancelCartAsync(int userId)
        {
            var cart = await GetCartByUserIdAsync(userId);
            if (cart != null)
            {
                var items = _context.CartItems
                    .Where(ci => ci.ShoppingCartId == cart.Id);

                _context.CartItems.RemoveRange(items);
                _context.ShoppingCarts.Remove(cart);

                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<ShoppingCart>> GetPaidCartsByUserAsync(int userId)
        {
            return await _context.ShoppingCarts
                .Where(c => c.UserId == userId && c.IsPaymentDone)
                .Include(c => c.CartItems)
                .ToListAsync();
        }
    }
}
