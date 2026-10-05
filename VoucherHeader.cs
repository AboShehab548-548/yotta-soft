using System;
using System.Collections.Generic;

namespace accounting_project.Models
{
    public class VoucherHeader
    {
        public string TreasuryAccountName { get; set; }
        public long VoucherID { get; set; }
        public long VoucherNo { get; set; }
        public byte VoucherType { get; set; } // 1: قبض, 2: صرف
        public DateTime VoucherDate { get; set; }
        public int FiscalYearID { get; set; }
        public string TreasuryAccountCode { get; set; }
        public byte PaymentType { get; set; }
        public int CurrencyID { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal LocalTotalAmount { get; set; }
        public string Notes { get; set; }
        public int CreatedBy { get; set; }

        public List<VoucherDetail> Details { get; set; } = new List<VoucherDetail>();
    }
}