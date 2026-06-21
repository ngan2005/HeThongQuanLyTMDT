using TMDT.Models;
using TMDT.DTOs;

namespace TMDT.Services.Interfaces;

public interface IOrderService
{
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task<List<Order>> GetBuyerOrdersAsync(int buyerId, string? statusFilter = null);
    Task<List<Order>> GetShopOrdersAsync(int shopId, string? statusFilter = null);
    Task<List<Order>> GetAllOrdersAsync(string? statusFilter = null, string? keyword = null);
    Task<Order?> CreateOrderFromCartAsync(int buyerId, int shopId, int? addressId, int? voucherId, string paymentMethod, decimal shippingFee, List<CartOrderItem> items);
    Task<bool> CancelOrderAsync(int orderId);
    Task<bool> ShipOrderAsync(int orderId, string shippingProvider);
    Task<bool> ReceiveOrderAsync(int orderId);
    Task<bool> RefundOrderAsync(int orderId);
    Task<bool> UpdatePaymentSuccessAsync(int orderId, string transactionCode);
    Task<OrderStats> GetBuyerStatsAsync(int buyerId);
    Task<OrderStats> GetShopStatsAsync(int shopId);
    Task<OrderStats> GetAdminStatsAsync();
}

public class OrderStats
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ShippingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalSpending { get; set; }
}
