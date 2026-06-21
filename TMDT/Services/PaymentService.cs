using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Services.Interfaces;

namespace TMDT.Services;

public class PaymentService : IPaymentService
{
    private static PaymentService? _instance;
    public static PaymentService Instance => _instance ??= new PaymentService();

    private PaymentService() { }

    public async Task<Payment> CreatePaymentAsync(int orderId, string method, decimal amount)
    {
        using var ctx = new TmdtContext();

        var payment = new Payment
        {
            OrderId = orderId,
            Method = method,
            Amount = amount,
            Status = "Pending",
            TransactionCode = null,
            PaidAt = null
        };

        ctx.Payments.Add(payment);
        await ctx.SaveChangesAsync();

        return payment;
    }

    public async Task<bool> ProcessPaymentAsync(int paymentId)
    {
        using var ctx = new TmdtContext();
        var payment = await ctx.Payments.FindAsync(paymentId);

        if (payment == null) return false;

        switch (payment.Method)
        {
            case "COD":
                // COD: đơn hàng Pending, thanh toán khi nhận hàng
                payment.Status = "Pending";
                break;

            case "VNPay":
                // Mock VNPay: tạo mã giao dịch giả lập
                payment.TransactionCode = GenerateTransactionCode("VNPAY");
                payment.Status = "Paid";
                payment.PaidAt = DateTime.Now;
                break;

            case "MoMo":
                // Mock MoMo: tạo mã giao dịch giả lập
                payment.TransactionCode = GenerateTransactionCode("MOMO");
                payment.Status = "Paid";
                payment.PaidAt = DateTime.Now;
                break;

            case "Wallet":
                // Ví: trừ tiền trong ví buyer
                var order = await ctx.Orders.FindAsync(payment.OrderId);
                if (order == null || order.BuyerId == null) return false;

                var buyer = await ctx.Users.FindAsync(order.BuyerId.Value);
                if (buyer == null) return false;

                if ((buyer.WalletBalance ?? 0) < (payment.Amount ?? 0))
                    throw new InvalidOperationException("Số dư ví không đủ.");

                buyer.WalletBalance = (buyer.WalletBalance ?? 0) - (payment.Amount ?? 0);
                payment.TransactionCode = GenerateTransactionCode("WALLET");
                payment.Status = "Paid";
                payment.PaidAt = DateTime.Now;
                break;

            default:
                payment.Status = "Pending";
                break;
        }

        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ConfirmPaymentAsync(int paymentId, string transactionCode)
    {
        using var ctx = new TmdtContext();
        var payment = await ctx.Payments.FindAsync(paymentId);

        if (payment == null) return false;

        payment.TransactionCode = transactionCode;
        payment.Status = "Paid";
        payment.PaidAt = DateTime.Now;

        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<bool> FailPaymentAsync(int paymentId, string reason)
    {
        using var ctx = new TmdtContext();
        var payment = await ctx.Payments.FindAsync(paymentId);

        if (payment == null) return false;

        payment.Status = "Failed";

        var order = await ctx.Orders.FindAsync(payment.OrderId);
        if (order != null)
        {
            order.OrderStatus = "Cancelled";
            ctx.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                NewStatus = "Cancelled",
                Note = $"Thanh toán thất bại: {reason}",
                ChangedAt = DateTime.Now
            });
        }

        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RefundPaymentAsync(int paymentId)
    {
        using var ctx = new TmdtContext();
        var payment = await ctx.Payments.FindAsync(paymentId);

        if (payment == null) return false;
        if (payment.Status != "Paid") return false;

        var order = await ctx.Orders.FindAsync(payment.OrderId);
        if (order == null || order.BuyerId == null) return false;

        var buyer = await ctx.Users.FindAsync(order.BuyerId.Value);
        if (buyer == null) return false;

        buyer.WalletBalance = (buyer.WalletBalance ?? 0) + (payment.Amount ?? 0);

        payment.Status = "Refunded";

        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<Payment?> GetPaymentByOrderIdAsync(int orderId)
    {
        using var ctx = new TmdtContext();
        return await ctx.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    public async Task<List<Payment>> GetPaymentsByUserAsync(int userId)
    {
        using var ctx = new TmdtContext();
        return await ctx.Payments
            .AsNoTracking()
            .Include(p => p.Order)
            .Where(p => p.Order != null && p.Order.BuyerId == userId)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync();
    }

    private static string GenerateTransactionCode(string prefix)
    {
        return $"{prefix}-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}
