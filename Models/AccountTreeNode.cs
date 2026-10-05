using System.Collections.Generic;

namespace accounting_project.Models
{
    public class AccountTreeNode
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public AccountModel Account { get; set; } = new AccountModel();
        public List<AccountTreeNode> Children { get; set; } = new List<AccountTreeNode>();

        // خصائص الوصول والتهيئة المباشرة لحل خطأ CS1061
        public string AccountNo
        {
            get => Code ?? Account?.AccountCode;
            set
            {
                Code = value;
                if (Account == null) Account = new AccountModel();
                Account.AccountCode = value;
            }
        }

        public string AccountName
        {
            get => Name ?? Account?.AccountNameAr;
            set
            {
                Name = value;
                if (Account == null) Account = new AccountModel();
                Account.AccountNameAr = value;
            }
        }

        public string ParentAccountNo
        {
            get => Account?.ParentAccountCode;
            set
            {
                if (Account == null) Account = new AccountModel();
                Account.ParentAccountCode = value;
            }
        }

        public int AccountType
        {
            get => Account?.AccountType ?? 1;
            set
            {
                if (Account == null) Account = new AccountModel();
                Account.AccountType = value;
            }
        }

        public string Currency
        {
            get => Account?.CurrencyCode ?? "YER";
            set
            {
                if (Account == null) Account = new AccountModel();
                Account.CurrencyCode = value;
            }
        }
    }
}