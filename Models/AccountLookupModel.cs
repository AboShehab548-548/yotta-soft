namespace accounting_project.Models
{
    public class AccountLookupModel
    {
        public AccountModel SelectedAccount { get; set; }
        public string AccountCode { get; set; }
        public string AccountNameAr { get; set; }
        public bool IsPostable { get; set; }
        public string DisplayName => $"{AccountCode} - {AccountNameAr}";
    }
}
