using BookHavenLibrary.Models;

namespace BookHavenLibrary.Repositories
{
    public interface ICartRepository
    {
        Task<ShoppingCart> GetCartByUserIdAsync(int userId);
        Task<IEnumerable<CartItem>> GetCartItemsAsync(int cartId);
        Task<CartItem> GetCartItemByIdAsync(int id);
        Task UpdateCartItemAsync(CartItem item);
        Task RemoveCartItemAsync(int itemId);
        Task CancelCartAsync(int userId);
        Task<bool> MakePaymentAsync(int userId);
        Task<List<ShoppingCart>> GetPaidCartsByUserAsync(int userId);
        Task<bool> AddToCartAsync(int userId, int bookId, int quantity);
    }
}
