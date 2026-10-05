using System;
using System.Data.SqlClient;
using System.Windows;
using accounting_project.Infrastructure;

namespace accounting_project
{
    public partial class AddBatchWindow : Window
    {
        public AddBatchWindow()
        {
            InitializeComponent();
            dpExpiryDate.SelectedDate = DateTime.Now.AddMonths(6);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            int qty;
            decimal price;
            if (string.IsNullOrWhiteSpace(txtItemName.Text) || string.IsNullOrWhiteSpace(txtBatchNumber.Text))
            { MessageBox.Show("أدخل اسم الصنف ورقم الدفعة."); return; }
            if (!int.TryParse(txtQuantity.Text, out qty) || qty <= 0) { MessageBox.Show("أدخل كمية صحيحة."); return; }
            if (!decimal.TryParse(txtCostPrice.Text, out price) || price < 0) { MessageBox.Show("أدخل سعراً صحيحاً."); return; }

            try
            {
                using (var conn = new SqlConnection(DbConnectionFactory.ConnectionString))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            int itemId;
                            using (var cmd = new SqlCommand(@"INSERT dbo.Items(ItemCode,ItemName,Barcode)
                                VALUES(@ItemCode,@ItemName,@Barcode); SELECT CONVERT(int,SCOPE_IDENTITY());", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@ItemCode", string.IsNullOrWhiteSpace(txtBarcode.Text) ? "ITEM-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") : txtBarcode.Text.Trim());
                                cmd.Parameters.AddWithValue("@ItemName", txtItemName.Text.Trim());
                                cmd.Parameters.AddWithValue("@Barcode", string.IsNullOrWhiteSpace(txtBarcode.Text) ? (object)DBNull.Value : txtBarcode.Text.Trim());
                                itemId = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                            long batchId;
                            using (var cmd = new SqlCommand(@"INSERT dbo.ItemBatches(ItemID,BranchID,BatchNo,ExpiryDate,CurrentQuantity,CostPrice)
                                VALUES(@ItemID,1,@BatchNo,@ExpiryDate,@Quantity,@CostPrice); SELECT CONVERT(bigint,SCOPE_IDENTITY());", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@ItemID", itemId);
                                cmd.Parameters.AddWithValue("@BatchNo", txtBatchNumber.Text.Trim());
                                cmd.Parameters.AddWithValue("@ExpiryDate", dpExpiryDate.SelectedDate ?? DateTime.Now.AddMonths(6));
                                cmd.Parameters.AddWithValue("@Quantity", qty);
                                cmd.Parameters.AddWithValue("@CostPrice", price);
                                batchId = Convert.ToInt64(cmd.ExecuteScalar());
                            }
                            using (var cmd = new SqlCommand(@"UPDATE dbo.Items SET StockQuantity=StockQuantity+@Quantity WHERE ItemID=@ItemID;
                                INSERT dbo.InventoryMovements(ItemID,BatchID,BranchID,MovementDate,MovementType,QuantityIn,UnitCost,SourceType)
                                VALUES(@ItemID,@BatchID,1,SYSUTCDATETIME(),1,@Quantity,@CostPrice,N'OpeningBalance');", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@ItemID", itemId);
                                cmd.Parameters.AddWithValue("@BatchID", batchId);
                                cmd.Parameters.AddWithValue("@Quantity", qty);
                                cmd.Parameters.AddWithValue("@CostPrice", price);
                                cmd.ExecuteNonQuery();
                            }
                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
                MessageBox.Show("تمت إضافة الصنف والدفعة وتسجيل الحركة المخزنية.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex) { MessageBox.Show("خطأ أثناء الإضافة: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}
