using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace accounting_project.ViewModels
{
    public class StatementRowModel : ViewModelBase
    {
        private DateTime _date;
        private string _voucherNo;
        private string _voucherType;
        private string _description;
        private decimal _debit;
        private decimal _credit;
        private decimal _balance;

        public DateTime Date { get => _date; set { _date = value; OnPropertyChanged(); } }
        public string VoucherNo { get => _voucherNo; set { _voucherNo = value; OnPropertyChanged(); } }
        public string VoucherType { get => _voucherType; set { _voucherType = value; OnPropertyChanged(); } }
        public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }
        public decimal Debit { get => _debit; set { _debit = value; OnPropertyChanged(); } }
        public decimal Credit { get => _credit; set { _credit = value; OnPropertyChanged(); } }
        public decimal Balance { get => _balance; set { _balance = value; OnPropertyChanged(); } }
    }

    public class AccountStatementViewModel : ViewModelBase
    {
        // تم تعديل رقم الحساب الافتراضي ليطابق الحسابات الموجودة في قاعدة البيانات (مثل 1101)
        private string _accountCode = "1101";
        private string _accountName = "حساب 1101";
        private DateTime _fromDate = new DateTime(2026, 08, 01); // يغطي تاريخ الحركات الموجودة في شهر 8
        private DateTime _toDate = DateTime.Now;

        public string AccountCode { get => _accountCode; set { _accountCode = value; OnPropertyChanged(); } }
        public string AccountName { get => _accountName; set { _accountName = value; OnPropertyChanged(); } }
        public DateTime FromDate { get => _fromDate; set { _fromDate = value; OnPropertyChanged(); } }
        public DateTime ToDate { get => _toDate; set { _toDate = value; OnPropertyChanged(); } }

        public ObservableCollection<StatementRowModel> Transactions { get; set; } = new ObservableCollection<StatementRowModel>();

        public decimal TotalDebit => Transactions.Sum(x => x.Debit);
        public decimal TotalCredit => Transactions.Sum(x => x.Credit);
        public decimal ClosingBalance => TotalDebit - TotalCredit;

        public ICommand LoadStatementCommand { get; }
        public ICommand SearchAccountCommand { get; }

        public AccountStatementViewModel()
        {
            LoadStatementCommand = new RelayCommand(_ => LoadData());
            SearchAccountCommand = new RelayCommand(_ => SearchAccount());
        }

        private void LoadData()
        {
            Transactions.Clear();
            decimal currentBalance = 0;

            string connectionString = @"Data Source=DESKTOP-TE00DUB\SQLEXPRESS;Initial Catalog=AccountingDB;Integrated Security=True;";

            // استخدام عمود Amount بدلاً من LocalAmount وتعديل اسم الجدول لتطابق القاعدة
            string query = @"
                SELECT h.VoucherDate, 
                       CAST(h.VoucherNo AS VARCHAR(50)) AS VoucherNo, 
                       CAST(h.VoucherType AS VARCHAR(50)) AS VoucherType, 
                       ISNULL(d.Notes, h.Notes) AS Notes, 
                       d.Amount AS LocalAmount 
                FROM GL_VouchersDetails d
                INNER JOIN GL_VouchersHeader h ON d.VoucherID = h.VoucherID
                WHERE d.AccountID = @AccountCode 
                  AND h.VoucherDate >= @FromDate 
                  AND h.VoucherDate <= @ToDate
                ORDER BY h.VoucherDate ASC";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccountCode", AccountCode);
                        cmd.Parameters.AddWithValue("@FromDate", FromDate.Date);
                        cmd.Parameters.AddWithValue("@ToDate", ToDate.Date.AddDays(1).AddSeconds(-1));

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime date = reader.GetDateTime(0);
                                string voucherNo = reader.IsDBNull(1) ? "" : reader.GetValue(1).ToString();
                                string voucherType = reader.IsDBNull(2) ? "" : reader.GetValue(2).ToString();
                                string notes = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                decimal localAmount = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4);

                                decimal debit = localAmount > 0 ? localAmount : 0;
                                decimal credit = localAmount < 0 ? Math.Abs(localAmount) : 0;

                                currentBalance += (debit - credit);

                                Transactions.Add(new StatementRowModel
                                {
                                    Date = date,
                                    VoucherNo = voucherNo,
                                    VoucherType = voucherType,
                                    Description = notes,
                                    Debit = debit,
                                    Credit = credit,
                                    Balance = currentBalance
                                });
                            }
                        }
                    }
                }

                OnPropertyChanged(nameof(TotalDebit));
                OnPropertyChanged(nameof(TotalCredit));
                OnPropertyChanged(nameof(ClosingBalance));

                if (Transactions.Count == 0)
                {
                    MessageBox.Show("لا توجد حركات مسجلة لهذا الحساب في الفترة المحددة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء جلب التقارير: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchAccount()
        {
            AccountCode = "1101";
            AccountName = "الحساب الرئيسي 1101";
            LoadData();
        }
    }
}