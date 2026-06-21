using TMDT.Models;

namespace TMDT.Services.Interfaces;

public interface IPaymentService
{
    Task<Payment> CreatePaymentAsync(int orderId, string method, decimal amount);
    Task<bool> ProcessPaymentAsync(int paymentId);
    Task<bool> ConfirmPaymentAsync(int paymentId, string transactionCode);
    Task<bool> FailPaymentAsync(int paymentId, string reason);
    Task<bool> RefundPaymentAsync(int paymentId);
    Task<Payment?> GetPaymentByOrderIdAsync(int orderId);
    Task<List<Payment>> GetPaymentsByUserAsync(int userId);
}
