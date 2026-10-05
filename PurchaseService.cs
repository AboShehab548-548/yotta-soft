using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using accounting_project.Models;

    namespace accounting_project.Services
{
    public class PurchaseService
    {
        private readonly string _connectionString;

        public PurchaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void ProcessPurchaseInvoice(int supplierId, string invoiceNum, List<PurchaseItemDTO> items, decimal totalAmount, decimal paidAmount)
        {
            decimal remainingAmount = totalAmount - paidAmount;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // 1. إدراج رأس الفاتورة
                    string insertInvoiceQuery = @"
                        INSERT INTO PurchaseInvoices (InvoiceNumber, SupplierID, InvoiceDate, TotalAmount, PaidAmount, RemainingAmount)
                        VALUES (@InvoiceNum, @SupplierID, GETDATE(), @Total, @Paid, @Remaining);
                        SELECT SCOPE_IDENTITY();";

                    SqlCommand cmdInvoice = new SqlCommand(insertInvoiceQuery, conn, transaction);
                    cmdInvoice.Parameters.AddWithValue("@InvoiceNum", invoiceNum);
                    cmdInvoice.Parameters.AddWithValue("@SupplierID", supplierId);
                    cmdInvoice.Parameters.AddWithValue("@Total", totalAmount);
                    cmdInvoice.Parameters.AddWithValue("@Paid", paidAmount);
                    cmdInvoice.Parameters.AddWithValue("@Remaining", remainingAmount);

                    int purchaseInvoiceId = Convert.ToInt32(cmdInvoice.ExecuteScalar());

                    foreach (var item in items)
                    {
                        // 2. إدراج تفاصيل الفاتورة
                        string insertDetailQuery = @"
                            INSERT INTO PurchaseInvoiceDetails (PurchaseInvoiceID, ItemID, Quantity, UnitPrice, BatchNumber, ExpiryDate)
                            VALUES (@InvoiceID, @ItemID, @Qty, @Price, @Batch, @Expiry);";

                        SqlCommand cmdDetail = new SqlCommand(insertDetailQuery, conn, transaction);
                        cmdDetail.Parameters.AddWithValue("@InvoiceID", purchaseInvoiceId);
                        cmdDetail.Parameters.AddWithValue("@ItemID", item.ItemID);
                        cmdDetail.Parameters.AddWithValue("@Qty", item.Quantity);
                        cmdDetail.Parameters.AddWithValue("@Price", item.UnitPrice);
                        cmdDetail.Parameters.AddWithValue("@Batch", item.BatchNumber ?? (object)DBNull.Value);
                        cmdDetail.Parameters.AddWithValue("@Expiry", item.ExpiryDate);
                        cmdDetail.ExecuteNonQuery();

                        // 3. إضافة شحنة جديدة لتتبع تاريخ الانتهاء
                        string insertBatchQuery = @"
                            INSERT INTO ItemBatches (ItemID, BatchNumber, ExpiryDate, QuantityOnHand, CostPrice)
                            VALUES (@ItemID, @Batch, @Expiry, @Qty, @Cost);";

                        SqlCommand cmdBatch = new SqlCommand(insertBatchQuery, conn, transaction);
                        cmdBatch.Parameters.AddWithValue("@ItemID", item.ItemID);
                        cmdBatch.Parameters.AddWithValue("@Batch", string.IsNullOrEmpty(item.BatchNumber) ? Guid.NewGuid().ToString().Substring(0, 8) : item.BatchNumber);
                        cmdBatch.Parameters.AddWithValue("@Expiry", item.ExpiryDate);
                        cmdBatch.Parameters.AddWithValue("@Qty", item.Quantity);
                        cmdBatch.Parameters.AddWithValue("@Cost", item.UnitPrice);
                        cmdBatch.ExecuteNonQuery();

                        // 4. تحديث إجمالي المخزون في جدول الأصناف
                        string updateStockQuery = @"
                            UPDATE Items 
                            SET QuantityInStock = QuantityInStock + @Qty,
                                CostPrice = @Cost
                            WHERE ItemID = @ItemID;";

                        SqlCommand cmdStock = new SqlCommand(updateStockQuery, conn, transaction);
                        cmdStock.Parameters.AddWithValue("@Qty", item.Quantity);
                        cmdStock.Parameters.AddWithValue("@Cost", item.UnitPrice);
                        cmdStock.Parameters.AddWithValue("@ItemID", item.ItemID);
                        cmdStock.ExecuteNonQuery();
                    }

                    // 5. تحديث رصيد المورد
                    if (remainingAmount > 0)
                    {
                        string updateSupplierQuery = "UPDATE Suppliers SET Balance = Balance + @Remaining WHERE SupplierID = @SupplierID";
                        SqlCommand cmdSupplier = new SqlCommand(updateSupplierQuery, conn, transaction);
                        cmdSupplier.Parameters.AddWithValue("@Remaining", remainingAmount);
                        cmdSupplier.Parameters.AddWithValue("@SupplierID", supplierId);
                        cmdSupplier.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
}