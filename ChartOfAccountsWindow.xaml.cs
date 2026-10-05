using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using accounting_project.Infrastructure;
using accounting_project.Models;
using accounting_project.Repositories;

namespace accounting_project
{
    public partial class ChartOfAccountsWindow : Window
    {
        private readonly string connStr = DbConnectionFactory.ConnectionString;
        private DataTable dtAccounts;

        public ChartOfAccountsWindow()
        {
            InitializeComponent();
            LoadComboBoxes();
            LoadAccountsTree();
            LoadCurrencies();
        }

        private void LoadComboBoxes()
        {
            cmbAccountType.Items.Add(new { ID = 1, Name = "رئيسي" });
            cmbAccountType.Items.Add(new { ID = 2, Name = "فرعي قابل للترحيل" });
            cmbAccountType.DisplayMemberPath = "Name";
            cmbAccountType.SelectedValuePath = "ID";
            cmbAccountType.SelectedIndex = 0;
            cmbReportType.Items.Add(new { ID = 1, Name = "المركز المالي" });
            cmbReportType.Items.Add(new { ID = 2, Name = "قائمة الدخل" });
            cmbReportType.DisplayMemberPath = "Name";
            cmbReportType.SelectedValuePath = "ID";
            cmbReportType.SelectedIndex = 0;
            cmbAccountNature.Items.Add(new { ID = 1, Name = "مدين" });
            cmbAccountNature.Items.Add(new { ID = 2, Name = "دائن" });
            cmbAccountNature.DisplayMemberPath = "Name";
            cmbAccountNature.SelectedValuePath = "ID";
            cmbAccountNature.SelectedIndex = 0;
        }

        private void LoadCurrencies()
        {
            try
            {
                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand("SELECT CurrencyID,CurrencyNameAr FROM dbo.System_Currencies WHERE IsActive=1 ORDER BY CurrencyCode", conn))
                using (var da = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable(); da.Fill(dt);
                    cmbCurrency.ItemsSource = dt.DefaultView;
                    cmbCurrency.DisplayMemberPath = "CurrencyNameAr";
                    cmbCurrency.SelectedValuePath = "CurrencyID";
                }
            }
            catch (Exception ex) { MessageBox.Show("خطأ في جلب العملات: " + ex.Message); }
        }

        public void LoadAccountsTree(string searchText = "")
        {
            try
            {
                if (dtAccounts == null)
                {
                    using (var conn = new SqlConnection(connStr))
                    using (var cmd = new SqlCommand(@"SELECT AccountCode,AccountNameAr,ParentAccountCode,AccountLevel,AccountType,IsPostable
                        FROM dbo.GL_Accounts ORDER BY AccountLevel,AccountCode", conn))
                    using (var da = new SqlDataAdapter(cmd))
                    { dtAccounts = new DataTable(); da.Fill(dtAccounts); }
                }
                var rows = dtAccounts.AsEnumerable().ToList();
                var visible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                searchText = (searchText ?? "").Trim();
                if (searchText.Length > 0)
                    foreach (var row in rows.Where(r => r.Field<string>("AccountCode").IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 || r.Field<string>("AccountNameAr").IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0))
                        AddWithAncestors(row.Field<string>("AccountCode"), visible);

                tvAccounts.Items.Clear();
                foreach (var row in rows.Where(r => row.IsNull("ParentAccountCode") || string.IsNullOrWhiteSpace(row.Field<string>("ParentAccountCode"))))
                {
                    var code = row.Field<string>("AccountCode");
                    if (searchText.Length > 0 && !visible.Contains(code)) continue;
                    var item = new TreeViewItem { Header = code + " - " + row.Field<string>("AccountNameAr"), Tag = code, IsExpanded = searchText.Length > 0 };
                    AddChildAccountsFiltered(item, rows, code, searchText, visible);
                    tvAccounts.Items.Add(item);
                }
            }
            catch (Exception ex) { MessageBox.Show("خطأ في تحميل الشجرة المحاسبية: " + ex.Message); }
        }

        private void AddWithAncestors(string code, HashSet<string> visible)
        {
            if (string.IsNullOrWhiteSpace(code) || !visible.Add(code) || dtAccounts == null) return;
            var row = dtAccounts.AsEnumerable().FirstOrDefault(r => string.Equals(r.Field<string>("AccountCode"), code, StringComparison.OrdinalIgnoreCase));
            if (row != null && !row.IsNull("ParentAccountCode")) AddWithAncestors(row.Field<string>("ParentAccountCode"), visible);
        }

        private void AddChildAccountsFiltered(TreeViewItem parent, List<DataRow> rows, string parentCode, string searchText, HashSet<string> visible)
        {
            foreach (var row in rows.Where(r => !r.IsNull("ParentAccountCode") && r.Field<string>("ParentAccountCode") == parentCode))
            {
                var code = row.Field<string>("AccountCode");
                if (searchText.Length > 0 && !visible.Contains(code)) continue;
                var child = new TreeViewItem { Header = code + " - " + row.Field<string>("AccountNameAr"), Tag = code, IsExpanded = searchText.Length > 0 };
                AddChildAccountsFiltered(child, rows, code, searchText, visible);
                parent.Items.Add(child);
            }
        }

        private void TxtSearchAccount_TextChanged(object sender, TextChangedEventArgs e) { LoadAccountsTree(txtSearchAccount.Text); }

        private void TvAccounts_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = tvAccounts.SelectedItem as TreeViewItem;
            if (item != null && item.Tag != null) FetchAccountDetails(item.Tag.ToString());
        }

        private void FetchAccountDetails(string code)
        {
            try
            {
                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand("SELECT AccountCode,ParentAccountCode,AccountNameAr,AccountNameEn,AccountType,ReportType,AccountNature,IsPostable,CurrencyCode FROM dbo.GL_Accounts WHERE AccountCode=@Code", conn))
                {
                    cmd.Parameters.Add("@Code", SqlDbType.NVarChar, 50).Value = code;
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read()) return;
                        txtAccountCode.Text = Convert.ToString(reader["AccountCode"]);
                        txtParentCode.Text = reader["ParentAccountCode"] == DBNull.Value ? "" : Convert.ToString(reader["ParentAccountCode"]);
                        txtAccountNameAr.Text = Convert.ToString(reader["AccountNameAr"]);
                        txtAccountNameEn.Text = reader["AccountNameEn"] == DBNull.Value ? "" : Convert.ToString(reader["AccountNameEn"]);
                        cmbAccountType.SelectedValue = Convert.ToInt32(reader["AccountType"]);
                        cmbReportType.SelectedValue = Convert.ToInt32(reader["ReportType"]);
                        cmbAccountNature.SelectedValue = Convert.ToInt32(reader["AccountNature"]);
                        chkIsSub.IsChecked = Convert.ToBoolean(reader["IsPostable"]);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("خطأ أثناء جلب تفاصيل الحساب: " + ex.Message); }
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            var item = tvAccounts.SelectedItem as TreeViewItem;
            txtParentCode.Text = item == null || item.Tag == null ? "" : item.Tag.ToString();
            txtAccountCode.Clear(); txtAccountNameAr.Clear(); txtAccountNameEn.Clear(); chkIsSub.IsChecked = false; txtAccountCode.Focus();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAccountCode.Text) || string.IsNullOrWhiteSpace(txtAccountNameAr.Text)) { MessageBox.Show("أدخل رقم الحساب واسم الحساب."); return; }
            try
            {
                var account = new AccountModel
                {
                    AccountCode = txtAccountCode.Text.Trim(), AccountNameAr = txtAccountNameAr.Text.Trim(),
                    AccountNameEn = string.IsNullOrWhiteSpace(txtAccountNameEn.Text) ? null : txtAccountNameEn.Text.Trim(),
                    ParentAccountCode = string.IsNullOrWhiteSpace(txtParentCode.Text) ? null : txtParentCode.Text.Trim(),
                    AccountType = Convert.ToInt32(cmbAccountType.SelectedValue ?? 1), IsPostable = chkIsSub.IsChecked == true,
                    ReportType = Convert.ToInt32(cmbReportType.SelectedValue ?? 1), AccountNature = Convert.ToInt32(cmbAccountNature.SelectedValue ?? 1),
                    AccountGroup = 1, CurrencyCode = "YER", IsActive = true
                };
                new AccountRepository(connStr).AddAccount(account);
                MessageBox.Show("تم حفظ الحساب.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                dtAccounts = null; LoadAccountsTree();
            }
            catch (Exception ex) { MessageBox.Show("خطأ أثناء الحفظ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var code = txtAccountCode.Text.Trim();
            if (code.Length == 0) return;
            if (MessageBox.Show("هل تريد حذف الحساب؟ سيتم رفض الحذف إذا كان له أبناء أو حركات.", "تأكيد", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                if (!new AccountRepository(connStr).DeleteAccount(code)) { MessageBox.Show("تعذر الحذف: الحساب مرتبط بأبناء أو حركات."); return; }
                MessageBox.Show("تم حذف الحساب.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                dtAccounts = null; LoadAccountsTree();
            }
            catch (Exception ex) { MessageBox.Show("خطأ أثناء الحذف: " + ex.Message); }
        }

        private void BtnOpenImport_Click(object sender, RoutedEventArgs e) { var w = new ImportAccountsWindow { Owner = this }; w.ShowDialog(); }
        private void BtnClose_Click(object sender, RoutedEventArgs e) { Close(); }
    }
}
