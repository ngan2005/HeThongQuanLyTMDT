using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TMDT.Models;
using TMDT.Utilities;
using Microsoft.Win32;
using System.Windows.Controls;

namespace TMDT.ViewModels.Seller
{
    public class BarcodeViewModel : ViewModelBase
    {
        private readonly Product _product;
        private bool _isQRCode = true;
        private BitmapImage _barcodeImage;

        public string ProductName => _product.ProductName ?? "Sản phẩm";

        public bool IsQRCode
        {
            get => _isQRCode;
            set
            {
                if (SetProperty(ref _isQRCode, value))
                {
                    OnPropertyChanged(nameof(IsBarcode));
                    GenerateImage();
                }
            }
        }

        public bool IsBarcode
        {
            get => !_isQRCode;
            set
            {
                IsQRCode = !value;
            }
        }

        public BitmapImage BarcodeImage
        {
            get => _barcodeImage;
            set => SetProperty(ref _barcodeImage, value);
        }

        public ICommand SaveImageCommand { get; }
        public ICommand PrintCommand { get; }

        public BarcodeViewModel(Product product)
        {
            _product = product;
            SaveImageCommand = new RelayCommand(_ => ExecuteSaveImage());
            PrintCommand = new RelayCommand(_ => ExecutePrint());
            
            GenerateImage();
        }

        private void GenerateImage()
        {
            try
            {
                if (IsQRCode)
                {
                    // QR Code can contain URL. We simulate a product URL here.
                    string url = $"https://volox.vn/product/{_product.ProductId}";
                    BarcodeImage = BarcodeGenerator.GenerateQRCode(url);
                }
                else
                {
                    // Code 128 usually contains SKU or Product ID (Must be alphanumeric)
                    string content = $"VOLOX-{_product.ProductId:D6}";
                    BarcodeImage = BarcodeGenerator.GenerateCode128(content);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo mã: {ex.Message}");
            }
        }

        private void ExecuteSaveImage()
        {
            if (BarcodeImage == null) return;

            var dlg = new SaveFileDialog
            {
                FileName = $"{(IsQRCode ? "QR" : "Barcode")}_{_product.ProductId}",
                DefaultExt = ".png",
                Filter = "PNG Image (.png)|*.png"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using (var fileStream = new FileStream(dlg.FileName, FileMode.Create))
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(BarcodeImage));
                        encoder.Save(fileStream);
                    }
                    MessageBox.Show("Lưu hình ảnh thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi lưu file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecutePrint()
        {
            if (BarcodeImage == null) return;

            try
            {
                PrintDialog printDlg = new PrintDialog();
                if (printDlg.ShowDialog() == true)
                {
                    Image img = new Image
                    {
                        Source = BarcodeImage,
                        Stretch = System.Windows.Media.Stretch.Uniform,
                        Margin = new Thickness(20)
                    };
                    
                    StackPanel printPanel = new StackPanel { Margin = new Thickness(50) };
                    printPanel.Children.Add(new TextBlock { Text = ProductName, FontSize = 24, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0,0,0,20) });
                    printPanel.Children.Add(img);

                    // Add to a measure/arrange pass to render it before printing
                    printPanel.Measure(new Size(printDlg.PrintableAreaWidth, printDlg.PrintableAreaHeight));
                    printPanel.Arrange(new Rect(new Point(0, 0), printPanel.DesiredSize));

                    printDlg.PrintVisual(printPanel, "In Mã Vạch Sản Phẩm");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi in: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
