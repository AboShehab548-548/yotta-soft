using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using accounting_project.Models;
using accounting_project.Repositories;
using AccountingSystem.UI;

namespace accounting_project.ViewModels
{
    public class VoucherDetailModel : ViewModelBase
    {
        private string _accountID;
        private string _accountName;
        private decimal _amount;
        private decimal _debit;
        private decimal _credit;
        private string _currency = "YER";
        private decimal _exchangeRate = 1m;
        private string _lineDescription;

        public int VoucherLineNo { get; set; }
        public string AccountID { get => _accountID; set { _accountID = value; OnPropertyChanged(); OnPropertyChanged(nameof(AccountNo)); } }
        public string AccountNo { get => AccountID; set => AccountID = value; }
        public string AccountName { get => _accountName; set { _accountName = value; OnPropertyChanged(); } }
        public decimal Amount { get => _amount; set { _amount = value; OnPropertyChanged(); } }
        public decimal Debit { get => _debit; set { _debit = value; Amount = value; OnPropertyChanged(); } }
        public decimal Credit { get => _credit; set { _credit = value; Amount = -value; OnPropertyChanged(); } }
        public decimal LocalAmount => (Debit > 0 ? Debit : Credit) * ExchangeRate;
        public string Currency { get => _currency; set { _currency = value; OnPropertyChanged(); } }
        public decimal ExchangeRate { get => _exchangeRate; set { _exchangeRate = value <= 0 ? 1m : value; OnPropertyChanged(); OnPropertyChanged(nameof(LocalAmount)); } }
        public string LineDescription { get => _lineDescription; set { _lineDescription = value; OnPropertyChanged(); } }
        public string AnalyticalType { get; set; }
        public string Notes { get => LineDescription; set => LineDescription = value; }
    }

    public class VoucherViewModel : ViewModelBase
    {
        private string _voucherNo = "1";
        private DateTime _voucherDate = DateTime.Now;
        private string _referenceNo;
        private string _generalDescription;
        private string _treasuryAccountCode;
        private string _treasuryAccountName;
        private string _notes;

        public string VoucherNo { get => _voucherNo; set { _voucherNo = value; OnPropertyChanged(); OnPropertyChanged(nameof(DocumentNo)); } }
        public string DocumentNo { get => VoucherNo; set => VoucherNo = value; }
        public DateTime VoucherDate { get => _voucherDate; set { _voucherDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(DocumentDate)); } }
        public DateTime DocumentDate { get => VoucherDate; set => VoucherDate = value; }
        public string ReferenceNo { get => _referenceNo; set { _referenceNo = value; OnPropertyChanged(); } }
        public string GeneralDescription { get => _generalDescription; set { _generalDescription = value; Notes = value; OnPropertyChanged(); } }
        public string TreasuryAccountCode { get => _treasuryAccountCode; set { _treasuryAccountCode = value; OnPropertyChanged(); } }
        public string TreasuryAccountName { get => _treasuryAccountName; set { _treasuryAccountName = value; OnPropertyChanged(); } }
        public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }

        public ObservableCollection<VoucherDetailModel> Details { get; } = new ObservableCollection<VoucherDetailModel>();
        public ObservableCollection<VoucherDetailModel> JournalEntries => Details;
        public decimal TotalAmount => TotalDebit;
        public decimal TotalDebit => Details.Sum(x => x.Debit);
        public decimal TotalCredit => Details.Sum(x => x.Credit);
        public decimal BalanceDifference => TotalDebit - TotalCredit;

        public ICommand AddRowCommand { get; }
        public ICommand RemoveRowCommand { get; }
        public ICommand SearchTreasuryAccountCommand { get; }
        public ICommand SearchDetailAccountCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand OpenVoucherListCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand ViewCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ExitCommand { get; }

        public VoucherViewModel()
        {
            AddRowCommand = new RelayCommand(_ => AddNewRow());
            RemoveRowCommand = new RelayCommand(param => RemoveRow(param as VoucherDetailModel));
            SearchTreasuryAccountCommand = new RelayCommand(_ => { });
            SearchDetailAccountCommand = new RelayCommand(_ => { });
            SaveCommand = new RelayCommand(_ => SaveVoucher());
            PrintCommand = new RelayCommand(_ => PrintVoucher());
            OpenVoucherListCommand = new RelayCommand(_ => OpenList());
            AddCommand = AddRowCommand;
            EditCommand = new RelayCommand(_ => MessageBox.Show("التعديل يكون قبل الترحيل فقط.", "تنبيه"));
            ViewCommand = OpenVoucherListCommand;
            DeleteCommand = new RelayCommand(param => RemoveRow(param as VoucherDetailModel));
            ExitCommand = new RelayCommand(p => { });
            Details.CollectionChanged += (s, e) => RaiseTotals();
            AddNewRow();
            AddNewRow();
        }

        private void AddNewRow()
        {
            Details.Add(new VoucherDetailModel
            {
                VoucherLineNo = Details.Count + 1,
                Currency = UserSession.CurrentCurrencyCode,
                ExchangeRate = 1m
            });
            RaiseTotals();
        }

        private void RemoveRow(VoucherDetailModel row)
        {
            if (row == null || Details.Count <= 2)
            {
                MessageBox.Show("يجب أن يحتوي القيد على سطرين على الأقل.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Details.Remove(row);
            var no = 1;
            foreach (var item in Details) item.VoucherLineNo = no++;
            RaiseTotals();
        }

        private void SaveVoucher()
        {
            try
            {
                if (Details.Any(x => string.IsNullOrWhiteSpace(x.AccountNo))) throw new InvalidOperationException("أدخل أرقام الحسابات لكل الأسطر.");
                if (Math.Abs(BalanceDifference) > 0.01m) throw new InvalidOperationException("القيد غير متوازن.");
                long number;
                if (!long.TryParse(DocumentNo, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                    number = new VoucherDAL().GetNextVoucherNo(3, UserSession.CurrentFiscalYearId);

                var header = new VoucherHeader
                {
                    VoucherNo = number,
                    VoucherType = 3,
                    VoucherDate = DocumentDate,
                    CompanyID = UserSession.CurrentCompanyId,
                    BranchID = UserSession.CurrentBranchId,
                    FiscalYearID = UserSession.CurrentFiscalYearId,
                    FiscalPeriodID = UserSession.CurrentFiscalPeriodId,
                    CurrencyID = UserSession.CurrentCurrencyId,
                    ExchangeRate = 1m,
                    TotalAmount = TotalDebit,
                    LocalTotalAmount = TotalDebit,
                    SourceType = "JournalVoucher",
                    Notes = GeneralDescription,
                    CreatedBy = UserSession.CurrentUserId,
                    Details = Details.Select(x => new VoucherDetail
                    {
                        VoucherLineNo = x.VoucherLineNo,
                        AccountID = x.AccountNo,
                        Debit = x.Debit,
                        Credit = x.Credit,
                        ForeignAmount = x.Debit > 0 ? x.Debit : x.Credit,
                        LocalAmount = x.LocalAmount,
                        ReferenceNo = ReferenceNo,
                        Notes = x.LineDescription
                    }).ToList()
                };
                var id = new VoucherDAL().SaveVoucher(header);
                MessageBox.Show("تم حفظ القيد وترحيله. رقم السجل: " + id, "حفظ ناجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "تعذر حفظ القيد", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RaiseTotals()
        {
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(TotalDebit));
            OnPropertyChanged(nameof(TotalCredit));
            OnPropertyChanged(nameof(BalanceDifference));
        }

        private void PrintVoucher() { MessageBox.Show("تتم الطباعة من شاشة التقرير بعد الحفظ.", "طباعة"); }
        private void OpenList() { MessageBox.Show("استخدم شاشة استعراض القيود الحالية.", "استعراض"); }
    }
}
