using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using ZXing;
using ZXing.Windows.Compatibility;

namespace TMDT.Views.Seller
{
    public partial class ScannerWindow : Window
    {
        private FilterInfoCollection _videoDevices;
        private VideoCaptureDevice _videoSource;
        private readonly BarcodeReader _barcodeReader;
        public string ScannedBarcode { get; private set; }
        private bool _isScanning = false;

        public ScannerWindow()
        {
            InitializeComponent();
            
            _barcodeReader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[] { BarcodeFormat.CODE_128, BarcodeFormat.EAN_13, BarcodeFormat.EAN_8, BarcodeFormat.QR_CODE }
                }
            };

            this.Loaded += ScannerWindow_Loaded;
            this.PreviewKeyDown += ScannerWindow_PreviewKeyDown;
        }

        private void ScannerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (_videoDevices.Count == 0)
                {
                    MessageBox.Show("Không tìm thấy Camera nào trên máy tính!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                foreach (FilterInfo device in _videoDevices)
                {
                    CameraComboBox.Items.Add(device.Name);
                }

                CameraComboBox.SelectedIndex = 0; // Tự động chọn camera đầu tiên
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tìm Camera: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CameraComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CameraComboBox.SelectedIndex < 0) return;

            StopCamera();

            _videoSource = new VideoCaptureDevice(_videoDevices[CameraComboBox.SelectedIndex].MonikerString);
            _videoSource.NewFrame += VideoSource_NewFrame;
            _videoSource.Start();
            _isScanning = true;
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (!_isScanning) return;

            try
            {
                using (Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    // Hiển thị lên UI
                    var bitmapSource = ConvertBitmap(bitmap);
                    Dispatcher.Invoke(() => { CameraImage.Source = bitmapSource; });

                    // Giải mã Barcode
                    var result = _barcodeReader.Decode(bitmap);
                    if (result != null && !string.IsNullOrEmpty(result.Text))
                    {
                        _isScanning = false;
                        ScannedBarcode = result.Text;

                        // Phát tiếng Beep
                        System.Media.SystemSounds.Beep.Play();

                        // Tắt Camera & Đóng cửa sổ (phải chạy trên luồng UI)
                        Dispatcher.Invoke(() =>
                        {
                            this.DialogResult = true;
                            this.Close();
                        });
                    }
                }
            }
            catch (Exception)
            {
                // Bỏ qua lỗi khung hình
            }
        }

        private BitmapImage ConvertBitmap(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Cần thiết để dùng trên thread khác (UI thread)
                return bitmapImage;
            }
        }

        private void StopCamera()
        {
            if (_videoSource != null)
            {
                // Unsubscribe first so no more NewFrame events can dispatch to the UI thread
                _videoSource.NewFrame -= VideoSource_NewFrame;
                if (_videoSource.IsRunning)
                {
                    _videoSource.SignalToStop();
                }
                _videoSource = null;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _isScanning = false;
            StopCamera();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ScannerWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }
    }
}
