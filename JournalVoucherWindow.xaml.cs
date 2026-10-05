using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using accounting_project.ViewModels;
using accounting_project.Models;


namespace accounting_project
{
    public partial class JournalVoucherWindow : Window
    {
   
        public JournalVoucherWindow()
        {
            InitializeComponent();
            DataContext = new VoucherViewModel();
        }

        // معالج حدث اختصار F9 داخل DataGrid
        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F9)
            {
                if (sender is DataGrid grid && grid.CurrentColumn != null && grid.CurrentCell.Column.DisplayIndex == 0)
                {
                    e.Handled = true;
                   
                }
            }
        }

     

      
    }
}