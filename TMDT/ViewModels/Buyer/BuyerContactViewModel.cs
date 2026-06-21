using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Buyer
{
    public class BuyerContactViewModel : ViewModelBase
    {
        private readonly BuyerMainViewModel _mainVm;

        // Contact Info (loaded from SystemConfig)
        private string _hotline = "1900 1234";
        private string _email = "support@volox.vn";
        private string _address = "123 Đường Lê Lợi, Quận 1, TP. Hồ Chí Minh";
        private string _workingHours = "Thứ 2 - Thứ 7: 8:00 - 20:00";

        public string Hotline { get => _hotline; set => SetProperty(ref _hotline, value); }
        public string Email { get => _email; set => SetProperty(ref _email, value); }
        public string Address { get => _address; set => SetProperty(ref _address, value); }
        public string WorkingHours { get => _workingHours; set => SetProperty(ref _workingHours, value); }

        // Form
        private string _senderName = string.Empty;
        private string _senderEmail = string.Empty;
        private string _selectedSubject = "Góp ý";
        private string _feedbackContent = string.Empty;
        private bool _isSending;
        private bool _isSuccess;

        public string SenderName { get => _senderName; set => SetProperty(ref _senderName, value); }
        public string SenderEmail { get => _senderEmail; set => SetProperty(ref _senderEmail, value); }
        public string SelectedSubject { get => _selectedSubject; set => SetProperty(ref _selectedSubject, value); }
        public string FeedbackContent { get => _feedbackContent; set { SetProperty(ref _feedbackContent, value); CommandManager.InvalidateRequerySuggested(); } }
        public bool IsSending { get => _isSending; set => SetProperty(ref _isSending, value); }
        public bool IsSuccess { get => _isSuccess; set => SetProperty(ref _isSuccess, value); }

        public ObservableCollection<string> SubjectList { get; } = new()
        {
            "Góp ý chung",
            "Báo lỗi kỹ thuật",
            "Hỏi về đơn hàng",
            "Hỏi về chính sách",
            "Khác"
        };

        public ICommand SendFeedbackCommand { get; }

        public BuyerContactViewModel(BuyerMainViewModel mainVm)
        {
            _mainVm = mainVm;

            // Pre-fill info if logged in
            if (SessionManager.IsLoggedIn && SessionManager.CurrentUser != null)
            {
                SenderName = SessionManager.CurrentUser.FullName ?? string.Empty;
                SenderEmail = SessionManager.CurrentUser.Email ?? string.Empty;
            }

            SendFeedbackCommand = new RelayCommand(
                async _ => await SendFeedbackAsync(),
                _ => !string.IsNullOrWhiteSpace(FeedbackContent) && !IsSending
            );

            _ = LoadContactInfoAsync();
        }

        private async Task LoadContactInfoAsync()
        {
            try
            {
                using var context = new TmdtContext();
                var configs = await context.SystemConfigs.AsNoTracking().ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var cfg in configs)
                    {
                        if (string.IsNullOrWhiteSpace(cfg.ConfigValue)) continue;
                        switch (cfg.ConfigKey)
                        {
                            case "ContactHotline":     Hotline      = cfg.ConfigValue; break;
                            case "ContactEmail":       Email        = cfg.ConfigValue; break;
                            case "ContactAddress":     Address      = cfg.ConfigValue; break;
                            case "ContactWorkingHours": WorkingHours = cfg.ConfigValue; break;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load contact info failed: " + ex.Message);
            }
        }

        private async Task SendFeedbackAsync()
        {
            if (string.IsNullOrWhiteSpace(FeedbackContent) || IsSending) return;

            IsSending = true;
            IsSuccess = false;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                using var context = new TmdtContext();

                string fullContent = $"[{SelectedSubject}]";
                if (!string.IsNullOrWhiteSpace(SenderName))
                    fullContent += $" Từ: {SenderName}";
                if (!string.IsNullOrWhiteSpace(SenderEmail))
                    fullContent += $" <{SenderEmail}>";
                fullContent += $"\n\n{FeedbackContent}";

                var complaint = new Complaint
                {
                    BuyerId = SessionManager.CurrentUser?.UserId,
                    Content = fullContent,
                    Status = "Open",
                    SubmittedAt = DateTime.Now
                };

                context.Complaints.Add(complaint);
                await context.SaveChangesAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsSuccess = true;
                    FeedbackContent = string.Empty;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Send feedback failed: " + ex.Message);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Gửi phản hồi thất bại. Vui lòng thử lại.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                IsSending = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
