using System;

namespace accounting_project.Models
{
    public class ExpiringItemDTO
    {
        public string ItemName { get; set; }
        public string Barcode { get; set; }
        public string BatchNumber { get; set; }
        public int QuantityOnHand { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int DaysRemaining { get; set; }

        // تم التعديل: تفعّل اللون الأحمر فقط إذا كان المتبقي 7 أيام أو أقل
        public bool IsExpiringSoon => DaysRemaining <= 7;
    }
}