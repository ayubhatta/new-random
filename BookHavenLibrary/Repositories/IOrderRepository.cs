using BookHavenLibrary.Models;

public interface IOrderRepository
{
    Task<Order?> GetOrderByClaimCodeAsync(string claimCode);
    Task<List<Order>> GetAllOrdersAsync();
    Task<Order?> GetOrderByIdAsync(int id);
    Task<Order?> GetUserOrderAsync(int orderId, int userId);
    Task<bool> CancelOrderAsync(Order order);
    Task SaveChangesAsync();
}