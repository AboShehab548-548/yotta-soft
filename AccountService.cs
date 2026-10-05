using System.Collections.Generic;
using System.Linq;

using accounting_project.Models;

namespace accounting_project.Services
{
    public class AccountService
    {
        // استعلام جلب الحسابات الفرعية المتاحة للتقييد فقط
        public List<AccountModel> GetPostingAccounts(List<AccountModel> allAccounts)
        {
            return allAccounts
                .Where(a => a.AccountType == 2) // جلب الحسابات الفرعية فقط
                .OrderBy(a => a.AccountNo)
                .ToList();
        }
    }
}