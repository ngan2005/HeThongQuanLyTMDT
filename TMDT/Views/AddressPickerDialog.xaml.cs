using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Services;
using TMDT.ViewModels;

namespace TMDT.Views
{
    public partial class AddressPickerDialog : Window
    {
        private readonly AddressPickerViewModel _vm;

        public Models.Address? SavedAddress { get; private set; }
        public Models.FullAddress? SelectedFullAddress { get; private set; }
        public bool IsDefault { get; private set; }

        public AddressPickerDialog(Models.Address? existingAddress = null)
        {
            InitializeComponent();

            var http = new HttpClient();
            var addressService = new AddressService(http);
            _vm = new AddressPickerViewModel(addressService);

            DataContext = _vm;

            if (existingAddress != null)
            {
                _vm.RecipientName = existingAddress.RecipientName ?? "";
                _vm.Phone = existingAddress.Phone ?? "";
                _vm.Street = existingAddress.FullAddress ?? "";
            }

            _vm.AddressConfirmed += OnAddressConfirmed;
            _vm.Cancelled += () => { DialogResult = false; Close(); };

            Loaded += async (s, e) =>
            {
                await _vm.LoadProvincesAsync();
                RecipientNameBox.Focus();
            };
        }

        private void OnAddressConfirmed(FullAddress fullAddress)
        {
            SelectedFullAddress = fullAddress;
            IsDefault = _vm.IsDefaultAddress;

            SavedAddress = new Models.Address
            {
                RecipientName = _vm.RecipientName,
                Phone = _vm.Phone,
                FullAddress = fullAddress.ToString(),
                Ward = fullAddress.Ward,
                District = fullAddress.District,
                Province = fullAddress.Province,
                IsDefault = IsDefault
            };

            DialogResult = true;
            Close();
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
    }
}
