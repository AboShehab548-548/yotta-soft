using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using accounting_project.Infrastructure;

namespace AccountingSystem.UI
{
    public partial class ItemManagementWindow : Window
    {
        private readonly string connStr = DbConnectionFactory.ConnectionString;
        private int selectedItemId;

        public ItemManagementWindow()
        {
            InitializeComponent();
            LoadItems();
        }

        private void LoadItems(string search = "")
        {
            try
            {
                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(@"SELECT ItemID,ItemCode,Barcode,ItemName,UnitName,CostPrice,UnitPrice,StockQuantity
                    FROM dbo.Items WHERE IsActive=1 AND (@Search='' OR ItemName LIKE @LikeSearch OR Barcode LIKE @LikeSearch OR ItemCode LIKE @LikeSearch)
                    ORDER BY ItemName", conn))
                {
                    var value = (search ?? "").Trim(); cmd.Parameters.Add("@Search", SqlDbType.NVarChar, 100).Value = value; cmd.Parameters.Add("@LikeSearch", SqlDbType.NVarChar, 110).Value = "%" + value + "%";
                    var table = new DataTable(); using (var da = new SqlDataAdapter(cmd)) da.Fill(table); dgItems.ItemsSource = table.DefaultView;
                }
            }
            catch (Exception ex) { MessageBox.Show("خطأ في تحميل الأصناف: " + ex.Message); }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) { LoadItems(txtSearch.Text); }

        private void dgItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var row = dgItems.SelectedItem as DataRowView;
            if (row == null) return;
            selectedItemId = Convert.ToInt32(row["ItemID"]);
            txtBarcode.Text = Convert.ToString(row["Barcode"]);
            txtItemName.Text = Convert.ToString(row["ItemName"]);
            txtCostPrice.Text = Convert.ToDecimal(row["CostPrice"]).ToString("0.####");
            txtUnitPrice.Text = Convert.ToDecimal(row["UnitPrice"]).ToString("0.####");
            var unit = Convert.ToString(row["UnitName"]);
            for (int i = 0; i < cmbUnit.Items.Count; i++) if (Convert.ToString(((ComboBoxItem)cmbUnit.Items[i]).Content) == unit) { cmbUnit.SelectedIndex = i; break; }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!TryRead(out var barcode, out var name, out var unit, out var cost, out var price)) return;
            try
            {
                var itemCode = string.IsNullOrWhiteSpace(barcode) ? "ITEM-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") : barcode;
                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(@"INSERT dbo.Items(ItemCode,Barcode,ItemName,UnitName,CostPrice,UnitPrice,StockQuantity,IsActive)
                    VALUES(@Code,@Barcode,@Name,@Unit,@Cost,@Price,0,1)", conn))
                {
                    Add(cmd, "@Code", SqlDbType.NVarChar, itemCode); Add(cmd, "@Barcode", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(barcode) ? (object)DBNull.Value : barcode); Add(cmd, "@Name", SqlDbType.NVarChar, name); Add(cmd, "@Unit", SqlDbType.NVarChar, unit); AddDecimal(cmd, "@Cost", cost); AddDecimal(cmd, "@Price", price); conn.Open(); cmd.ExecuteNonQuery();
                }
                MessageBox.Show("تم حفظ الصنف.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information); ClearFields(); LoadItems();
            }
            catch (Exception ex) { MessageBox.Show("تعذر حفظ الصنف: " + ex.Message); }
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItemId <= 0) { MessageBox.Show("اختر صنفاً للتعديل."); return; }
            if (!TryRead(out var barcode, out var name, out var unit, out var cost, out var price)) return;
            try
            {
                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(@"UPDATE dbo.Items SET Barcode=@Barcode,ItemName=@Name,UnitName=@Unit,CostPrice=@Cost,UnitPrice=@Price WHERE ItemID=@ID", conn))
                { Add(cmd, "@Barcode", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(barcode) ? (object)DBNull.Value : barcode); Add(cmd, "@Name", SqlDbType.NVarChar, name); Add(cmd, "@Unit", SqlDbType.NVarChar, unit); AddDecimal(cmd, "@Cost", cost); AddDecimal(cmd, "@Price", price); Add(cmd, "@ID", SqlDbType.Int, selectedItemId); conn.Open(); cmd.ExecuteNonQuery(); }
                MessageBox.Show("تم تعديل الصنف.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information); ClearFields(); LoadItems();
            }
            catch (Exception ex) { MessageBox.Show("تعذر تعديل الصنف: " + ex.Message); }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItemId <= 0) { MessageBox.Show("اختر صنفاً للحذف."); return; }
            if (MessageBox.Show("سيتم إيقاف الصنف عن الاستخدام دون حذف حركاته. هل تريد المتابعة؟", "تأكيد", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand("UPDATE dbo.Items SET IsActive=0 WHERE ItemID=@ID", conn)) { Add(cmd, "@ID", SqlDbType.Int, selectedItemId); conn.Open(); cmd.ExecuteNonQuery(); }
                ClearFields(); LoadItems();
            }
            catch (Exception ex) { MessageBox.Show("تعذر إيقاف الصنف: " + ex.Message); }
        }

        private bool TryRead(out string barcode, out string name, out string unit, out decimal cost, out decimal price)
        {
            barcode = txtBarcode.Text.Trim(); name = txtItemName.Text.Trim(); unit = ((ComboBoxItem)cmbUnit.SelectedItem)?.Content?.ToString() ?? "حبة";
            if (!decimal.TryParse(txtCostPrice.Text, out cost) || !decimal.TryParse(txtUnitPrice.Text, out price) || cost < 0 || price < 0 || name.Length == 0) { MessageBox.Show("أدخل اسم الصنف وأسعاراً صحيحة غير سالبة."); return false; }
            return true;
        }

        private void ClearFields() { selectedItemId = 0; txtBarcode.Clear(); txtItemName.Clear(); txtCostPrice.Text = "0.00"; txtUnitPrice.Text = "0.00"; cmbUnit.SelectedIndex = 0; dgItems.SelectedItem = null; }
        private void btnClear_Click(object sender, RoutedEventArgs e) { ClearFields(); }
        private void txtBarcode_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) txtItemName.Focus(); }
        private void PriceTextBox_GotFocus(object sender, RoutedEventArgs e) { ((TextBox)sender).SelectAll(); }
        private static void Add(SqlCommand cmd, string name, SqlDbType type, object value) { var p = cmd.Parameters.Add(name, type); p.Size = 200; p.Value = value ?? DBNull.Value; }
        private static void AddDecimal(SqlCommand cmd, string name, decimal value) { var p = cmd.Parameters.Add(name, SqlDbType.Decimal); p.Precision = 19; p.Scale = 6; p.Value = value; }
    }
}
