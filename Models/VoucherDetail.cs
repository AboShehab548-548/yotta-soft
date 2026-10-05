using accounting_project.ViewModels;

namespace accounting_project.Models
{
    public class VoucherDetail : ViewModelBase
    {
        private string _accountName;
        private string _accountID;
        private decimal _amount;
        private decimal _foreignAmount;
        private decimal _localAmount;
        private decimal _debit;
        private decimal _credit;
        private int _voucherLineNo;
        private int? _costCenterID;
        private string _referenceNo;
        private string _notes;

        public string AccountName { get => _accountName; set { _accountName = value; OnPropertyChanged(); } }
        public int VoucherLineNo { get => _voucherLineNo; set { _voucherLineNo = value; OnPropertyChanged(); } }
        public string AccountID { get => _accountID; set { _accountID = value; OnPropertyChanged(); } }
        public decimal Amount { get => _amount; set { _amount = value; OnPropertyChanged(); } }
        public decimal ForeignAmount { get => _foreignAmount; set { _foreignAmount = value; OnPropertyChanged(); } }
        public decimal LocalAmount { get => _localAmount; set { _localAmount = value; OnPropertyChanged(); } }
        public decimal Debit { get => _debit; set { _debit = value; OnPropertyChanged(); } }
        public decimal Credit { get => _credit; set { _credit = value; OnPropertyChanged(); } }
        public int? CostCenterID { get => _costCenterID; set { _costCenterID = value; OnPropertyChanged(); } }
        public string ReferenceNo { get => _referenceNo; set { _referenceNo = value; OnPropertyChanged(); } }
        public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }

        public decimal DebitAmount => Debit != 0 ? Debit : (Amount > 0 ? Amount : 0);
        public decimal CreditAmount => Credit != 0 ? Credit : (Amount < 0 ? -Amount : 0);
    }
}
