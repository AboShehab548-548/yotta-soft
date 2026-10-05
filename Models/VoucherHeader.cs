using System;
using System.Collections.Generic;

namespace accounting_project.Models
{
    public class VoucherHeader
    {
        public long VoucherID { get; set; }
        public long VoucherNo { get; set; }
        public byte VoucherType { get; set; }
        public DateTime VoucherDate { get; set; }
        public int CompanyID { get; set; }
        public int BranchID { get; set; }
        public int FiscalYearID { get; set; }
        public int FiscalPeriodID { get; set; }
        public string TreasuryAccountCode { get; set; }
        public string TreasuryAccountName { get; set; }
        public byte PaymentType { get; set; }
        public int CurrencyID { get; set; }
        public decimal ExchangeRate { get; set; } = 1m;
        public decimal TotalAmount { get; set; }
        public decimal LocalTotalAmount { get; set; }
        public string SourceType { get; set; }
        public long? SourceID { get; set; }
        public string Notes { get; set; }
        public int CreatedBy { get; set; }
        public bool IsPosted { get; set; }
        public List<VoucherDetail> Details { get; set; } = new List<VoucherDetail>();
    }
}
