using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace TMDT.Views.Buyer
{
    public partial class ReviewDialog : Window
    {
        public int StarRating { get; private set; } = 5;
        public string ReviewContent { get; private set; } = "";

        public ReviewDialog(string productName)
        {
            InitializeComponent();
            ProductNameText.Text = productName;
            UpdateStars(5);
        }

        private void Star_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                if (int.TryParse(btn.Tag.ToString(), out int rating))
                {
                    StarRating = rating;
                    UpdateStars(rating);
                }
            }
        }

        private void UpdateStars(int rating)
        {
            SetStarState(Star1, rating >= 1);
            SetStarState(Star2, rating >= 2);
            SetStarState(Star3, rating >= 3);
            SetStarState(Star4, rating >= 4);
            SetStarState(Star5, rating >= 5);
        }

        private void SetStarState(Button btn, bool isFilled)
        {
            if (btn.Content is PackIcon icon)
            {
                icon.Kind = isFilled ? PackIconKind.Star : PackIconKind.StarOutline;
                icon.Foreground = isFilled ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (StarRating < 1)
            {
                MessageBox.Show("Vui lòng chọn số sao để đánh giá!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ReviewContent = ReviewContentBox.Text.Trim();
            DialogResult = true;
            Close();
        }
    }
}
