using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using accounting_project.Infrastructure;
using accounting_project.Models;

namespace accounting_project.Services
{
    public class InventoryService
    {
        private readonly string _connectionString;

        public InventoryService(string connectionString = null)
        {
            _connectionString = string.IsNullOrWhiteSpace(connectionString)
                ? DbConnectionFactory.ConnectionString
                : connectionString;
        }

        public List<ExpiringItemDTO> GetExpiringItems(int daysThreshold = 30, int branchId = 1)
        {
            if (daysThreshold < 0) throw new ArgumentOutOfRangeException("daysThreshold");
            var list = new List<ExpiringItemDTO>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"SELECT i.ItemCode, i.ItemName, i.Barcode, b.BatchNo,
                    b.ExpiryDate, b.CurrentQuantity QuantityOnHand, b.CostPrice,
                    DATEDIFF(day, CONVERT(date,SYSUTCDATETIME()), b.ExpiryDate) DaysRemaining
                FROM dbo.ItemBatches b
                INNER JOIN dbo.Items i ON i.ItemID=b.ItemID
                WHERE b.BranchID=@BranchID AND b.CurrentQuantity>0
                  AND b.ExpiryDate IS NOT NULL
                  AND b.ExpiryDate <= DATEADD(DAY,@DaysThreshold,CONVERT(date,SYSUTCDATETIME()))
                ORDER BY b.ExpiryDate, i.ItemCode", conn))
            {
                cmd.Parameters.AddWithValue("@BranchID", branchId);
                cmd.Parameters.AddWithValue("@DaysThreshold", daysThreshold);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new ExpiringItemDTO
                        {
                            ItemCode = Convert.ToString(reader["ItemCode"]),
                            ItemName = Convert.ToString(reader["ItemName"]),
                            Barcode = reader["Barcode"] == DBNull.Value ? null : Convert.ToString(reader["Barcode"]),
                            BatchNumber = reader["BatchNo"] == DBNull.Value ? null : Convert.ToString(reader["BatchNo"]),
                            ExpiryDate = reader["ExpiryDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ExpiryDate"]),
                            QuantityOnHand = Convert.ToDecimal(reader["QuantityOnHand"]),
                            CostPrice = Convert.ToDecimal(reader["CostPrice"]),
                            DaysRemaining = Convert.ToInt32(reader["DaysRemaining"])
                        });
                    }
                }
            }
            return list;
        }
    }
}
