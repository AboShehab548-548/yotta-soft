using System;

namespace accounting_project.Models
{
    public class ExpiringItemDTO
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Barcode { get; set; }
        public string BatchNumber { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal CostPrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsExpiringSoon => DaysRemaining <= 7;
    }
}
