using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using accounting_project.Models;

namespace accounting_project
{
    public partial class AccountLookupWindow : Window
    {
        private List<AccountModel> _allSubAccounts = new List<AccountModel>();
        public AccountModel SelectedAccount { get; private set; }

        public AccountLookupWindow() : this(new List<AccountModel>())
        {
        }

        public AccountLookupWindow(List<AccountModel> accounts)
        {
            InitializeComponent();

            if (accounts != null && accounts.Any())
            {
                var filtered = accounts
                    .Where(a => a != null && a.AccountType == 2 && (a.AccountStatus == 1 || a.AccountStatus == null))
                    .ToList();

                _allSubAccounts = filtered.Any() ? filtered : accounts;
            }

            DgAccounts.ItemsSource = _allSubAccounts;

            if (_allSubAccounts.Any())
                DgAccounts.SelectedIndex = 0;

            TxtSearch.Focus();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = TxtSearch.Text?.Trim().ToLower() ?? string.Empty;
            if (string.IsNullOrEmpty(search))
            {
                DgAccounts.ItemsSource = _allSubAccounts;
            }
            else
            {
                DgAccounts.ItemsSource = _allSubAccounts.Where(a =>
                    a != null && (
                        (!string.IsNullOrEmpty(a.AccountCode) && a.AccountCode.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(a.AccountNameAr) && a.AccountNameAr.ToLower().Contains(search))
                    )
                ).ToList();
            }

            if (DgAccounts.Items.Count > 0)
                DgAccounts.SelectedIndex = 0;
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                DgAccounts.Focus();
            }
            else if (e.Key == Key.Enter)
            {
                ConfirmSelection();
            }
        }

        private void DgAccounts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ConfirmSelection();
                e.Handled = true;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void ConfirmSelection()
        {
            if (DgAccounts.SelectedItem is AccountModel acc)
            {
                SelectedAccount = acc;
                DialogResult = true;
                Close();
            }
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e) => ConfirmSelection();

        private void DgAccounts_MouseDoubleClick(object sender, MouseButtonEventArgs e) => ConfirmSelection();

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}