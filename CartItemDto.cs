using System.ComponentModel;

namespace AccountingSystem.UI
{
    public class CartItemDto : INotifyPropertyChanged
    {
        private decimal _quantity;
        private decimal _unitPrice;
        private decimal _total;

        public int ItemID { get; set; }
        public int ItemUnitID { get; set; }
        public string Barcode { get; set; }
        public string ItemName { get; set; }
        public string UnitName { get; set; }
        public decimal CostPrice { get; set; }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                    RecalculateTotal();
                }
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (_unitPrice != value)
                {
                    _unitPrice = value;
                    OnPropertyChanged(nameof(UnitPrice));
                    RecalculateTotal();
                }
            }
        }

        public decimal Total
        {
            get => _total;
            set
            {
                if (_total != value)
                {
                    _total = value;
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        private void RecalculateTotal()
        {
            Total = Quantity * UnitPrice;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}