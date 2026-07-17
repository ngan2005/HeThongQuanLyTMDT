using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace TMDT.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            if (parameter?.ToString() == "Inverse")
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return value;
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter?.ToString() == "Inverse")
            {
                return value == null ? Visibility.Visible : Visibility.Collapsed;
            }
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HexToCharConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            {
                try
                {
                    int code = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                    return ((char)code).ToString();
                }
                catch
                {
                    return ""; 
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EqualityToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return value.ToString().Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter != null)
            {
                try
                {
                    return System.Convert.ChangeType(parameter.ToString(), targetType);
                }
                catch
                {
                    return parameter.ToString();
                }
            }
            return Binding.DoNothing;
        }
    }

    public class PageToNavForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = value != null && parameter != null &&
                value.ToString().Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase);

            return isActive
                ? Application.Current.FindResource("BrandPrimaryBrush")
                : Application.Current.FindResource("TextMutedBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class PageToNavFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = value != null && parameter != null &&
                value.ToString().Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase);

            return isActive ? FontWeights.Bold : FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class Base64ToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string base64 && !string.IsNullOrWhiteSpace(base64))
            {
                try
                {
                    byte[] bytes = System.Convert.FromBase64String(base64);
                    using var ms = new System.IO.MemoryStream(bytes);
                    var image = new System.Windows.Media.Imaging.BitmapImage();
                    image.BeginInit();
                    image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringNullOrEmptyToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEmpty = string.IsNullOrWhiteSpace(value as string);

            if (targetType == typeof(Visibility))
            {
                bool inverse = parameter?.ToString() == "Inverse";
                bool show = inverse ? isEmpty : !isEmpty;
                return show ? Visibility.Visible : Visibility.Collapsed;
            }

            bool inverse2 = parameter?.ToString() == "Inverse";
            return inverse2 ? isEmpty : !isEmpty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GreaterThanZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i) return i > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (value is decimal d) return d > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (value is double dbl) return dbl > 0 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i && parameter is string s && int.TryParse(s, out int param))
                return i == param;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b && parameter != null && int.TryParse(parameter.ToString(), out int param))
                return param;
            return Binding.DoNothing;
        }
    }

    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEmpty = (value as int? ?? 0) == 0;
            bool showWhenEmpty = parameter?.ToString() == "Empty";
            bool result = showWhenEmpty ? isEmpty : !isEmpty;
            return result ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>Lấy ký tự đầu tiên (viết hoa) của chuỗi — dùng làm fallback avatar.</summary>
    public class InitialLetterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
                return s.Trim()[0].ToString().ToUpper();
            return "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            return string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            return string.IsNullOrWhiteSpace(str) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NumericLessThanZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal d) return d < 0;
            if (value is int i) return i < 0;
            if (value is double dbl) return dbl < 0;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StockToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int stock) return stock > 0;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StockToTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int stock && stock <= 0)
                return "Sản phẩm đã hết hàng";
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StockToBorderBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int stock && stock <= 0)
                return new SolidColorBrush(Color.FromRgb(254, 202, 202));
            return new SolidColorBrush(Color.FromRgb(226, 232, 240));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ProductCountToEmptyVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = value as int? ?? 0;
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ProductCountToItemsVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = value as int? ?? 0;
            return count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Hiển thị overlay "Hết hàng" khi StockQuantity = 0
    /// </summary>
    public class StockVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int stock = value is int i ? i : 0;
            return stock == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Hiển thị overlay đen mờ khi StockQuantity = 0 (cho image)
    /// </summary>
    public class OutOfStockOverlayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int stock = value is int i ? i : 0;
            return stock == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Tính số cột cho UniformGrid dựa trên chiều rộng container.
    /// Khoảng cách giữa các card là ~140px mỗi cột, tối thiểu 2, tối đa 6.
    /// </summary>
    public class WindowSizeToCardWidthConverter : IValueConverter
    {
        public double MinCardWidth { get; set; } = 140;
        public int MinColumns { get; set; } = 2;
        public int MaxColumns { get; set; } = 6;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && width > 0)
            {
                int cols = (int)(width / MinCardWidth);
                return Math.Max(MinColumns, Math.Min(MaxColumns, cols));
            }
            return MinColumns;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// 🟢 So sánh string với parameter — Visible nếu khớp, Collapsed nếu không.
    /// Dùng cho badge "(Shop)" / "(Global)" trong phí sàn.
    /// </summary>
    public class StringEqualsToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Visibility.Collapsed;
            return value.ToString()!.Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// 🟢 Nhận "OK" | "Low" | "OutOfStock" → trả về Brush màu tương ứng (xanh lá / vàng / đỏ).
    /// Dùng cho badge trạng thái tồn kho.
    /// </summary>
    public class InventoryStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value?.ToString() ?? "OK";
            return status switch
            {
                "OutOfStock" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),   // red-500
                "Low" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),           // amber-500
                _ => new SolidColorBrush(Color.FromRgb(16, 185, 129))                // emerald-500
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// 🟢 Nhận "OK" | "Low" | "OutOfStock" → trả về text tiếng Việt.
    /// </summary>
    public class InventoryStatusToVietnameseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value?.ToString() ?? "OK";
            return status switch
            {
                "OutOfStock" => "Hết hàng",
                "Low" => "Sắp hết",
                _ => "Bình thường"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// 🟢 Nhận "Import" | "Export" | "Adjust" | "Order" | "Refund" | "Cancel" → trả về Brush màu.
    /// </summary>
    public class InventoryTypeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string type = value?.ToString() ?? "";
            return type switch
            {
                "Import" => new SolidColorBrush(Color.FromRgb(16, 185, 129)),   // emerald-500
                "Export" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),   // amber-500
                "Order" => new SolidColorBrush(Color.FromRgb(59, 130, 246)),    // blue-500
                "Refund" => new SolidColorBrush(Color.FromRgb(168, 85, 247)),         // purple-500
                "Cancel" => new SolidColorBrush(Color.FromRgb(107, 114, 128)),  // gray-500
                "Adjust" => new SolidColorBrush(Color.FromRgb(20, 184, 166)),   // teal-500
                _ => new SolidColorBrush(Color.FromRgb(100, 116, 139))          // slate-500
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// 🟢 Nhận "Import" | "Export" | "Adjust" | ... → trả về dấu + / − / =.
    /// </summary>
    public class InventoryTypeToSignConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string type = value?.ToString() ?? "";
            return type switch
            {
                "Import" => "+",
                "Export" => "−",
                "Order" => "−",
                "Refund" => "+",
                "Cancel" => "+",
                "Adjust" => "↔",
                _ => ""
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
