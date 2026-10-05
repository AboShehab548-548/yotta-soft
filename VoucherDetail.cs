using accounting_project.ViewModels;

namespace accounting_project.Models
{
    public class VoucherDetail : ViewModelBase
    {
        private string _accountName;
        public string AccountName
        {
            get => _accountName;
            set { _accountName = value; OnPropertyChanged(); }
        }
        
        private int _voucherLineNo;
        public int VoucherLineNo
        {
            get => _voucherLineNo;
            set { _voucherLineNo = value; OnPropertyChanged(); }
        }

        private string _accountID;
        public string AccountID
        {
            get => _accountID;
            set { _accountID = value; OnPropertyChanged(); }
        }

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set { _amount = value; OnPropertyChanged(); }
        }

        private decimal _foreignAmount;
        public decimal ForeignAmount
        {
            get => _foreignAmount;
            set { _foreignAmount = value; OnPropertyChanged(); }
        }

        private decimal _localAmount;
        public decimal LocalAmount
        {
            get => _localAmount;
            set { _localAmount = value; OnPropertyChanged(); }
        }

        private int? _costCenterID;
        public int? CostCenterID
        {
            get => _costCenterID;
            set { _costCenterID = value; OnPropertyChanged(); }
        }

        private string _referenceNo;
        public string ReferenceNo
        {
            get => _referenceNo;
            set { _referenceNo = value; OnPropertyChanged(); }
        }

        private string _notes;
        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }
    }
}