using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using accounting_project.Infrastructure;
using accounting_project.Models;

namespace accounting_project.Repositories
{
    public class AccountRepository
    {
        private readonly string _connectionString;

        public AccountRepository(string connectionString)
        {
            _connectionString = string.IsNullOrWhiteSpace(connectionString)
                ? DbConnectionFactory.ConnectionString
                : connectionString;
        }

        public AccountRepository() : this(DbConnectionFactory.ConnectionString) { }

        public int ImportAccountsFromList(List<AccountModel> accounts)
        {
            if (accounts == null || accounts.Count == 0) return 0;
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        var count = 0;
                        foreach (var account in accounts)
                        {
                            ValidateAccount(account);
                            var parentCode = NormalizeParent(account.ParentAccountCode);
                            var level = GetAccountLevel(conn, tx, parentCode);
                            if (parentCode != null) level++;
                            const string sql = @"
IF EXISTS (SELECT 1 FROM dbo.GL_Accounts WHERE AccountCode = @AccountCode)
    UPDATE dbo.GL_Accounts SET AccountNameAr=@AccountNameAr, AccountNameEn=@AccountNameEn,
        ParentAccountCode=@ParentAccountCode, AccountLevel=@AccountLevel, AccountType=@AccountType,
        IsPostable=@IsPostable, ReportType=@ReportType, AccountGroup=@AccountGroup,
        AccountNature=@AccountNature, CloseType=@CloseType, AccountAnalysis=@AccountAnalysis,
        CurrencyCode=@CurrencyCode, IsActive=@IsActive, UpdatedAt=SYSUTCDATETIME()
    WHERE AccountCode=@AccountCode;
ELSE
    INSERT INTO dbo.GL_Accounts
      (AccountCode, AccountNameAr, AccountNameEn, ParentAccountCode, AccountLevel, AccountType,
       IsPostable, ReportType, AccountGroup, AccountNature, CloseType, AccountAnalysis,
       CurrencyCode, IsActive, CreatedAt)
    VALUES
      (@AccountCode, @AccountNameAr, @AccountNameEn, @ParentAccountCode, @AccountLevel, @AccountType,
       @IsPostable, @ReportType, @AccountGroup, @AccountNature, @CloseType, @AccountAnalysis,
       @CurrencyCode, @IsActive, SYSUTCDATETIME());";
                            using (var cmd = new SqlCommand(sql, conn, tx))
                            {
                                AddAccountParameters(cmd, account, parentCode, level);
                                cmd.ExecuteNonQuery();
                                count++;
                            }
                        }
                        tx.Commit();
                        return count;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool AddAccount(AccountModel account)
        {
            return ImportAccountsFromList(new List<AccountModel> { account }) == 1;
        }

        public bool DeleteAccount(string accountCode)
        {
            if (string.IsNullOrWhiteSpace(accountCode)) return false;
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
IF EXISTS (SELECT 1 FROM dbo.GL_Accounts WHERE ParentAccountCode=@AccountCode)
    SELECT 0;
ELSE IF EXISTS (SELECT 1 FROM dbo.GL_VoucherDetails d
               INNER JOIN dbo.GL_Accounts a ON a.AccountID=d.AccountID
               WHERE a.AccountCode=@AccountCode)
    SELECT 0;
ELSE
BEGIN
    DELETE FROM dbo.GL_Accounts WHERE AccountCode=@AccountCode;
    SELECT @@ROWCOUNT;
END", conn))
            {
                cmd.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = accountCode.Trim();
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
            }
        }

        public List<AccountModel> GetAllAccounts()
        {
            var list = new List<AccountModel>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"SELECT AccountID, AccountCode, AccountNameAr, AccountNameEn,
                ParentAccountCode, AccountLevel, AccountType, IsPostable, ReportType, AccountGroup,
                AccountNature, CloseType, AccountAnalysis, CurrencyCode, IsActive
                FROM dbo.GL_Accounts ORDER BY AccountCode", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read()) list.Add(MapAccount(reader));
            }
            return list;
        }

        public List<AccountTreeNode> GetAccountTree()
        {
            var all = GetAllAccounts();
            var nodes = new Dictionary<string, AccountTreeNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var account in all)
                nodes[account.AccountCode] = new AccountTreeNode { Code = account.AccountCode, Name = account.AccountNameAr, Account = account };

            var roots = new List<AccountTreeNode>();
            foreach (var account in all)
            {
                var node = nodes[account.AccountCode];
                var parentCode = NormalizeParent(account.ParentAccountCode);
                AccountTreeNode parent;
                if (parentCode != null && nodes.TryGetValue(parentCode, out parent) &&
                    !string.Equals(parentCode, account.AccountCode, StringComparison.OrdinalIgnoreCase))
                    parent.Children.Add(node);
                else
                    roots.Add(node);
            }
            return roots;
        }

        private static void ValidateAccount(AccountModel account)
        {
            if (account == null || string.IsNullOrWhiteSpace(account.AccountCode)) throw new ArgumentException("رقم الحساب مطلوب.");
            if (string.IsNullOrWhiteSpace(account.AccountNameAr)) throw new ArgumentException("اسم الحساب مطلوب.");
            if (string.Equals(account.AccountCode.Trim(), NormalizeParent(account.ParentAccountCode), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("لا يمكن أن يكون الحساب أباً لنفسه.");
        }

        private static string NormalizeParent(string parentCode)
        {
            if (string.IsNullOrWhiteSpace(parentCode) || parentCode.Trim() == "0") return null;
            return parentCode.Trim();
        }

        private static int GetAccountLevel(SqlConnection conn, SqlTransaction tx, string parentCode)
        {
            if (parentCode == null) return 1;
            using (var cmd = new SqlCommand("SELECT AccountLevel FROM dbo.GL_Accounts WHERE AccountCode=@ParentCode", conn, tx))
            {
                cmd.Parameters.Add("@ParentCode", SqlDbType.NVarChar, 50).Value = parentCode;
                var value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value) throw new InvalidOperationException("الحساب الأب غير موجود: " + parentCode);
                return Convert.ToInt32(value);
            }
        }

        private static void AddAccountParameters(SqlCommand cmd, AccountModel a, string parentCode, int level)
        {
            cmd.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = a.AccountCode.Trim();
            cmd.Parameters.Add("@AccountNameAr", SqlDbType.NVarChar, 200).Value = a.AccountNameAr.Trim();
            cmd.Parameters.Add("@AccountNameEn", SqlDbType.NVarChar, 200).Value = (object)a.AccountNameEn ?? DBNull.Value;
            cmd.Parameters.Add("@ParentAccountCode", SqlDbType.NVarChar, 50).Value = (object)parentCode ?? DBNull.Value;
            cmd.Parameters.Add("@AccountLevel", SqlDbType.Int).Value = level;
            cmd.Parameters.Add("@AccountType", SqlDbType.Int).Value = a.AccountType == 2 ? 2 : 1;
            cmd.Parameters.Add("@IsPostable", SqlDbType.Bit).Value = a.AccountType == 2 || a.IsPostable;
            cmd.Parameters.Add("@ReportType", SqlDbType.Int).Value = a.ReportType;
            cmd.Parameters.Add("@AccountGroup", SqlDbType.Int).Value = a.AccountGroup;
            cmd.Parameters.Add("@AccountNature", SqlDbType.Int).Value = a.AccountNature;
            cmd.Parameters.Add("@CloseType", SqlDbType.Int).Value = a.CloseType;
            cmd.Parameters.Add("@AccountAnalysis", SqlDbType.Int).Value = a.AccountAnalysis;
            cmd.Parameters.Add("@CurrencyCode", SqlDbType.NVarChar, 10).Value = string.IsNullOrWhiteSpace(a.CurrencyCode) ? "YER" : a.CurrencyCode;
            cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = a.IsActive;
        }

        private static AccountModel MapAccount(IDataRecord r)
        {
            return new AccountModel
            {
                AccountID = Convert.ToInt32(r["AccountID"]),
                AccountCode = Convert.ToString(r["AccountCode"]),
                AccountNameAr = Convert.ToString(r["AccountNameAr"]),
                AccountNameEn = r["AccountNameEn"] == DBNull.Value ? null : Convert.ToString(r["AccountNameEn"]),
                ParentAccountCode = r["ParentAccountCode"] == DBNull.Value ? null : Convert.ToString(r["ParentAccountCode"]),
                AccountLevel = Convert.ToInt32(r["AccountLevel"]),
                AccountType = Convert.ToInt32(r["AccountType"]),
                IsPostable = Convert.ToBoolean(r["IsPostable"]),
                ReportType = Convert.ToInt32(r["ReportType"]),
                AccountGroup = Convert.ToInt32(r["AccountGroup"]),
                AccountNature = Convert.ToInt32(r["AccountNature"]),
                CloseType = Convert.ToInt32(r["CloseType"]),
                AccountAnalysis = Convert.ToInt32(r["AccountAnalysis"]),
                CurrencyCode = Convert.ToString(r["CurrencyCode"]),
                IsActive = Convert.ToBoolean(r["IsActive"])
            };
        }
    }
}
