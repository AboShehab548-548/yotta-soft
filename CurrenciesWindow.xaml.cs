using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using accounting_project.Infrastructure;

namespace accounting_project
{
    public partial class CurrenciesWindow : Window
    {
        private readonly string connStr = DbConnectionFactory.ConnectionString;
        private int selectedCurrencyId;

        public CurrenciesWindow() { InitializeComponent(); LoadCurrencies(); }

        private void LoadCurrencies()
        {
            try
            {
                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(@"SELECT CurrencyID,CurrencyNameAr AS CurrencyName,CurrencyCode AS Symbol,ExchangeRate,IsBase AS IsPrimary,ForeignName,MinExchangeRate,MaxExchangeRate,FractionsCount FROM dbo.System_Currencies WHERE IsActive=1 ORDER BY CurrencyID", conn))
                using (var da = new SqlDataAdapter(cmd))
                { var dt = new DataTable(); da.Fill(dt); dgCurrencies.ItemsSource = dt.DefaultView; }
            }
            catch (Exception ex) { MessageBox.Show("خطأ في تحميل العملات: " + ex.Message); }
        }

        private void DgCurrencies_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = dgCurrencies.SelectedItem as DataRowView;
            if (row == null) return;
            selectedCurrencyId = Convert.ToInt32(row["CurrencyID"]);
            txtCurrencyID.Text = selectedCurrencyId.ToString(); txtCurrencyName.Text = Convert.ToString(row["CurrencyName"]); txtSymbol.Text = Convert.ToString(row["Symbol"]); txtForeignName.Text = Convert.ToString(row["ForeignName"]);
            txtExchangeRate.Text = Convert.ToDecimal(row["ExchangeRate"]).ToString("0.######"); txtMinRate.Text = row["MinExchangeRate"] == DBNull.Value ? "" : Convert.ToDecimal(row["MinExchangeRate"]).ToString("0.######"); txtMaxRate.Text = row["MaxExchangeRate"] == DBNull.Value ? "" : Convert.ToDecimal(row["MaxExchangeRate"]).ToString("0.######"); txtFractions.Text = Convert.ToString(row["FractionsCount"]); chkIsPrimary.IsChecked = Convert.ToBoolean(row["IsPrimary"]);
        }

        private void TxtCurrencyID_LostFocus(object sender, RoutedEventArgs e)
        {
            int id; if (int.TryParse(txtCurrencyID.Text.Trim(), out id) && id > 0) selectedCurrencyId = id;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!TryRead(out var name, out var code, out var foreign, out var rate, out var min, out var max, out var fractions)) return;
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction(IsolationLevel.Serializable))
                    {
                        try
                        {
                            if (chkIsPrimary.IsChecked == true) using (var clear = new SqlCommand("UPDATE dbo.System_Currencies SET IsBase=0 WHERE IsActive=1", conn, tx)) clear.ExecuteNonQuery();
                            const string sql = @"IF EXISTS(SELECT 1 FROM dbo.System_Currencies WHERE CurrencyID=@ID)
UPDATE dbo.System_Currencies SET CurrencyNameAr=@Name,CurrencyCode=@Code,ForeignName=@ForeignName,ExchangeRate=@Rate,MinExchangeRate=@MinRate,MaxExchangeRate=@MaxRate,FractionsCount=@Fractions,IsBase=@IsBase,IsActive=1 WHERE CurrencyID=@ID
ELSE INSERT dbo.System_Currencies(CurrencyCode,CurrencyNameAr,ForeignName,ExchangeRate,MinExchangeRate,MaxExchangeRate,FractionsCount,IsBase,IsActive) VALUES(@Code,@Name,@ForeignName,@Rate,@MinRate,@MaxRate,@Fractions,@IsBase,1);";
                            using (var cmd = new SqlCommand(sql, conn, tx))
                            {
                                Add(cmd, "@ID", SqlDbType.Int, selectedCurrencyId); Add(cmd, "@Name", SqlDbType.NVarChar, name); Add(cmd, "@Code", SqlDbType.NVarChar, code); Add(cmd, "@ForeignName", SqlDbType.NVarChar, foreign); AddDecimal(cmd, "@Rate", rate); AddDecimalNullable(cmd, "@MinRate", min); AddDecimalNullable(cmd, "@MaxRate", max); Add(cmd, "@Fractions", SqlDbType.TinyInt, fractions); Add(cmd, "@IsBase", SqlDbType.Bit, chkIsPrimary.IsChecked == true); cmd.ExecuteNonQuery();
                            }
                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
                MessageBox.Show("تم حفظ العملة.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information); ClearFields(); LoadCurrencies();
            }
            catch (Exception ex) { MessageBox.Show("تعذر حفظ العملة: " + ex.Message); }
        }

        private bool TryRead(out string name, out string code, out string foreign, out decimal rate, out decimal? min, out decimal? max, out int fractions)
        {
            name = txtCurrencyName.Text.Trim(); code = txtSymbol.Text.Trim(); foreign = txtForeignName.Text.Trim(); min = null; max = null; rate = 0; fractions = 2;
            decimal value; int fraction;
            if (name.Length == 0 || code.Length == 0 || !decimal.TryParse(txtExchangeRate.Text, out rate) || rate <= 0 || !int.TryParse(txtFractions.Text, out fraction) || fraction < 0 || fraction > 6) { MessageBox.Show("أدخل اسم ورمز العملة وسعر تحويل صحيح."); return false; }
            fractions = fraction;
            if (!string.IsNullOrWhiteSpace(txtMinRate.Text)) { if (!decimal.TryParse(txtMinRate.Text, out value) || value < 0) { MessageBox.Show("أدنى سعر غير صحيح."); return false; } min = value; }
            if (!string.IsNullOrWhiteSpace(txtMaxRate.Text)) { if (!decimal.TryParse(txtMaxRate.Text, out value) || value < 0) { MessageBox.Show("أعلى سعر غير صحيح."); return false; } max = value; }
            return true;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCurrencyId <= 0 || MessageBox.Show("سيتم إيقاف العملة. هل تريد المتابعة؟", "تأكيد", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                using (var conn = new SqlConnection(connStr)) using (var cmd = new SqlCommand("UPDATE dbo.System_Currencies SET IsActive=0,IsBase=0 WHERE CurrencyID=@ID", conn)) { Add(cmd, "@ID", SqlDbType.Int, selectedCurrencyId); conn.Open(); cmd.ExecuteNonQuery(); }
                ClearFields(); LoadCurrencies();
            }
            catch (Exception ex) { MessageBox.Show("تعذر حذف العملة: " + ex.Message); }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e) { ClearFields(); }
        private void BtnClose_Click(object sender, RoutedEventArgs e) { Close(); }
        private void ClearFields() { selectedCurrencyId = 0; txtCurrencyID.Clear(); txtCurrencyName.Clear(); txtSymbol.Clear(); txtForeignName.Clear(); txtExchangeRate.Text = "1"; txtMinRate.Clear(); txtMaxRate.Clear(); txtFractions.Text = "2"; chkIsPrimary.IsChecked = false; }
        private static void Add(SqlCommand cmd, string name, SqlDbType type, object value) { var p = cmd.Parameters.Add(name, type); p.Size = 200; p.Value = value ?? DBNull.Value; }
        private static void AddDecimal(SqlCommand cmd, string name, decimal value) { var p = cmd.Parameters.Add(name, SqlDbType.Decimal); p.Precision = 19; p.Scale = 6; p.Value = value; }
        private static void AddDecimalNullable(SqlCommand cmd, string name, decimal? value) { var p = cmd.Parameters.Add(name, SqlDbType.Decimal); p.Precision = 19; p.Scale = 6; p.Value = value.HasValue ? (object)value.Value : DBNull.Value; }
    }
}
