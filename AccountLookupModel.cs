namespace accounting_project.Models
{
    public class AccountLookupModel
    {
        // خاصية للاحتفاظ بالحساب المحدد
        public AccountModel SelectedAccount { get; private set; }
        public string AccountCode { get; set; }
        public string AccountNameAr { get; set; }
        public string DisplayName => $"{AccountCode} - {AccountNameAr}";
    }

}

