using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using accounting_project.Infrastructure;
using accounting_project.Models;

namespace accounting_project.Services
{
    public class PurchaseService
    {
        private readonly string _connectionString;

        public PurchaseService(string connectionString = null)
        {
            _connectionString = string.IsNullOrWhiteSpace(connectionString)
                ? DbConnectionFactory.ConnectionString
                : connectionString;
        }

        /// <summary>
        /// يحفظ فاتورة الشراء وتفاصيلها والدفعات وحركات المخزون ورصيد المورد ذرّياً.
        /// الترحيل العام للقيد يتم عبر VoucherDAL بعد إنشاء المستند المصدر، أو عبر إجراء ترحيل موحد في طبقة الأعمال.
        /// </summary>
        public long ProcessPurchaseInvoice(int supplierId, string invoiceNum, List<PurchaseItemDTO> items,
            decimal totalAmount, decimal paidAmount, int companyId = 1, int branchId = 1,
            int fiscalYearId = 0, int fiscalPeriodId = 0)
        {
            if (items == null || items.Count == 0) throw new ArgumentException("تفاصيل فاتورة الشراء مطلوبة.");
            if (totalAmount < 0 || paidAmount < 0 || paidAmount > totalAmount)
                throw new ArgumentException("قيم الفاتورة أو المدفوع غير صحيحة.");
            var calculatedTotal = items.Sum(x => x.Quantity * x.UnitPrice);
            if (Math.Abs(calculatedTotal - totalAmount) > 0.01m)
                throw new ArgumentException("إجمالي الفاتورة لا يساوي مجموع تفاصيلها.");

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        if (fiscalYearId <= 0 || fiscalPeriodId <= 0)
                            ResolveOpenContext(conn, transaction, out fiscalYearId, out fiscalPeriodId);
                        long invoiceId;
                        using (var cmd = new SqlCommand(@"INSERT dbo.PurchaseInvoices
                            (CompanyID,BranchID,SupplierID,InvoiceNumber,InvoiceDate,FiscalYearID,FiscalPeriodID,TotalAmount,PaidAmount)
                            VALUES(@CompanyID,@BranchID,@SupplierID,@InvoiceNumber,SYSUTCDATETIME(),@FiscalYearID,@FiscalPeriodID,@TotalAmount,@PaidAmount);
                            SELECT CONVERT(bigint,SCOPE_IDENTITY());", conn, transaction))
                        {
                            Add(cmd, "@CompanyID", SqlDbType.Int, companyId);
                            Add(cmd, "@BranchID", SqlDbType.Int, branchId);
                            Add(cmd, "@SupplierID", SqlDbType.Int, supplierId);
                            Add(cmd, "@InvoiceNumber", SqlDbType.NVarChar, invoiceNum);
                            Add(cmd, "@FiscalYearID", SqlDbType.Int, fiscalYearId);
                            Add(cmd, "@FiscalPeriodID", SqlDbType.Int, fiscalPeriodId);
                            AddDecimal(cmd, "@TotalAmount", totalAmount);
                            AddDecimal(cmd, "@PaidAmount", paidAmount);
                            invoiceId = Convert.ToInt64(cmd.ExecuteScalar());
                        }

                        foreach (var item in items)
                        {
                            if (item.ItemID <= 0 || item.Quantity <= 0 || item.UnitPrice < 0)
                                throw new ArgumentException("يوجد صنف أو كمية أو سعر غير صحيح.");

                            var batchNo = string.IsNullOrWhiteSpace(item.BatchNumber)
                                ? Guid.NewGuid().ToString("N").Substring(0, 12)
                                : item.BatchNumber.Trim();
                            long batchId;

                            using (var cmd = new SqlCommand(@"INSERT dbo.PurchaseInvoiceDetails
                                (PurchaseInvoiceID,ItemID,Quantity,UnitPrice,BatchNumber,ExpiryDate)
                                VALUES(@InvoiceID,@ItemID,@Quantity,@UnitPrice,@BatchNumber,@ExpiryDate);
                                INSERT dbo.ItemBatches(ItemID,BranchID,BatchNo,ExpiryDate,CurrentQuantity,CostPrice)
                                VALUES(@ItemID,@BranchID,@BatchNo,@ExpiryDate,@Quantity,@UnitPrice);
                                SELECT CONVERT(bigint,SCOPE_IDENTITY());", conn, transaction))
                            {
                                Add(cmd, "@InvoiceID", SqlDbType.BigInt, invoiceId);
                                Add(cmd, "@ItemID", SqlDbType.Int, item.ItemID);
                                AddDecimal(cmd, "@Quantity", item.Quantity);
                                AddDecimal(cmd, "@UnitPrice", item.UnitPrice);
                                Add(cmd, "@BatchNumber", SqlDbType.NVarChar, batchNo);
                                Add(cmd, "@BatchNo", SqlDbType.NVarChar, batchNo);
                                Add(cmd, "@BranchID", SqlDbType.Int, branchId);
                                Add(cmd, "@ExpiryDate", SqlDbType.Date, item.ExpiryDate == default(DateTime) ? (object)DBNull.Value : item.ExpiryDate.Date);
                                batchId = Convert.ToInt64(cmd.ExecuteScalar());
                            }

                            using (var cmd = new SqlCommand(@"UPDATE dbo.Items SET StockQuantity=StockQuantity+@Quantity WHERE ItemID=@ItemID;
                                INSERT dbo.InventoryMovements(ItemID,BatchID,BranchID,MovementDate,MovementType,QuantityIn,UnitCost,SourceType,SourceID)
                                VALUES(@ItemID,@BatchID,@BranchID,SYSUTCDATETIME(),1,@Quantity,@UnitCost,N'PurchaseInvoice',@SourceID);", conn, transaction))
                            {
                                Add(cmd, "@ItemID", SqlDbType.Int, item.ItemID);
                                Add(cmd, "@BatchID", SqlDbType.BigInt, batchId);
                                Add(cmd, "@BranchID", SqlDbType.Int, branchId);
                                AddDecimal(cmd, "@Quantity", item.Quantity);
                                AddDecimal(cmd, "@UnitCost", item.UnitPrice);
                                Add(cmd, "@SourceID", SqlDbType.BigInt, invoiceId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        using (var cmd = new SqlCommand("UPDATE dbo.Suppliers SET Balance=Balance+(@TotalAmount-@PaidAmount) WHERE SupplierID=@SupplierID;", conn, transaction))
                        {
                            AddDecimal(cmd, "@TotalAmount", totalAmount);
                            AddDecimal(cmd, "@PaidAmount", paidAmount);
                            Add(cmd, "@SupplierID", SqlDbType.Int, supplierId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return invoiceId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private static void ResolveOpenContext(SqlConnection conn, SqlTransaction transaction, out int fiscalYearId, out int fiscalPeriodId)
        {
            using (var cmd = new SqlCommand(@"SELECT TOP 1 y.FiscalYearID,p.FiscalPeriodID FROM dbo.GL_FiscalYears y INNER JOIN dbo.GL_FiscalPeriods p ON p.FiscalYearID=y.FiscalYearID WHERE CAST(GETDATE() AS date) BETWEEN y.StartDate AND y.EndDate AND CAST(GETDATE() AS date) BETWEEN p.StartDate AND p.EndDate AND y.IsClosed=0 AND p.IsClosed=0 ORDER BY p.PeriodNo", conn, transaction))
            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.Read()) throw new InvalidOperationException("لا توجد سنة أو فترة مالية مفتوحة تغطي تاريخ اليوم.");
                fiscalYearId = Convert.ToInt32(reader["FiscalYearID"]);
                fiscalPeriodId = Convert.ToInt32(reader["FiscalPeriodID"]);
            }
        }

        private static void Add(SqlCommand cmd, string name, SqlDbType type, object value)
        {
            var p = cmd.Parameters.Add(name, type);
            if (type == SqlDbType.NVarChar) p.Size = 200;
            p.Value = value ?? DBNull.Value;
        }

        private static void AddDecimal(SqlCommand cmd, string name, decimal value)
        {
            var p = cmd.Parameters.Add(name, SqlDbType.Decimal);
            p.Precision = 19;
            p.Scale = 4;
            p.Value = value;
        }
    }
}
