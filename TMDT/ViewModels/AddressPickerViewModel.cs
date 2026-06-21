using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Services;

namespace TMDT.ViewModels;

public class AddressPickerViewModel : INotifyPropertyChanged
{
    private readonly IAddressService _addressService;

    public event Action<FullAddress>? AddressConfirmed;
    public event Action? Cancelled;

        private ObservableCollection<VnProvince> _provinces = new();
    public ObservableCollection<VnProvince> Provinces
    {
        get => _provinces;
        set { _provinces = value; OnPropertyChanged(); }
    }

    private VnProvince? _selectedProvince;
    public VnProvince? SelectedProvince
    {
        get => _selectedProvince;
        set
        {
            if (_selectedProvince != value)
            {
                _selectedProvince = value;
                OnPropertyChanged();
                _ = OnProvinceChangedAsync();
            }
        }
    }

    private ObservableCollection<VnDistrict> _districts = new();
    public ObservableCollection<VnDistrict> Districts
    {
        get => _districts;
        set { _districts = value; OnPropertyChanged(); }
    }

    private VnDistrict? _selectedDistrict;
    public VnDistrict? SelectedDistrict
    {
        get => _selectedDistrict;
        set
        {
            if (_selectedDistrict != value)
            {
                _selectedDistrict = value;
                OnPropertyChanged();
                _ = OnDistrictChangedAsync();
            }
        }
    }

    private ObservableCollection<VnWard> _wards = new();
    public ObservableCollection<VnWard> Wards
    {
        get => _wards;
        set { _wards = value; OnPropertyChanged(); }
    }

    private VnWard? _selectedWard;
    public VnWard? SelectedWard
    {
        get => _selectedWard;
        set { _selectedWard = value; OnPropertyChanged(); }
    }

    private string _street = "";
    public string Street
    {
        get => _street;
        set { _street = value; OnPropertyChanged(); }
    }

    private string _recipientName = "";
    public string RecipientName
    {
        get => _recipientName;
        set { _recipientName = value; OnPropertyChanged(); }
    }

    private string _phone = "";
    public string Phone
    {
        get => _phone;
        set { _phone = value; OnPropertyChanged(); }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    private bool _isDefaultAddress;
    public bool IsDefaultAddress
    {
        get => _isDefaultAddress;
        set { _isDefaultAddress = value; OnPropertyChanged(); }
    }

    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ClearStreetCommand { get; }

    public AddressPickerViewModel(IAddressService addressService)
    {
        _addressService = addressService;
        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        CancelCommand = new RelayCommand(_ => Cancelled?.Invoke());
        ClearStreetCommand = new RelayCommand(_ => Street = "");
    }

    public async Task LoadProvincesAsync()
    {
        IsLoading = true;
        try
        {
            var provinces = await _addressService.GetProvincesAsync();
            Provinces = new ObservableCollection<VnProvince>(provinces);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OnProvinceChangedAsync()
    {
        Districts = new ObservableCollection<VnDistrict>();
        Wards = new ObservableCollection<VnWard>();
        SelectedDistrict = null;
        SelectedWard = null;

        if (SelectedProvince == null) return;

        IsLoading = true;
        try
        {
            var districts = await _addressService.GetDistrictsAsync(SelectedProvince.Code);
            Districts = new ObservableCollection<VnDistrict>(districts);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OnDistrictChangedAsync()
    {
        Wards = new ObservableCollection<VnWard>();
        SelectedWard = null;

        if (SelectedDistrict == null) return;

        IsLoading = true;
        try
        {
            var wards = await _addressService.GetWardsAsync(SelectedDistrict.Code);
            Wards = new ObservableCollection<VnWard>(wards);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanConfirm(object? _)
    {
        return !string.IsNullOrWhiteSpace(RecipientName)
            && !string.IsNullOrWhiteSpace(Phone)
            && SelectedProvince != null
            && SelectedDistrict != null
            && SelectedWard != null;
    }

        private void Confirm(object? _)
    {
        if (!CanConfirm(_)) return;

        var fullAddress = new Models.FullAddress
        {
            Street = Street,
            Ward = SelectedWard?.Name,
            District = SelectedDistrict?.Name,
            Province = SelectedProvince?.Name
        };

        AddressConfirmed?.Invoke(fullAddress);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
