using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using accounting_project.Models;

namespace accounting_project.Repositories
{
    public class VoucherDAL
    {
        private readonly string _connectionString = @"Data Source=DESKTOP-TE00DUB\SQLEXPRESS;Initial Catalog=AccountingDB;Integrated Security=True;";

        // 1. دالة جلب الحسابات النشطة القادمة من قاعدة البيانات
        public List<AccountLookupModel> GetActiveAccounts()
        {
            var list = new List<AccountLookupModel>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT AccountCode, AccountNameAr FROM dbo.GL_Accounts WHERE IsActive = 1 ORDER BY AccountCode";
                using (var cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new AccountLookupModel
                        {
                            AccountCode = reader["AccountCode"].ToString(),
                            AccountNameAr = reader["AccountNameAr"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        // 2. دالة حفظ السند
        public long SaveVoucher(VoucherHeader voucher)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("dbo.sp_GL_Vouchers_Save", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@VoucherNo", voucher.VoucherNo);
                    cmd.Parameters.AddWithValue("@VoucherType", voucher.VoucherType);
                    cmd.Parameters.AddWithValue("@VoucherDate", voucher.VoucherDate);
                    cmd.Parameters.AddWithValue("@FiscalYearID", voucher.FiscalYearID);
                    cmd.Parameters.AddWithValue("@TreasuryAccountCode", voucher.TreasuryAccountCode);
                    cmd.Parameters.AddWithValue("@PaymentType", voucher.PaymentType);
                    cmd.Parameters.AddWithValue("@CurrencyID", voucher.CurrencyID);
                    cmd.Parameters.AddWithValue("@ExchangeRate", voucher.ExchangeRate);
                    cmd.Parameters.AddWithValue("@TotalAmount", voucher.TotalAmount);
                    cmd.Parameters.AddWithValue("@LocalTotalAmount", voucher.LocalTotalAmount);
                    cmd.Parameters.AddWithValue("@Notes", (object)voucher.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedBy", voucher.CreatedBy);

                    // تحويل قائمة التفاصيل إلى DataTable لإرسالها للإجراء المخزن
                    SqlParameter detailsParam = cmd.Parameters.AddWithValue("@Details", ConvertDetailsToDataTable(voucher.Details));
                    detailsParam.SqlDbType = SqlDbType.Structured;
                    detailsParam.TypeName = "dbo.VoucherDetailType";

                    object result = cmd.ExecuteScalar();
                    return Convert.ToInt64(result);
                }
            }
        }

        public long GetMaxVoucherNo(int voucherType, int fiscalYearID)
        {
            long maxNo = 0;
            string query = @"SELECT ISNULL(MAX(VoucherNo), 0) 
                    FROM dbo.GL_VouchersHeader 
                    WHERE VoucherType = @VoucherType AND FiscalYearID = @FiscalYearID";

            using (SqlConnection conn = new SqlConnection(@"Data Source=DESKTOP-TE00DUB\SQLEXPRESS;Initial Catalog=AccountingDB;Integrated Security=True"))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@VoucherType", voucherType);
                    cmd.Parameters.AddWithValue("@FiscalYearID", fiscalYearID);

                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        maxNo = Convert.ToInt64(result);
                    }
                }
            }

            return maxNo;
        }

        public List<VoucherHeader> GetVouchersList(int voucherType = 1)
        {
            var list = new List<VoucherHeader>();

            // إضافة COLLATE DATABASE_DEFAULT لحل تعارض Collation
            // وتعديل اسم عمود اسم الحساب إلى AccountName (أو الاسم المطابق لديك في جدول Accounts)
            string query = @"SELECT h.VoucherID, h.VoucherNo, h.VoucherDate, h.TreasuryAccountCode, 
                            ISNULL(a.AccountName, '') AS TreasuryAccountName, 
                            h.TotalAmount, ISNULL(h.Notes, '') AS Notes
                     FROM dbo.GL_VouchersHeader h
                     LEFT JOIN GL_Accounts a 
                            ON h.TreasuryAccountCode COLLATE DATABASE_DEFAULT = a.AccountCode COLLATE DATABASE_DEFAULT
                     WHERE h.VoucherType = @VoucherType
                     ORDER BY h.VoucherNo DESC";

            using (SqlConnection conn = new SqlConnection(@"Data Source=DESKTOP-TE00DUB\SQLEXPRESS;Initial Catalog=AccountingDB;Integrated Security=True"))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@VoucherType", voucherType);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new VoucherHeader
                            {
                                VoucherID = Convert.ToInt64(reader["VoucherID"]),
                                VoucherNo = Convert.ToInt64(reader["VoucherNo"]),
                                VoucherDate = Convert.ToDateTime(reader["VoucherDate"]),
                                TreasuryAccountCode = reader["TreasuryAccountCode"]?.ToString(),
                                TreasuryAccountName = reader["TreasuryAccountName"]?.ToString(),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                Notes = reader["Notes"]?.ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        // 3. دالة تحويل تفاصيل السند إلى DataTable
        private DataTable ConvertDetailsToDataTable(List<VoucherDetail> details)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("VoucherLineNo", typeof(int));
            dt.Columns.Add("AccountID", typeof(string));
            dt.Columns.Add("Amount", typeof(decimal));
            dt.Columns.Add("LocalAmount", typeof(decimal));
            dt.Columns.Add("Notes", typeof(string));

            foreach (var item in details)
            {
                dt.Rows.Add(
                    item.VoucherLineNo,
                    item.AccountID,
                    item.Amount,
                    item.LocalAmount,
                    (object)item.Notes ?? DBNull.Value
                );
            }

            return dt;
        }
    }
}