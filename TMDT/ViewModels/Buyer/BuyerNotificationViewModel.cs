using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Services;
using TMDT.Utilities;

namespace TMDT.ViewModels.Buyer
{
    public class BuyerNotificationViewModel : ViewModelBase
    {
        private readonly BuyerMainViewModel _mainVm;
        private ObservableCollection<Notification> _notifications;
        private bool _isLoading;

        public ObservableCollection<Notification> Notifications
        {
            get => _notifications;
            set { SetProperty(ref _notifications, value); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { SetProperty(ref _isLoading, value); }
        }

        public ICommand MarkAsReadCommand { get; }
        public ICommand MarkAllAsReadCommand { get; }

        public BuyerNotificationViewModel(BuyerMainViewModel mainVm)
        {
            _mainVm = mainVm;
            _notifications = new ObservableCollection<Notification>();

            MarkAsReadCommand = new RelayCommand(ExecuteMarkAsRead);
            MarkAllAsReadCommand = new RelayCommand(_ => ExecuteMarkAllAsRead());

            _ = LoadNotificationsAsync();
        }

        private async Task LoadNotificationsAsync()
        {
            if (!SessionManager.IsLoggedIn || SessionManager.CurrentUser == null) return;
            
            IsLoading = true;
            try
            {
                var notifs = await NotificationService.Instance.GetNotificationsAsync(SessionManager.CurrentUser.UserId);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Notifications.Clear();
                    foreach (var n in notifs)
                    {
                        Notifications.Add(n);
                    }
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExecuteMarkAsRead(object obj)
        {
            if (obj is Notification notif && notif.IsRead == false)
            {
                await NotificationService.Instance.MarkAsReadAsync(notif.NotificationId);
                // Update local model
                notif.IsRead = true;
                
                // Force UI update
                var index = Notifications.IndexOf(notif);
                if (index >= 0)
                {
                    Notifications[index] = new Notification 
                    {
                        NotificationId = notif.NotificationId,
                        Title = notif.Title,
                        Content = notif.Content,
                        CreatedAt = notif.CreatedAt,
                        IsRead = true
                    };
                }
            }
        }

        private async void ExecuteMarkAllAsRead()
        {
            if (!SessionManager.IsLoggedIn || SessionManager.CurrentUser == null) return;

            await NotificationService.Instance.MarkAllAsReadAsync(SessionManager.CurrentUser.UserId);
            
            // Reload all
            await LoadNotificationsAsync();
        }
    }
}
