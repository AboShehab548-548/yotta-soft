using System;
using System.Collections.Generic;
using System.Data.SqlClient;

using accounting_project.Models;
using System.Linq;

namespace accounting_project.Repositories
{
    public class AccountRepository
    {
        private readonly string _connectionString;

        public AccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public AccountRepository()
        {
        }

        /// <summary>
        /// استيراد قائمة حسابات من ملف Excel وإدخالها في قاعدة البيانات
        /// </summary>
        public int ImportAccountsFromList(List<AccountModel> accounts)
        {
            int insertedCount = 0;

            // ترتيب الحسابات بحسب المستوى وطول الرقم لتغذية الحسابات الرئيسية قبل الفرعية
            var sortedAccounts = accounts
                .OrderBy(a => a.AccountLevel)
                .ThenBy(a => a.AccountCode)
                .ToList();

            string query = @"
                IF NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountCode = @AccountCode)
                BEGIN
                    INSERT INTO Accounts 
                    (AccountCode, CurrencyCode, AccountNameAr, AccountNameEn, ParentAccountCode, AccountLevel, AccountType, ReportType, AccountGroup, AccountNature, CloseType, AccountAnalysis) 
                    VALUES 
                    (@AccountCode, @CurrencyCode, @AccountNameAr, @AccountNameEn, @ParentAccountCode, @AccountLevel, @AccountType, @ReportType, @AccountGroup, @AccountNature, @CloseType, @AccountAnalysis)
                END";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var acc in sortedAccounts)
                        {
                            using (var command = new SqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@AccountCode", acc.AccountCode);
                                command.Parameters.AddWithValue("@CurrencyCode", string.IsNullOrEmpty(acc.CurrencyCode) ? "YER" : acc.CurrencyCode);
                                command.Parameters.AddWithValue("@AccountNameAr", acc.AccountNameAr);
                                command.Parameters.AddWithValue("@AccountNameEn", (object)acc.AccountNameEn ?? DBNull.Value);
                                command.Parameters.AddWithValue("@ParentAccountCode", string.IsNullOrEmpty(acc.ParentAccountCode) ? "0" : acc.ParentAccountCode);
                                command.Parameters.AddWithValue("@AccountLevel", acc.AccountLevel > 0 ? acc.AccountLevel : 1);
                                command.Parameters.AddWithValue("@AccountType", acc.AccountType);
                                command.Parameters.AddWithValue("@ReportType", acc.ReportType > 0 ? acc.ReportType : 1);
                                command.Parameters.AddWithValue("@AccountGroup", acc.AccountGroup > 0 ? acc.AccountGroup : 1);
                                command.Parameters.AddWithValue("@AccountNature", acc.AccountNature);
                                command.Parameters.AddWithValue("@CloseType", acc.CloseType);
                                command.Parameters.AddWithValue("@AccountAnalysis", acc.AccountAnalysis);

                                int rows = command.ExecuteNonQuery();
                                if (rows > 0) insertedCount++;
                            }
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

            return insertedCount;
        }

        /// <summary>
        /// حذف حساب من قاعدة البيانات بشرط عدم وجود حسابات فرعية مرقومة تحته
        /// </summary>
        public bool DeleteAccount(string accountCode)
        {
            string query = @"
        -- التحقق من عدم وجود حسابات فرعية قبل الحذف
        IF NOT EXISTS (SELECT 1 FROM Accounts WHERE ParentAccountCode = @AccountCode)
        BEGIN
            DELETE FROM Accounts WHERE AccountCode = @AccountCode;
        END";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }

        public bool AddAccount(AccountModel account)
        {
            string query = @"
                INSERT INTO Accounts 
                (AccountCode, CurrencyCode, AccountNameAr, AccountNameEn, ParentAccountCode, AccountLevel, AccountType, ReportType, AccountGroup, AccountNature, CloseType, AccountAnalysis) 
                VALUES 
                (@AccountCode, @CurrencyCode, @AccountNameAr, @AccountNameEn, @ParentAccountCode, @AccountLevel, @AccountType, @ReportType, @AccountGroup, @AccountNature, @CloseType, @AccountAnalysis)";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@AccountCode", account.AccountCode);
                command.Parameters.AddWithValue("@CurrencyCode", account.CurrencyCode ?? "YER");
                command.Parameters.AddWithValue("@AccountNameAr", account.AccountNameAr);
                command.Parameters.AddWithValue("@AccountNameEn", (object)account.AccountNameEn ?? DBNull.Value);
                command.Parameters.AddWithValue("@ParentAccountCode", account.ParentAccountCode ?? "0");
                command.Parameters.AddWithValue("@AccountLevel", account.AccountLevel);
                command.Parameters.AddWithValue("@AccountType", account.AccountType);
                command.Parameters.AddWithValue("@ReportType", account.ReportType);
                command.Parameters.AddWithValue("@AccountGroup", account.AccountGroup);
                command.Parameters.AddWithValue("@AccountNature", account.AccountNature);
                command.Parameters.AddWithValue("@CloseType", account.CloseType);
                command.Parameters.AddWithValue("@AccountAnalysis", account.AccountAnalysis);

                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// جلب كافة الحسابات مسطحة لشاشات البحث (F9) وجداول القيود
        /// </summary>
        public List<AccountModel> GetAllAccounts()
        {
            var accounts = new List<AccountModel>();
            string query = @"SELECT AccountCode, CurrencyCode, AccountNameAr, AccountNameEn, 
                                    ParentAccountCode, AccountLevel, AccountType, ReportType, 
                                    AccountGroup, AccountNature, CloseType, AccountAnalysis 
                             FROM Accounts 
                             ORDER BY AccountCode";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        accounts.Add(new AccountModel
                        {
                            AccountCode = reader["AccountCode"].ToString(),
                            CurrencyCode = reader["CurrencyCode"].ToString(),
                            AccountNameAr = reader["AccountNameAr"].ToString(),
                            AccountNameEn = reader["AccountNameEn"] != DBNull.Value ? reader["AccountNameEn"].ToString() : string.Empty,
                            ParentAccountCode = reader["ParentAccountCode"].ToString(),
                            AccountLevel = Convert.ToInt32(reader["AccountLevel"]),
                            AccountType = Convert.ToInt32(reader["AccountType"]),
                            ReportType = Convert.ToInt32(reader["ReportType"]),
                            AccountGroup = Convert.ToInt32(reader["AccountGroup"]),
                            AccountNature = Convert.ToInt32(reader["AccountNature"]),
                            CloseType = Convert.ToInt32(reader["CloseType"]),
                            AccountAnalysis = Convert.ToInt32(reader["AccountAnalysis"])
                        });
                    }
                }
            }
            return accounts;
        }

        /// <summary>
        /// تحويل القائمة المسطحة إلى شجرة هرمية لشاشات عرض TreeView
        /// </summary>
        public List<AccountTreeNode> GetAccountTree()
        {
            var allAccounts = GetAllAccounts();
            return BuildTreeRecursive(allAccounts, "0");
        }

        private List<AccountTreeNode> BuildTreeRecursive(List<AccountModel> flatList, string parentCode)
        {
            return flatList
                .Where(a => a.ParentAccountCode == parentCode)
                .Select(a => new AccountTreeNode
                {
                    Code = a.AccountCode,
                    Name = a.AccountNameAr,
                    Account = a,
                    Children = BuildTreeRecursive(flatList, a.AccountCode)
                })
                .ToList();
        }
    }
}