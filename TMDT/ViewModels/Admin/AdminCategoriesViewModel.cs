using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Admin
{
    public class AdminCategoriesViewModel : ViewModelBase
    {
        // Removed long-lived _context for async safety
        private ObservableCollection<Category> _categories;
        private Category _selectedCategory;

        // Input Fields
        private string _categoryName = "";
        private string _categoryIcon = "E179"; // default hex
        private int _categorySortOrder = 1;
        private bool _isEditing = false;

        // Detail Request Events for Lightbox
        public event Action ShowDetailRequest;
        public event Action HideDetailRequest;

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set { _categories = value; OnPropertyChanged(); }
        }

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set 
            { 
                _selectedCategory = value; 
                OnPropertyChanged(); 
                if (value != null)
                {
                    CategoryName = value.CategoryName;
                    CategoryIcon = value.Icon ?? "E179";
                    CategorySortOrder = value.SortOrder ?? 1;
                    IsEditing = true;
                    ShowDetailRequest?.Invoke();
                }
            }
        }

        public string CategoryName
        {
            get => _categoryName;
            set { _categoryName = value; OnPropertyChanged(); }
        }

        public string CategoryIcon
        {
            get => _categoryIcon;
            set { _categoryIcon = value; OnPropertyChanged(); }
        }

        public int CategorySortOrder
        {
            get => _categorySortOrder;
            set { _categorySortOrder = value; OnPropertyChanged(); }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set { _isEditing = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand SaveCategoryCommand { get; }
        public ICommand DeleteCategoryCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand CreateNewCommand { get; }
        public ICommand CloseDetailCommand { get; }

        public AdminCategoriesViewModel()
        {

            Categories = new ObservableCollection<Category>();

            // Setup Commands
            SaveCategoryCommand = new RelayCommand(ExecuteSaveCategory);
            DeleteCategoryCommand = new RelayCommand(ExecuteDeleteCategory);
            CancelEditCommand = new RelayCommand(ExecuteCancelEdit);
            CreateNewCommand = new RelayCommand(ExecuteCreateNew);
            CloseDetailCommand = new RelayCommand(ExecuteCloseDetail);

            _ = LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                using var context = new TmdtContext();
                if (await context.Categories.AnyAsync())
                {
                    var dbCategories = await context.Categories.AsNoTracking()
                        .Include(c => c.Products)
                        .OrderBy(c => c.SortOrder)
                        .ToListAsync();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Categories.Clear();
                        foreach (var cat in dbCategories)
                        {
                            Categories.Add(cat);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EF query for Categories failed: " + ex.Message);
            }
        }

        private void ClearInputs()
        {
            CategoryName = "";
            CategoryIcon = "E179";
            CategorySortOrder = Categories.Any() ? Categories.Max(c => c.SortOrder ?? 0) + 1 : 1;
            IsEditing = false;
            _selectedCategory = null;
            OnPropertyChanged(nameof(SelectedCategory));
        }

        // --- Commands Implementation ---

        private void ExecuteCreateNew(object obj)
        {
            ClearInputs();
            IsEditing = false;
            ShowDetailRequest?.Invoke();
        }

        private void ExecuteCloseDetail(object obj)
        {
            ClearInputs();
            HideDetailRequest?.Invoke();
        }

        private void ExecuteCancelEdit(object obj)
        {
            ClearInputs();
            HideDetailRequest?.Invoke();
        }

        private async void ExecuteSaveCategory(object obj)
        {
            if (string.IsNullOrWhiteSpace(CategoryName))
            {
                MessageBox.Show("Vui lòng nhập tên danh mục ngành hàng!", "Thông tin trống", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsEditing && SelectedCategory != null)
            {
                // Update
                SelectedCategory.CategoryName = CategoryName;
                SelectedCategory.Icon = CategoryIcon;
                SelectedCategory.SortOrder = CategorySortOrder;

                try
                {
                    using var context = new TmdtContext();
                    var dbCat = await context.Categories.FindAsync(SelectedCategory.CategoryId);
                    if (dbCat != null)
                    {
                        dbCat.CategoryName = CategoryName;
                        dbCat.Icon = CategoryIcon;
                        dbCat.SortOrder = CategorySortOrder;
                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
                }

                MessageBox.Show("Cập nhật danh mục thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Add new
                var newCat = new Category
                {
                    CategoryName = CategoryName,
                    Icon = CategoryIcon,
                    SortOrder = CategorySortOrder,
                    IsActive = true
                };

                try
                {
                    using var context = new TmdtContext();
                    context.Categories.Add(newCat);
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Database insert failed: " + ex.Message);
                }

                Categories.Add(newCat);
                MessageBox.Show("Thêm danh mục mới thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            _ = LoadCategoriesAsync();
            ClearInputs();
            HideDetailRequest?.Invoke();
        }

        private async void ExecuteDeleteCategory(object obj)
        {
            if (SelectedCategory == null) return;

            var result = MessageBox.Show($"Xác nhận thay đổi trạng thái hoạt động của danh mục '{SelectedCategory.CategoryName}'?", 
                                         "Xác nhận thay đổi", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            // Toggle active status
            SelectedCategory.IsActive = !(SelectedCategory.IsActive ?? true);

            try
            {
                using var context = new TmdtContext();
                var dbCat = await context.Categories.FindAsync(SelectedCategory.CategoryId);
                if (dbCat != null)
                {
                    dbCat.IsActive = SelectedCategory.IsActive;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update failed: " + ex.Message);
            }

            MessageBox.Show($"Đã cập nhật trạng thái hoạt động danh mục '{SelectedCategory.CategoryName}' thành công!", 
                            "Đã thực hiện", MessageBoxButton.OK, MessageBoxImage.Information);

            _ = LoadCategoriesAsync();
            ClearInputs();
            HideDetailRequest?.Invoke();
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
