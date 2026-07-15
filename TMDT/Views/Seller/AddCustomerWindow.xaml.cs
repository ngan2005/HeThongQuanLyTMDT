using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Helpers;

namespace TMDT.Views.Seller
{
    public partial class AddCustomerWindow : Window
    {
        public string RegisteredPhone { get; private set; } = string.Empty;

        /// <summary>Có truyền sẵn SĐT (prefill) khi mở từ POS hay không.</summary>
        public bool HasPrefilledPhone => !string.IsNullOrWhiteSpace(InitialPhone);

        private readonly string InitialPhone;

        public AddCustomerWindow() : this(string.Empty) { }

        /// <summary>
        /// Mở form tạo KH mới với tuỳ chọn prefill SĐT (dùng khi gọi từ POS khi SĐT không tồn tại).
        /// </summary>
        public AddCustomerWindow(string prefilledPhone)
        {
            InitializeComponent();
            InitialPhone = prefilledPhone?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(InitialPhone))
            {
                txtPhone.Text = InitialPhone;
                txtPhone.IsEnabled = false; // Khoá ô SĐT để tránh nhập SĐT khác
                txtPhone.ToolTip = "SĐT tự động từ POS";
                txtFullName.Focus();
                txtFullName.SelectAll();
            }
            else
            {
                txtPhone.Focus();
            }
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Nếu đã prefill, lấy từ InitialPhone; nếu không, lấy từ ô nhập
            string phone = !string.IsNullOrWhiteSpace(InitialPhone) ? InitialPhone : txtPhone.Text.Trim();
            string fullName = txtFullName.Text.Trim();

            // Validate dữ liệu
            if (string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("Vui lòng nhập số điện thoại khách hàng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPhone.Focus();
                return;
            }

            if (phone.Length < 9 || phone.Length > 11)
            {
                MessageBox.Show("Số điện thoại không hợp lệ (độ dài từ 9 đến 11 số).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPhone.Focus();
                return;
            }

            if (string.IsNullOrEmpty(fullName))
            {
                MessageBox.Show("Vui lòng nhập họ và tên khách hàng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtFullName.Focus();
                return;
            }

            btnSave.IsEnabled = false;

            try
            {
                using var context = new TmdtContext();

                // Kiểm tra trùng số điện thoại (race-condition safe)
                var existingUser = await context.Users.AnyAsync(u => u.Phone == phone);
                if (existingUser)
                {
                    MessageBox.Show("Số điện thoại này đã được đăng ký thành viên trước đó.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    btnSave.IsEnabled = true;
                    txtPhone.Focus();
                    return;
                }

                // Tạo tài khoản khách hàng mới
                var newUser = new User
                {
                    FullName = fullName,
                    Phone = phone,
                    Email = $"kh-{phone}@pos.local",
                    Password = PasswordHelper.HashPassword("123456"),
                    RoleId = 2, // Buyer
                    LoyaltyPoints = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                context.Users.Add(newUser);
                await context.SaveChangesAsync();

                RegisteredPhone = phone;
                DialogResult = true;
                // Không show MessageBox khi prefill (đã hiển thị hint ở POS rồi) — tránh thừa tương tác
                if (string.IsNullOrWhiteSpace(InitialPhone))
                {
                    MessageBox.Show("Đăng ký thành viên mới thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu thông tin khách hàng: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                btnSave.IsEnabled = true;
            }
        }

        private void txtFullName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSave_Click(sender, new RoutedEventArgs());
            }
        }
    }
}
