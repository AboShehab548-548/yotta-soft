using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace accounting_project.ViewModels
{
    // كلاس سطر التفاصيل داخل السند
    public class VoucherDetailModel : ViewModelBase
    {
        private int _voucherLineNo;
        private string _accountID;
        private string _accountName;
        private decimal _amount;
        private decimal _localAmount;
        private string _notes;

        public int VoucherLineNo { get => _voucherLineNo; set { _voucherLineNo = value; OnPropertyChanged(); } }
        public string AccountID { get => _accountID; set { _accountID = value; OnPropertyChanged(); } }
        public string AccountName { get => _accountName; set { _accountName = value; OnPropertyChanged(); } }
        public decimal Amount
        {
            get => _amount;
            set
            {
                _amount = value;
                LocalAmount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LocalAmount));
            }
        }
        public decimal LocalAmount { get => _localAmount; set { _localAmount = value; OnPropertyChanged(); } }
        public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }
    }

    // كلاس الـ ViewModel الرئيسي للسندات
    public class VoucherViewModel : ViewModelBase
    {
        private string _voucherNo = "V-2026-0001";
        private DateTime _voucherDate = DateTime.Now;
        private string _treasuryAccountCode;
        private string _treasuryAccountName;
        private string _notes;

        public string VoucherNo { get => _voucherNo; set { _voucherNo = value; OnPropertyChanged(); } }
        public DateTime VoucherDate { get => _voucherDate; set { _voucherDate = value; OnPropertyChanged(); } }
        public string TreasuryAccountCode { get => _treasuryAccountCode; set { _treasuryAccountCode = value; OnPropertyChanged(); } }
        public string TreasuryAccountName { get => _treasuryAccountName; set { _treasuryAccountName = value; OnPropertyChanged(); } }
        public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }

        public ObservableCollection<VoucherDetailModel> Details { get; set; } = new ObservableCollection<VoucherDetailModel>();

        public decimal TotalAmount => Details.Sum(x => x.Amount);

        public ICommand AddRowCommand { get; }
        public ICommand RemoveRowCommand { get; }
        public ICommand SearchTreasuryAccountCommand { get; }
        public ICommand SearchDetailAccountCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand OpenVoucherListCommand { get; }

        public VoucherViewModel()
        {
            AddRowCommand = new RelayCommand(_ => AddNewRow());
            RemoveRowCommand = new RelayCommand(param => RemoveRow(param as VoucherDetailModel));
            SearchTreasuryAccountCommand = new RelayCommand(_ => SearchTreasury());
            SearchDetailAccountCommand = new RelayCommand(param => SearchDetail(param as VoucherDetailModel));
            SaveCommand = new RelayCommand(_ => SaveVoucher());
            PrintCommand = new RelayCommand(_ => PrintVoucher());
            OpenVoucherListCommand = new RelayCommand(_ => OpenList());

            // إضافة سطر تجريبي أولي عند فتح الشاشة
            AddNewRow();
            Details.CollectionChanged += (s, e) => OnPropertyChanged(nameof(TotalAmount));
        }

        private void AddNewRow()
        {
            Details.Add(new VoucherDetailModel
            {
                VoucherLineNo = Details.Count + 1,
                AccountID = "1211",
                AccountName = "الصندوق الرئيسي",
                Amount = 0
            });
            OnPropertyChanged(nameof(TotalAmount));
        }

        private void RemoveRow(VoucherDetailModel row)
        {
            if (row != null && Details.Count > 1)
            {
                Details.Remove(row);
                RenumberRows();
                OnPropertyChanged(nameof(TotalAmount));
            }
            else
            {
                MessageBox.Show("لا يمكن حذف السطر الأخير!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RenumberRows()
        {
            int i = 1;
            foreach (var item in Details)
            {
                item.VoucherLineNo = i++;
            }
        }

        private void SearchTreasury()
        {
            TreasuryAccountCode = "1211";
            TreasuryAccountName = "الصندوق العام الرئيسي";
        }

        private void SearchDetail(VoucherDetailModel row)
        {
            if (row != null)
            {
                row.AccountID = "3111";
                row.AccountName = "حساب العملاء والمشتركين";
            }
        }

        private void SaveVoucher()
        {
            MessageBox.Show("تم حفظ السند بنجاح وتوليد القيود!", "حفظ ناجح", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PrintVoucher()
        {
            MessageBox.Show("جاري إعداد تقرير طباعة السند...", "طباعة", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenList()
        {
            MessageBox.Show("فتح شاشة استعراض السندات السابقة...", "استعراض", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}