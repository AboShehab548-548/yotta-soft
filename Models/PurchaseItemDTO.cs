using System;

namespace accounting_project.Models
{
    public class PurchaseItemDTO
    {
        public int ItemID { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string BatchNumber { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}