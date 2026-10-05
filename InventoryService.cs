using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using accounting_project.Models;

namespace accounting_project.Services
{
    public class InventoryService
    {
        private readonly string _connectionString;

        public InventoryService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<ExpiringItemDTO> GetExpiringItems(int daysThreshold = 30)
        {
            var expiringList = new List<ExpiringItemDTO>();

            // تم إلغاء شرط التحديد بـ 30 يوماً لجلب جميع الدفعات المتوفرة
            string query = @"
        SELECT 
            i.ItemName,
            i.Barcode,
            b.BatchNumber,
            b.CurrentQuantity AS QuantityOnHand, 
            b.ExpiryDate,
            DATEDIFF(day, GETDATE(), b.ExpiryDate) AS DaysRemaining
        FROM ItemBatches b
        INNER JOIN Items i ON b.ItemID = i.ItemID
        WHERE b.CurrentQuantity > 0 
        ORDER BY b.ExpiryDate ASC;";

            using (SqlConnection conn = new SqlConnection(@"Data Source=DESKTOP-TE00DUB\SQLEXPRESS;Initial Catalog=AccountingDB;Integrated Security=True"))
            {
                SqlCommand cmd = new SqlCommand(query, conn);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        expiringList.Add(new ExpiringItemDTO
                        {
                            ItemName = reader["ItemName"].ToString(),
                            Barcode = reader["Barcode"] != DBNull.Value ? reader["Barcode"].ToString() : "",
                            BatchNumber = reader["BatchNumber"].ToString(),
                            QuantityOnHand = Convert.ToInt32(reader["QuantityOnHand"]),
                            ExpiryDate = Convert.ToDateTime(reader["ExpiryDate"]),
                            DaysRemaining = Convert.ToInt32(reader["DaysRemaining"])
                        });
                    }
                }
            }

            return expiringList;
        }
    }
}