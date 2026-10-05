using System;

namespace accounting_project.Models
{
    /// <summary>
    /// نموذج الحساب في الدليل الموحد GL_Accounts.
    /// الحساب الرئيسي للتجميع، والحساب الفرعي فقط قابل للتأثر بالعمليات.
    /// </summary>
    public class AccountModel
    {
        public int AccountID { get; set; }
        public string AccountCode { get; set; }
        public string CurrencyCode { get; set; } = "YER";
        public string AccountNameAr { get; set; }
        public int? AccountStatus { get; set; }
        public string AccountNameEn { get; set; }
        public string ParentAccountCode { get; set; }
        public int AccountLevel { get; set; }
        public int AccountType { get; set; } // 1: رئيسي، 2: فرعي
        public int ReportType { get; set; }
        public int AccountGroup { get; set; }
        public int AccountNature { get; set; }
        public int CloseType { get; set; }
        public int AccountAnalysis { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsPostable { get; set; }

        public string AccountNo
        {
            get => AccountCode;
            set => AccountCode = value;
        }

        public string AccountName
        {
            get => AccountNameAr;
            set => AccountNameAr = value;
        }

        public string Currency
        {
            get => CurrencyCode;
            set => CurrencyCode = value;
        }

        public string DisplayName => $"{AccountCode} - {AccountNameAr}";
    }
}
