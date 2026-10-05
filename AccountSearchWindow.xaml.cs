using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using accounting_project.Models;

namespace accounting_project.Views
{
    public partial class AccountSearchWindow : Window
    {
        private readonly List<AccountLookupModel> _allAccounts;

        public List<AccountLookupModel> SelectedAccounts { get; private set; } = new List<AccountLookupModel>();

        public AccountLookupModel SelectedAccount => SelectedAccounts.FirstOrDefault();

        public AccountSearchWindow(List<AccountLookupModel> accounts)
        {
            InitializeComponent();
            _allAccounts = accounts ?? new List<AccountLookupModel>();
            dgAccounts.ItemsSource = _allAccounts;
            txtSearch.Focus();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = txtSearch.Text?.Trim().ToLower() ?? string.Empty;
            if (string.IsNullOrEmpty(keyword))
            {
                dgAccounts.ItemsSource = _allAccounts;
            }
            else
            {
                dgAccounts.ItemsSource = _allAccounts.Where(a =>
                    a != null && (
                        (a.AccountCode != null && a.AccountCode.ToLower().Contains(keyword)) ||
                        (a.AccountNameAr != null && a.AccountNameAr.ToLower().Contains(keyword))
                    )).ToList();
            }
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            SelectCurrent();
        }

        private void DgAccounts_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectCurrent();
        }

        private void SelectCurrent()
        {
            if (dgAccounts.SelectedItems != null && dgAccounts.SelectedItems.Count > 0)
            {
                SelectedAccounts = dgAccounts.SelectedItems.Cast<AccountLookupModel>().ToList();
                DialogResult = true;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}