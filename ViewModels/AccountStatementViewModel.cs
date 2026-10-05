using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using accounting_project.Infrastructure;

namespace accounting_project.ViewModels
{
    public class StatementRowModel : ViewModelBase
    {
        public DateTime Date { get; set; }
        public string VoucherNo { get; set; }
        public string VoucherType { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }

    public class AccountStatementViewModel : ViewModelBase
    {
        private string _accountCode = "1101";
        private string _accountName = "";
        private DateTime _fromDate = new DateTime(DateTime.Now.Year, 1, 1);
        private DateTime _toDate = DateTime.Now;

        public string AccountCode { get => _accountCode; set { _accountCode = value; OnPropertyChanged(); } }
        public string AccountName { get => _accountName; set { _accountName = value; OnPropertyChanged(); } }
        public DateTime FromDate { get => _fromDate; set { _fromDate = value; OnPropertyChanged(); } }
        public DateTime ToDate { get => _toDate; set { _toDate = value; OnPropertyChanged(); } }
        public ObservableCollection<StatementRowModel> Transactions { get; } = new ObservableCollection<StatementRowModel>();
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
            decimal balance = 0m;
            const string query = @"SELECT h.VoucherDate, CONVERT(varchar(50),h.VoucherNo) VoucherNo,
                    CONVERT(varchar(50),h.VoucherType) VoucherType,
                    ISNULL(d.Notes,h.Notes) Notes, d.DebitAmount, d.CreditAmount,
                    a.AccountNameAr
                FROM dbo.GL_VoucherDetails d
                INNER JOIN dbo.GL_VouchersHeader h ON h.VoucherID=d.VoucherID
                INNER JOIN dbo.GL_Accounts a ON a.AccountID=d.AccountID
                WHERE a.AccountCode=@AccountCode AND h.VoucherDate>=@FromDate
                  AND h.VoucherDate<@ToDate ORDER BY h.VoucherDate,h.VoucherNo,d.VoucherLineNo";
            try
            {
                using (var conn = new SqlConnection(DbConnectionFactory.ConnectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AccountCode", AccountCode ?? "");
                    cmd.Parameters.AddWithValue("@FromDate", FromDate.Date);
                    cmd.Parameters.AddWithValue("@ToDate", ToDate.Date.AddDays(1));
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var debit = Convert.ToDecimal(reader["DebitAmount"]);
                            var credit = Convert.ToDecimal(reader["CreditAmount"]);
                            balance += debit - credit;
                            AccountName = Convert.ToString(reader["AccountNameAr"]);
                            Transactions.Add(new StatementRowModel
                            {
                                Date = Convert.ToDateTime(reader["VoucherDate"]),
                                VoucherNo = Convert.ToString(reader["VoucherNo"]),
                                VoucherType = Convert.ToString(reader["VoucherType"]),
                                Description = reader["Notes"] == DBNull.Value ? "" : Convert.ToString(reader["Notes"]),
                                Debit = debit,
                                Credit = credit,
                                Balance = balance
                            });
                        }
                    }
                }
                RaiseTotals();
                if (Transactions.Count == 0) MessageBox.Show("لا توجد حركات لهذا الحساب في الفترة المحددة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء جلب كشف الحساب: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchAccount()
        {
            if (string.IsNullOrWhiteSpace(AccountCode)) AccountCode = "1101";
            LoadData();
        }

        private void RaiseTotals()
        {
            OnPropertyChanged(nameof(TotalDebit));
            OnPropertyChanged(nameof(TotalCredit));
            OnPropertyChanged(nameof(ClosingBalance));
        }
    }
}
