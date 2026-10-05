using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace AccountingSystem.Services
{
    public class DashboardSummaryDto
    {
        public decimal TodaySales { get; set; }
        public decimal MonthlySales { get; set; }
        public int TodayInvoicesCount { get; set; }
    }

    public class TopSellingItemDto
    {
        public string ItemName { get; set; }
        public decimal TotalQtySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class LowStockItemDto
    {
        public string ItemCode { get; set; }
        public string Barcode { get; set; }
        public string ItemName { get; set; }
        public decimal StockQuantity { get; set; }
        public string UnitName { get; set; }
    }

    public class DashboardService
    {
        private readonly string _connectionString;

        public DashboardService(string connectionString)
        {
            _connectionString = connectionString;
        }

        // 1. جلب المبيعات اليومية والشهرية
        public DashboardSummaryDto GetDashboardSummary()
        {
            var summary = new DashboardSummaryDto();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT 
                        ISNULL(SUM(CASE WHEN CAST(InvoiceDate AS DATE) = CAST(GETDATE() AS DATE) THEN NetAmount ELSE 0 END), 0) AS TodaySales,
                        ISNULL(SUM(CASE WHEN MONTH(InvoiceDate) = MONTH(GETDATE()) AND YEAR(InvoiceDate) = YEAR(GETDATE()) THEN NetAmount ELSE 0 END), 0) AS MonthlySales,
                        ISNULL(COUNT(CASE WHEN CAST(InvoiceDate AS DATE) = CAST(GETDATE() AS DATE) THEN 1 END), 0) AS TodayInvoicesCount
                    FROM SalesInvoices;";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        summary.TodaySales = Convert.ToDecimal(reader["TodaySales"]);
                        summary.MonthlySales = Convert.ToDecimal(reader["MonthlySales"]);
                        summary.TodayInvoicesCount = Convert.ToInt32(reader["TodayInvoicesCount"]);
                    }
                }
            }
            return summary;
        }

        // 2. جلب الأصناف الأكثر مبيعاً
        public List<TopSellingItemDto> GetTopSellingItems()
        {
            var list = new List<TopSellingItemDto>();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT TOP 5 
                        i.ItemName, 
                        SUM(d.Quantity) AS TotalQtySold, 
                        SUM(d.TotalPrice) AS TotalRevenue
                    FROM SalesInvoiceDetails d
                    INNER JOIN Items i ON d.ItemID = i.ItemID
                    GROUP BY i.ItemName
                    ORDER BY TotalQtySold DESC;";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new TopSellingItemDto
                        {
                            ItemName = reader["ItemName"].ToString(),
                            TotalQtySold = Convert.ToDecimal(reader["TotalQtySold"]),
                            TotalRevenue = Convert.ToDecimal(reader["TotalRevenue"])
                        });
                    }
                }
            }
            return list;
        }

        // 3. جلب نواقص المخزون
        public List<LowStockItemDto> GetLowStockItems()
        {
            var list = new List<LowStockItemDto>();

            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT ItemCode, Barcode, ItemName, StockQuantity, UnitName
                    FROM Items
                    WHERE StockQuantity <= 5 AND IsActive = 1
                    ORDER BY StockQuantity ASC;";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new LowStockItemDto
                        {
                            ItemCode = reader["ItemCode"].ToString(),
                            Barcode = reader["Barcode"]?.ToString(),
                            ItemName = reader["ItemName"].ToString(),
                            StockQuantity = Convert.ToDecimal(reader["StockQuantity"]),
                            UnitName = reader["UnitName"]?.ToString()
                        });
                    }
                }
            }
            return list;
        }
    }
}