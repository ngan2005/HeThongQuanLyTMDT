using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TMDT.Models;

namespace TMDT.ViewModels.Buyer
{
    public class HomeViewModel : ViewModelBase
    {
        private string _searchQuery;
        public string SearchQuery
        {
            get => _searchQuery;
            set { _searchQuery = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Category> Categories { get; set; }
        public ObservableCollection<Product> FeaturedProducts { get; set; }
        public ObservableCollection<Banner> Banners { get; set; }

        public ICommand LoginCommand { get; }

        public HomeViewModel()
        {
            Categories = new ObservableCollection<Category>();
            FeaturedProducts = new ObservableCollection<Product>();
            Banners = new ObservableCollection<Banner>();

            LoginCommand = new TMDT.Utilities.RelayCommand(ExecuteLogin);

            LoadMockData();
        }

        private void ExecuteLogin(object parameter)
        {
            var loginView = new TMDT.Views.Auth.LoginView();
            loginView.ShowDialog();
        }

        private void LoadMockData()
        {
            // Danh mục mẫu
            Categories.Add(new Category { CategoryName = "Điện thoại - Máy tính bảng", Icon = "Smartphone" });
            Categories.Add(new Category { CategoryName = "Laptop - Máy tính", Icon = "Laptop" });
            Categories.Add(new Category { CategoryName = "Phụ kiện - Thiết bị số", Icon = "Headphones" });
            Categories.Add(new Category { CategoryName = "Điện gia dụng", Icon = "Home" });
            Categories.Add(new Category { CategoryName = "Thời trang nam", Icon = "Tshirt" });
            Categories.Add(new Category { CategoryName = "Thời trang nữ", Icon = "Dress" });

            // Sản phẩm mẫu bám sát hình ảnh
            FeaturedProducts.Add(new Product { ProductName = "iPhone 15 Pro Max 256GB", Price = 28990000, OriginalPrice = 34990000, Rating = 4.8m });
            FeaturedProducts.Add(new Product { ProductName = "Laptop ASUS ROG Zephyrus G14", Price = 26990000, OriginalPrice = 31990000, Rating = 4.7m });
            FeaturedProducts.Add(new Product { ProductName = "Tai nghe Apple AirPods Pro 2", Price = 5490000, OriginalPrice = 6490000, Rating = 4.9m });
            FeaturedProducts.Add(new Product { ProductName = "Apple Watch Series 9 45mm", Price = 9490000, OriginalPrice = 11990000, Rating = 4.6m });
            FeaturedProducts.Add(new Product { ProductName = "Nước hoa Chanel Coco Mademoiselle", Price = 2650000, OriginalPrice = 3290000, Rating = 4.5m });

            // Banner mẫu
            Banners.Add(new Banner { Title = "Rực rỡ mùa hè SALE đến 50%", ImageUrl = "pack://application:,,,/Resources/Images/banner_summer.png" });
        }
    }
}
