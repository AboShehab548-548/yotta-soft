using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using accounting_project.Infrastructure;
using accounting_project.Models;

namespace accounting_project.Repositories
{
    public class VoucherDAL
    {
        private readonly string _connectionString;

        public VoucherDAL(string connectionString = null)
        {
            _connectionString = string.IsNullOrWhiteSpace(connectionString)
                ? DbConnectionFactory.ConnectionString
                : connectionString;
        }

        public List<AccountLookupModel> GetActiveAccounts()
        {
            var list = new List<AccountLookupModel>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"SELECT AccountCode, AccountNameAr, IsPostable
                FROM dbo.GL_Accounts WHERE IsActive=1 AND IsPostable=1 ORDER BY AccountCode", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(new AccountLookupModel
                        {
                            AccountCode = Convert.ToString(reader["AccountCode"]),
                            AccountNameAr = Convert.ToString(reader["AccountNameAr"]),
                            IsPostable = Convert.ToBoolean(reader["IsPostable"])
                        });
                }
            }
            return list;
        }

        public long SaveVoucher(VoucherHeader voucher)
        {
            ValidateVoucher(voucher);
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.sp_GL_Vouchers_Save", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                AddParameter(cmd, "@VoucherNo", SqlDbType.BigInt, voucher.VoucherNo);
                AddParameter(cmd, "@VoucherType", SqlDbType.TinyInt, voucher.VoucherType);
                AddParameter(cmd, "@VoucherDate", SqlDbType.DateTime2, voucher.VoucherDate);
                AddParameter(cmd, "@CompanyID", SqlDbType.Int, voucher.CompanyID);
                AddParameter(cmd, "@BranchID", SqlDbType.Int, voucher.BranchID);
                AddParameter(cmd, "@FiscalYearID", SqlDbType.Int, voucher.FiscalYearID);
                AddParameter(cmd, "@FiscalPeriodID", SqlDbType.Int, voucher.FiscalPeriodID);
                AddParameter(cmd, "@TreasuryAccountCode", SqlDbType.NVarChar, voucher.TreasuryAccountCode);
                AddParameter(cmd, "@PaymentType", SqlDbType.TinyInt, voucher.PaymentType);
                AddParameter(cmd, "@CurrencyID", SqlDbType.Int, voucher.CurrencyID);
                AddParameter(cmd, "@ExchangeRate", SqlDbType.Decimal, voucher.ExchangeRate);
                AddParameter(cmd, "@TotalAmount", SqlDbType.Decimal, voucher.TotalAmount);
                AddParameter(cmd, "@LocalTotalAmount", SqlDbType.Decimal, voucher.LocalTotalAmount);
                AddParameter(cmd, "@SourceType", SqlDbType.NVarChar, (object)voucher.SourceType ?? DBNull.Value);
                AddParameter(cmd, "@SourceID", SqlDbType.BigInt, (object)voucher.SourceID ?? DBNull.Value);
                AddParameter(cmd, "@Notes", SqlDbType.NVarChar, (object)voucher.Notes ?? DBNull.Value);
                AddParameter(cmd, "@CreatedBy", SqlDbType.Int, voucher.CreatedBy);

                var details = cmd.Parameters.Add("@Details", SqlDbType.Structured);
                details.TypeName = "dbo.GL_VoucherDetailType";
                details.Value = ConvertDetailsToDataTable(voucher.Details);

                conn.Open();
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        public long GetNextVoucherNo(byte voucherType, int fiscalYearId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.sp_GL_GetNextVoucherNo", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                AddParameter(cmd, "@VoucherType", SqlDbType.TinyInt, voucherType);
                AddParameter(cmd, "@FiscalYearID", SqlDbType.Int, fiscalYearId);
                conn.Open();
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        // توافق مع الاستدعاء القديم، لكنه يعتمد الآن على عداد ذري وليس MAX.
        public long GetMaxVoucherNo(int voucherType, int fiscalYearID)
        {
            return GetNextVoucherNo((byte)voucherType, fiscalYearID) - 1;
        }

        public List<VoucherHeader> GetVouchersList(int voucherType = 1)
        {
            var list = new List<VoucherHeader>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"SELECT h.VoucherID, h.VoucherNo, h.VoucherDate,
                h.TreasuryAccountCode, ISNULL(a.AccountNameAr,'') TreasuryAccountName,
                h.TotalAmount, ISNULL(h.Notes,'') Notes, h.CompanyID, h.BranchID, h.FiscalYearID,
                h.FiscalPeriodID, h.IsPosted
                FROM dbo.GL_VouchersHeader h
                LEFT JOIN dbo.GL_Accounts a ON a.AccountCode=h.TreasuryAccountCode
                WHERE h.VoucherType=@VoucherType ORDER BY h.VoucherDate DESC, h.VoucherNo DESC", conn))
            {
                cmd.Parameters.Add("@VoucherType", SqlDbType.TinyInt).Value = voucherType;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(new VoucherHeader
                        {
                            VoucherID = Convert.ToInt64(reader["VoucherID"]),
                            VoucherNo = Convert.ToInt64(reader["VoucherNo"]),
                            VoucherDate = Convert.ToDateTime(reader["VoucherDate"]),
                            TreasuryAccountCode = reader["TreasuryAccountCode"] == DBNull.Value ? null : Convert.ToString(reader["TreasuryAccountCode"]),
                            TreasuryAccountName = Convert.ToString(reader["TreasuryAccountName"]),
                            TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                            Notes = Convert.ToString(reader["Notes"]),
                            CompanyID = Convert.ToInt32(reader["CompanyID"]),
                            BranchID = Convert.ToInt32(reader["BranchID"]),
                            FiscalYearID = Convert.ToInt32(reader["FiscalYearID"]),
                            FiscalPeriodID = Convert.ToInt32(reader["FiscalPeriodID"]),
                            IsPosted = Convert.ToBoolean(reader["IsPosted"])
                        });
                }
            }
            return list;
        }

        private static void ValidateVoucher(VoucherHeader voucher)
        {
            if (voucher == null) throw new ArgumentNullException("voucher");
            if (voucher.Details == null || voucher.Details.Count < 2) throw new InvalidOperationException("القيد يحتاج إلى سطرين على الأقل.");
            decimal debit = 0m, credit = 0m;
            foreach (var detail in voucher.Details)
            {
                if (string.IsNullOrWhiteSpace(detail.AccountID)) throw new InvalidOperationException("رقم الحساب مطلوب في كل سطر.");
                debit += detail.DebitAmount;
                credit += detail.CreditAmount;
            }
            if (debit <= 0m || Math.Abs(debit - credit) > 0.01m)
                throw new InvalidOperationException("القيد غير متوازن: إجمالي المدين يجب أن يساوي إجمالي الدائن.");
        }

        private static DataTable ConvertDetailsToDataTable(List<VoucherDetail> details)
        {
            var dt = new DataTable();
            dt.Columns.Add("VoucherLineNo", typeof(int));
            dt.Columns.Add("AccountCode", typeof(string));
            dt.Columns.Add("DebitAmount", typeof(decimal));
            dt.Columns.Add("CreditAmount", typeof(decimal));
            dt.Columns.Add("ForeignAmount", typeof(decimal));
            dt.Columns.Add("LocalAmount", typeof(decimal));
            dt.Columns.Add("CostCenterID", typeof(int));
            dt.Columns.Add("ReferenceNo", typeof(string));
            dt.Columns.Add("Notes", typeof(string));
            foreach (var item in details)
                dt.Rows.Add(item.VoucherLineNo, item.AccountID, item.DebitAmount, item.CreditAmount,
                    item.ForeignAmount, item.LocalAmount, item.CostCenterID.HasValue ? (object)item.CostCenterID.Value : DBNull.Value,
                    (object)item.ReferenceNo ?? DBNull.Value, (object)item.Notes ?? DBNull.Value);
            return dt;
        }

        private static void AddParameter(SqlCommand cmd, string name, SqlDbType type, object value)
        {
            var p = cmd.Parameters.Add(name, type);
            if (type == SqlDbType.Decimal) { p.Precision = 19; p.Scale = 4; }
            if (type == SqlDbType.NVarChar) p.Size = 200;
            p.Value = value ?? DBNull.Value;
        }
    }
}
