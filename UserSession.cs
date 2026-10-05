using System;

namespace AccountingSystem.UI
{
    /// <summary>
    /// سياق تشغيل بسيط مطابق لأبعاد النظام في المرجع: شركة، فرع، سنة، فترة وعملة.
    /// </summary>
    public static class UserSession
    {
        public static int CurrentUserId { get; set; } = 1;
        public static string CurrentUsername { get; set; } = "المستخدم";
        public static int CurrentRoleId { get; set; } = 1;
        public static int CurrentCompanyId { get; set; } = 1;
        public static int CurrentBranchId { get; set; } = 1;
        public static int CurrentFiscalYearId { get; set; } = 1;
        public static int CurrentFiscalPeriodId { get; set; } = 1;
        public static int CurrentCurrencyId { get; set; } = 1;
        public static string CurrentCurrencyCode { get; set; } = "YER";
        public static DateTime SessionStartedAt { get; } = DateTime.Now;

        public static void Clear()
        {
            CurrentUserId = 0;
            CurrentUsername = null;
            CurrentRoleId = 0;
            CurrentCompanyId = 0;
            CurrentBranchId = 0;
            CurrentFiscalYearId = 0;
            CurrentFiscalPeriodId = 0;
            CurrentCurrencyId = 0;
            CurrentCurrencyCode = "YER";
        }
    }
}
