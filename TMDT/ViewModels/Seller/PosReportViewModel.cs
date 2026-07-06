using System;
using System.Windows.Input;
using TMDT.Utilities;
using System.Windows;

namespace TMDT.ViewModels.Seller
{
    public class PosReportViewModel : ViewModelBase
    {
        public DateTime ReportDate { get; set; } = DateTime.Today;
        
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        private decimal _totalCash;
        public decimal TotalCash
        {
            get => _totalCash;
            set
            {
                if (SetProperty(ref _totalCash, value))
                {
                    OnPropertyChanged(nameof(ExpectedCash));
                    OnPropertyChanged(nameof(Difference));
                }
            }
        }
        
        public decimal TotalMoMo { get; set; }
        public decimal TotalVNPay { get; set; }

        public decimal OpeningFloat
        {
            get => StartingCash;
            set
            {
                StartingCash = value;
                OnPropertyChanged();
            }
        }

        private decimal _startingCash;
        public decimal StartingCash
        {
            get => _startingCash;
            set
            {
                if (SetProperty(ref _startingCash, value))
                {
                    OnPropertyChanged(nameof(ExpectedCash));
                    OnPropertyChanged(nameof(Difference));
                }
            }
        }

        private decimal _actualCash;
        public decimal ActualCash
        {
            get => _actualCash;
            set
            {
                if (SetProperty(ref _actualCash, value))
                {
                    OnPropertyChanged(nameof(Difference));
                }
            }
        }

        public decimal ExpectedCash => StartingCash + TotalCash;
        public decimal Difference => ActualCash - ExpectedCash;

        public ICommand CloseCommand { get; }

        public PosReportViewModel()
        {
            CloseCommand = new RelayCommand(o => {
                if (o is Window window) window.Close();
            });
        }
    }
}
