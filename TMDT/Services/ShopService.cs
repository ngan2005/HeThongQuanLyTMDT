using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Services.Interfaces;

namespace TMDT.Services
{
    public class ShopService : IShopService
    {
        private readonly TmdtContext _context;

        public ShopService(TmdtContext context)
        {
            _context = context;
        }

        public async Task<bool> HasShopForUserAsync(int userId)
        {
            return await _context.Shops.AnyAsync(s => s.UserId == userId);
        }

        public async Task<ShopDto> RegisterShopAsync(ShopRegisterRequest request)
        {
            // Kiểm tra user đã có shop chưa
            if (await _context.Shops.AnyAsync(s => s.UserId == request.UserId))
                return null;

            // Kiểm tra tên shop đã tồn tại chưa
            if (await _context.Shops.AnyAsync(s => s.ShopName == request.ShopName))
                return null;

            var shop = new Shop
            {
                UserId = request.UserId,
                ShopName = request.ShopName.Trim(),
                WarehouseAddress = request.WarehouseAddress?.Trim(),
                CommissionRate = 3.0m,
                WalletBalance = 0,
                Rating = 0,
                IsActive = null, // Chờ duyệt
                OpenedAt = null,
                VacationMode = false
            };

            _context.Shops.Add(shop);
            await _context.SaveChangesAsync();

            return new ShopDto
            {
                ShopId = shop.ShopId,
                ShopName = shop.ShopName,
                IsActive = shop.IsActive
            };
        }
    }
}
