using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;

namespace TMDT.Services
{
    public class ViewHistoryService
    {
        private static ViewHistoryService? _instance;
        public static ViewHistoryService Instance => _instance ??= new ViewHistoryService();

        private ViewHistoryService() { }

        public async Task LogProductViewAsync(int userId, int productId)
        {
            try
            {
                using var context = new TmdtContext();

                var history = await context.ViewHistories
                    .FirstOrDefaultAsync(v => v.UserId == userId && v.ProductId == productId);

                if (history != null)
                {
                    history.ViewedAt = DateTime.Now;
                }
                else
                {
                    history = new ViewHistory
                    {
                        UserId = userId,
                        ProductId = productId,
                        ViewedAt = DateTime.Now
                    };
                    context.ViewHistories.Add(history);
                }

                await context.SaveChangesAsync();
                
                // Cleanup old records to keep max 50 items per user
                var oldRecords = await context.ViewHistories
                    .Where(v => v.UserId == userId)
                    .OrderByDescending(v => v.ViewedAt)
                    .Skip(50)
                    .ToListAsync();
                    
                if (oldRecords.Any())
                {
                    context.ViewHistories.RemoveRange(oldRecords);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging product view: {ex.Message}");
            }
        }

        public async Task<List<Product>> GetRecentViewsAsync(int userId, int limit = 20)
        {
            try
            {
                using var context = new TmdtContext();
                return await context.ViewHistories
                    .AsNoTracking()
                    .Where(v => v.UserId == userId)
                    .OrderByDescending(v => v.ViewedAt)
                    .Take(limit)
                    .Select(v => v.Product)
                    .Include(p => p.Shop)
                    .Include(p => p.Category)
                    .Where(p => p != null && p.Status == "Active")
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting recent views: {ex.Message}");
                return new List<Product>();
            }
        }
    }
}
