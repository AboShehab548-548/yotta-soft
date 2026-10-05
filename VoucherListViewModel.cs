using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace accounting_project.ViewModels
{
    // نموذج بيانات السند الواحد المعروض في الجدول
    public class VoucherModel : ViewModelBase
    {
        private string _voucherNo;
        private DateTime _voucherDate = DateTime.Now;
        private string _treasuryAccountName;
        private decimal _totalAmount;
        private string _notes;

        public string VoucherNo
        {
            get => _voucherNo;
            set { _voucherNo = value; OnPropertyChanged(); }
        }

        public DateTime VoucherDate
        {
            get => _voucherDate;
            set { _voucherDate = value; OnPropertyChanged(); }
        }

        public string TreasuryAccountName
        {
            get => _treasuryAccountName;
            set { _treasuryAccountName = value; OnPropertyChanged(); }
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set { _totalAmount = value; OnPropertyChanged(); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }
    }

    // الـ ViewModel الخاص بشاشة استعراض وسجل السندات
    public class VoucherListViewModel : ViewModelBase
    {
        private string _searchText;
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private VoucherModel _selectedVoucher;

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); FilterVouchers(); }
        }

        public DateTime? FromDate
        {
            get => _fromDate;
            set { _fromDate = value; OnPropertyChanged(); FilterVouchers(); }
        }

        public DateTime? ToDate
        {
            get => _toDate;
            set { _toDate = value; OnPropertyChanged(); FilterVouchers(); }
        }

        public VoucherModel SelectedVoucher
        {
            get => _selectedVoucher;
            set { _selectedVoucher = value; OnPropertyChanged(); }
        }

        // القوائم
        public ObservableCollection<VoucherModel> AllVouchers { get; set; } = new ObservableCollection<VoucherModel>();
        public ObservableCollection<VoucherModel> FilteredVouchers { get; set; } = new ObservableCollection<VoucherModel>();

        // الأوامر (Commands)
        public ICommand ResetFilterCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand PrintCommand { get; }

        public VoucherListViewModel()
        {
            ResetFilterCommand = new RelayCommand(_ => ResetFilters());
            RefreshCommand = new RelayCommand(_ => LoadVouchersFromDatabase());
            PrintCommand = new RelayCommand(_ => PrintSelectedVoucher(), _ => SelectedVoucher != null);

            // تحميل بيانات تجريبية أولية (يمكنك استبدالها لاحقاً بالاتصال بقاعدة البيانات)
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            AllVouchers.Add(new VoucherModel { VoucherNo = "JV-2026-0001", VoucherDate = DateTime.Now.AddDays(-2), TreasuryAccountName = "صندوق الشركة الرئيسي", TotalAmount = 150000, Notes = "إثبات تحصيل نقدي افتتاحية" });
            AllVouchers.Add(new VoucherModel { VoucherNo = "JV-2026-0002", VoucherDate = DateTime.Now.AddDays(-1), TreasuryAccountName = "بنك اليمن الدولي", TotalAmount = 350000, Notes = "سداد مصاريف إيجار المكتب" });
            AllVouchers.Add(new VoucherModel { VoucherNo = "JV-2026-0003", VoucherDate = DateTime.Now, TreasuryAccountName = "صندوق الفرع", TotalAmount = 75000, Notes = "شراء أدوات قرطاسية ومطبوعات" });

            FilterVouchers();
        }

        private void LoadVouchersFromDatabase()
        {
            try
            {
                AllVouchers.Clear();

                // استدعاء السندات من طبقة البيانات الخاصة بك (VoucherDAL)
                // مثال افتراضي لاستدعاء الدالة (تأكد من مطابقة اسم الدالة في الـ DAL لديك):
                // var vouchersList = VoucherDAL.GetAllVouchers(); 

                // foreach (var item in vouchersList)
                // {
                //     AllVouchers.Add(new VoucherModel 
                //     { 
                //         VoucherNo = item.VoucherNo, 
                //         VoucherDate = item.VoucherDate, 
                //         TreasuryAccountName = item.AccountName, 
                //         TotalAmount = item.TotalAmount, 
                //         Notes = item.Notes 
                //     });
                // }

                FilterVouchers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء تحميل السندات من قاعدة البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // منطق الفلترة المتقدمة (حسب النص المدخل وفترات التواريخ)
        private void FilterVouchers()
        {
            var query = AllVouchers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var keyword = SearchText.Trim().ToLower();
                query = query.Where(v =>
                    (v.VoucherNo != null && v.VoucherNo.ToLower().Contains(keyword)) ||
                    (v.Notes != null && v.Notes.ToLower().Contains(keyword)) ||
                    (v.TreasuryAccountName != null && v.TreasuryAccountName.ToLower().Contains(keyword))
                );
            }

            if (FromDate.HasValue)
            {
                query = query.Where(v => v.VoucherDate.Date >= FromDate.Value.Date);
            }

            if (ToDate.HasValue)
            {
                query = query.Where(v => v.VoucherDate.Date <= ToDate.Value.Date);
            }

            FilteredVouchers.Clear();
            foreach (var item in query)
            {
                FilteredVouchers.Add(item);
            }
        }

        private void ResetFilters()
        {
            SearchText = string.Empty;
            FromDate = null;
            ToDate = null;
            FilterVouchers();
        }

        private void PrintSelectedVoucher()
        {
            if (SelectedVoucher != null)
            {
                MessageBox.Show($"جاري إعداد طباعة السند رقم: {SelectedVoucher.VoucherNo}", "طباعة المستند", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}