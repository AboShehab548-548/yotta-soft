using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using accounting_project.Infrastructure;
using accounting_project.Models;
using accounting_project.Services;

namespace accounting_project
{
    public partial class PurchaseInvoiceWindow : Window
    {
        private DataTable dtInvoiceItems;
        private readonly string _connectionString = DbConnectionFactory.ConnectionString;

        public PurchaseInvoiceWindow()
        {
            InitializeComponent();
            InitInvoiceTable();
            LoadSuppliers();
            dpInvoiceDate.SelectedDate = DateTime.Now;
            dpExpiryDate.SelectedDate = DateTime.Now.AddMonths(6);
            txtInvoiceNumber.Text = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        private void InitInvoiceTable()
        {
            dtInvoiceItems = new DataTable();
            dtInvoiceItems.Columns.Add("ItemID", typeof(int));
            dtInvoiceItems.Columns.Add("ItemName", typeof(string));
            dtInvoiceItems.Columns.Add("Quantity", typeof(int));
            dtInvoiceItems.Columns.Add("UnitPrice", typeof(decimal));
            dtInvoiceItems.Columns.Add("BatchNumber", typeof(string));
            dtInvoiceItems.Columns.Add("ExpiryDate", typeof(DateTime));
            dtInvoiceItems.Columns.Add("TotalPrice", typeof(decimal));
            dgPurchaseItems.ItemsSource = dtInvoiceItems.DefaultView;
        }

        private void LoadSuppliers()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("SELECT SupplierID, SupplierNameAr FROM dbo.Suppliers WHERE IsActive=1 ORDER BY SupplierNameAr", conn))
                using (var da = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbSuppliers.ItemsSource = dt.DefaultView;
                    cmbSuppliers.DisplayMemberPath = "SupplierNameAr";
                    cmbSuppliers.SelectedValuePath = "SupplierID";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل الموردين: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddItem_Click(object sender, RoutedEventArgs e) { AddItemToGrid(); }
        private void TxtItemSearch_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) AddItemToGrid(); }

        private void AddItemToGrid()
        {
            var searchKey = txtItemSearch.Text.Trim();
            int qty;
            decimal price;
            if (string.IsNullOrWhiteSpace(searchKey)) { MessageBox.Show("أدخل اسم الصنف أو الباركود."); return; }
            if (!int.TryParse(txtQuantity.Text, out qty) || qty <= 0) { MessageBox.Show("أدخل كمية صحيحة."); return; }
            if (!decimal.TryParse(txtCostPrice.Text, out price) || price < 0) { MessageBox.Show("أدخل سعر شراء صحيح."); return; }

            int itemId = 0;
            var itemName = searchKey;
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT TOP 1 ItemID, ItemName FROM dbo.Items WHERE Barcode=@Key OR ItemName LIKE @SearchName", conn))
                    {
                        cmd.Parameters.AddWithValue("@Key", searchKey);
                        cmd.Parameters.AddWithValue("@SearchName", "%" + searchKey + "%");
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read()) { itemId = Convert.ToInt32(reader["ItemID"]); itemName = Convert.ToString(reader["ItemName"]); }
                        }
                    }
                    if (itemId == 0)
                    {
                        using (var cmd = new SqlCommand("INSERT dbo.Items(ItemCode,ItemName,Barcode) VALUES(@Code,@Name,@Barcode); SELECT CONVERT(int,SCOPE_IDENTITY());", conn))
                        {
                            cmd.Parameters.AddWithValue("@Code", "ITEM-" + DateTime.Now.ToString("yyyyMMddHHmmssfff"));
                            cmd.Parameters.AddWithValue("@Name", searchKey);
                            cmd.Parameters.AddWithValue("@Barcode", searchKey);
                            itemId = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("خطأ أثناء البحث عن الصنف: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); return; }

            var batchNo = "B-" + DateTime.Now.ToString("yyMMddHHmmss");
            var expiry = dpExpiryDate.SelectedDate ?? DateTime.Now.AddMonths(6);
            dtInvoiceItems.Rows.Add(itemId, itemName, qty, price, batchNo, expiry, qty * price);
            CalculateTotals();
            txtItemSearch.Clear(); txtQuantity.Text = "1"; txtCostPrice.Clear(); txtItemSearch.Focus();
        }

        private void TxtPaidAmount_TextChanged(object sender, TextChangedEventArgs e) { CalculateTotals(); }

        private void CalculateTotals()
        {
            decimal total = 0;
            foreach (DataRow row in dtInvoiceItems.Rows) total += Convert.ToDecimal(row["TotalPrice"]);
            lblTotalInvoice.Text = total.ToString("N2");
            decimal paid;
            if (!decimal.TryParse(txtPaidAmount.Text, out paid)) paid = 0;
            lblRemainingAmount.Text = (total - paid).ToString("N2");
        }

        private void BtnSavePurchase_Click(object sender, RoutedEventArgs e)
        {
            if (dtInvoiceItems.Rows.Count == 0) { MessageBox.Show("أضف أصنافاً إلى الفاتورة أولاً."); return; }
            if (cmbSuppliers.SelectedValue == null) { MessageBox.Show("اختر المورد."); return; }

            decimal total = 0, paid = 0;
            foreach (DataRow row in dtInvoiceItems.Rows) total += Convert.ToDecimal(row["TotalPrice"]);
            decimal.TryParse(txtPaidAmount.Text, out paid);
            if (paid < 0 || paid > total) { MessageBox.Show("قيمة المدفوع غير صحيحة."); return; }

            var items = new List<PurchaseItemDTO>();
            foreach (DataRow row in dtInvoiceItems.Rows)
                items.Add(new PurchaseItemDTO
                {
                    ItemID = Convert.ToInt32(row["ItemID"]),
                    ItemName = Convert.ToString(row["ItemName"]),
                    Quantity = Convert.ToInt32(row["Quantity"]),
                    UnitPrice = Convert.ToDecimal(row["UnitPrice"]),
                    BatchNumber = Convert.ToString(row["BatchNumber"]),
                    ExpiryDate = Convert.ToDateTime(row["ExpiryDate"])
                });

            try
            {
                var id = new PurchaseService(_connectionString).ProcessPurchaseInvoice(
                    Convert.ToInt32(cmbSuppliers.SelectedValue), txtInvoiceNumber.Text.Trim(), items, total, paid);
                MessageBox.Show("تم حفظ فاتورة الشراء وحركة المخزون. رقم السجل: " + id, "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                if (System.Windows.Interop.ComponentDispatcher.IsThreadModal) DialogResult = true; else Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء حفظ الفاتورة: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
