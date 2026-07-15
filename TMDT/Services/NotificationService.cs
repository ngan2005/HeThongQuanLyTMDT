using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.Services
{
    public class NotificationService
    {
        private static NotificationService? _instance;
        public static NotificationService Instance => _instance ??= new NotificationService();

        public event Action? NotificationChanged;

        private NotificationService() { }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            try
            {
                using var context = new TmdtContext();
                return await context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead == false)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting unread notification count: {ex.Message}");
                return 0;
            }
        }

        public async Task<List<Notification>> GetNotificationsAsync(int userId)
        {
            try
            {
                using var context = new TmdtContext();
                return await context.Notifications
                    .AsNoTracking()
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting notifications: {ex.Message}");
                return new List<Notification>();
            }
        }

        public async Task CreateNotificationAsync(int userId, string title, string content, string type, int? relatedId = null)
        {
            try
            {
                using var context = new TmdtContext();
                var notif = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Content = content,
                    NotificationType = type,
                    RelatedId = relatedId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                context.Notifications.Add(notif);
                await context.SaveChangesAsync();
                NotifyChanged();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating notification: {ex.Message}");
            }
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            try
            {
                using var context = new TmdtContext();
                var notif = await context.Notifications.FindAsync(notificationId);
                if (notif != null && notif.IsRead != true)
                {
                    notif.IsRead = true;
                    await context.SaveChangesAsync();
                    NotifyChanged();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking notification as read: {ex.Message}");
            }
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            try
            {
                using var context = new TmdtContext();
                var unreadNotifs = await context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead == false)
                    .ToListAsync();

                if (unreadNotifs.Any())
                {
                    foreach (var n in unreadNotifs)
                    {
                        n.IsRead = true;
                    }
                    await context.SaveChangesAsync();
                    NotifyChanged();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking all notifications as read: {ex.Message}");
            }
        }

        private void NotifyChanged()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                NotificationChanged?.Invoke();
            });
        }
    }
}
