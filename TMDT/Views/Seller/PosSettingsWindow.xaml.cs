using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TMDT.Services;

namespace TMDT.Views.Seller
{
    public partial class PosSettingsWindow : Window
    {
        private string? _qrImagePath;        // MoMo QR path
        private string? _vnpayQrImagePath;   // VNPay QR path

        public PosSettingsWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            var s = PosSettingsHelper.Current;
            txtMoMoPhone.Text = s.MoMoPhone ?? "";
            
            _qrImagePath = s.MoMoQrImagePath;
            UpdateQrPreview();

            // Bank
            if (!string.IsNullOrEmpty(s.VnpayBankName))
            {
                foreach (ComboBoxItem item in cboBank.Items)
                {
                    if ((item.Content as string) == s.VnpayBankName)
                    {
                        cboBank.SelectedItem = item;
                        break;
                    }
                }
            }
            txtVnpayAccount.Text = s.VnpayBankAccount ?? "";

            // Load VNPay QR
            _vnpayQrImagePath = s.VnpayQrImagePath;
            UpdateVnpayQrPreview();

            chkAutoReprint.IsChecked = s.AutoReprintReceipt;
            chkPrintAfterSync.IsChecked = s.PrintAfterSync;
        }

        private void UpdateQrPreview()
        {
            if (!string.IsNullOrEmpty(_qrImagePath) && File.Exists(_qrImagePath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(_qrImagePath, UriKind.Absolute);
                    bitmap.EndInit();
                    
                    imgQrPreview.Source = bitmap;
                    iconNoQr.Visibility = Visibility.Collapsed;
                    btnRemoveQr.Visibility = Visibility.Visible;
                    txtMoMoHint.Text = "Đang sử dụng ảnh QR tĩnh.";
                }
                catch
                {
                    ClearQrPreview();
                }
            }
            else
            {
                ClearQrPreview();
            }
        }

        private void ClearQrPreview()
        {
            _qrImagePath = null;
            imgQrPreview.Source = null;
            iconNoQr.Visibility = Visibility.Visible;
            btnRemoveQr.Visibility = Visibility.Collapsed;
            
            var s = PosSettingsHelper.Current;
            txtMoMoHint.Text = string.IsNullOrEmpty(txtMoMoPhone.Text)
                ? "Chưa cấu hình — QR sẽ fallback về trang chủ MoMo."
                : $"SĐT hiện tại: {txtMoMoPhone.Text}";
        }

        private void UpdateVnpayQrPreview()
        {
            if (!string.IsNullOrEmpty(_vnpayQrImagePath) && File.Exists(_vnpayQrImagePath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(_vnpayQrImagePath, UriKind.Absolute);
                    bitmap.EndInit();

                    imgVnpayQrPreview.Source = bitmap;
                    iconNoVnpayQr.Visibility = Visibility.Collapsed;
                    btnRemoveVnpayQr.Visibility = Visibility.Visible;
                    txtVnpayHint.Text = "✅ Đang dùng QR tĩnh của bạn — khách quét QR này khi thanh toán VNPay.";
                }
                catch
                {
                    ClearVnpayQrPreview();
                }
            }
            else
            {
                ClearVnpayQrPreview();
            }
        }

        private void ClearVnpayQrPreview()
        {
            _vnpayQrImagePath = null;
            imgVnpayQrPreview.Source = null;
            iconNoVnpayQr.Visibility = Visibility.Visible;
            btnRemoveVnpayQr.Visibility = Visibility.Collapsed;
            txtVnpayHint.Text = "Chưa có QR — hệ thống sẽ tự tạo QR mô phỏng khi thanh toán.";
        }

        private void BtnUploadQr_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Chọn ảnh QR MoMo",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.webp",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string sourceFile = openFileDialog.FileName;
                    string ext = Path.GetExtension(sourceFile);
                    
                    string posDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TMDT_POS");
                    Directory.CreateDirectory(posDir);
                    
                    string targetFile = Path.Combine(posDir, $"momo_qr_{DateTime.Now.Ticks}{ext}");
                    File.Copy(sourceFile, targetFile, true);
                    
                    _qrImagePath = targetFile;
                    UpdateQrPreview();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi tải ảnh: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRemoveQr_Click(object sender, RoutedEventArgs e)
        {
            ClearQrPreview();
        }

        private void BtnUploadVnpayQr_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Chọn ảnh QR chuyển khoản VNPay",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.webp",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string sourceFile = openFileDialog.FileName;
                    string ext = Path.GetExtension(sourceFile);

                    string posDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TMDT_POS");
                    Directory.CreateDirectory(posDir);

                    string targetFile = Path.Combine(posDir, $"vnpay_qr_{DateTime.Now.Ticks}{ext}");
                    File.Copy(sourceFile, targetFile, true);

                    _vnpayQrImagePath = targetFile;
                    UpdateVnpayQrPreview();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi tải ảnh: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRemoveVnpayQr_Click(object sender, RoutedEventArgs e)
        {
            ClearVnpayQrPreview();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var phone = txtMoMoPhone.Text.Trim();

            // Validate: 10-11 số, bắt đầu bằng 0
            if (!string.IsNullOrEmpty(phone) && !Regex.IsMatch(phone, @"^0\d{9,10}$"))
            {
                MessageBox.Show("SĐT MoMo không hợp lệ. Vui lòng nhập 10-11 số, bắt đầu bằng 0.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMoMoPhone.Focus();
                txtMoMoPhone.SelectAll();
                return;
            }

            var settings = new PosSettings
            {
                MoMoPhone = string.IsNullOrEmpty(phone) ? null : phone,
                MoMoQrImagePath = _qrImagePath,
                VnpayBankName = (cboBank.SelectedItem as ComboBoxItem)?.Content as string,
                VnpayBankAccount = string.IsNullOrWhiteSpace(txtVnpayAccount.Text) ? null : txtVnpayAccount.Text.Trim(),
                VnpayQrImagePath = _vnpayQrImagePath,
                AutoReprintReceipt = chkAutoReprint.IsChecked == true,
                PrintAfterSync = chkPrintAfterSync.IsChecked == true,
            };

            PosSettingsHelper.Save(settings);
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnTestMoMo_Click(object sender, RoutedEventArgs e)
        {
            var phone = txtMoMoPhone.Text.Trim();
            if (string.IsNullOrEmpty(phone) && string.IsNullOrEmpty(_qrImagePath))
            {
                MessageBox.Show("Nhập SĐT hoặc tải ảnh QR trước khi test.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                txtMoMoPhone.Focus();
                return;
            }
            
            // Note: Cần save tạm setting để MockWindow đọc được QR Image (nếu có)
            var currentSettings = PosSettingsHelper.Current;
            string? oldImagePath = currentSettings.MoMoQrImagePath;
            currentSettings.MoMoQrImagePath = _qrImagePath;
            
            var test = new TMDT.Views.Components.MoMoMockWindow(10000m, phone, "TEST-001");
            test.ShowDialog();
            
            // Phục hồi lại setting cũ sau khi test (vì chưa bấm Save)
            currentSettings.MoMoQrImagePath = oldImagePath;
        }

        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Chỉ cho phép nhập số
            e.Handled = !char.IsDigit(e.Text, 0);
        }
    }
}
