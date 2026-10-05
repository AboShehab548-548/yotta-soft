using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using ExcelDataReader;
using Microsoft.Win32;
using accounting_project.Models;
using accounting_project.Repositories;

namespace accounting_project
{
    public partial class ImportAccountsWindow : Window
    {
        private string selectedExcelFile = string.Empty;

        public ImportAccountsWindow() { InitializeComponent(); }

        private void BtnBrowseExcelApp_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Excel Executable|EXCEL.EXE|All Files|*.*", Title = "تحديد مسار برنامج الإكسل" };
            if (dialog.ShowDialog() == true) txtExcelPath.Text = dialog.FileName;
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Excel Files|*.xls;*.xlsx;*.xlsm|All Files|*.*", Title = "اختيار ملف الدليل المحاسبي" };
            if (dialog.ShowDialog() == true) { selectedExcelFile = dialog.FileName; txtFilePath.Text = selectedExcelFile; PreviewExcelData(selectedExcelFile); }
        }

        private void PreviewExcelData(string filePath)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet(new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true } });
                    dgPreview.ItemsSource = result.Tables[0].DefaultView;
                }
            }
            catch (Exception ex) { MessageBox.Show("خطأ في قراءة ملف الإكسل: " + ex.Message); }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var view = dgPreview.ItemsSource as DataView;
            if (string.IsNullOrWhiteSpace(selectedExcelFile) || !File.Exists(selectedExcelFile) || view == null)
            { MessageBox.Show("اختر ملف الإكسل واعرض بياناته أولاً."); return; }
            try
            {
                var accounts = new List<AccountModel>();
                foreach (DataRow row in view.Table.Rows)
                {
                    if (row.Table.Columns.Count == 0 || row[0] == DBNull.Value) continue;
                    var code = Convert.ToString(row[0]).Trim();
                    if (code.Length == 0) continue;
                    var parent = row.Table.Columns.Count > 3 && row[3] != DBNull.Value ? Convert.ToString(row[3]).Trim() : null;
                    if (string.IsNullOrWhiteSpace(parent) || parent == "0" || parent.Equals("NULL", StringComparison.OrdinalIgnoreCase)) parent = null;
                    int level = 1, type = 1;
                    if (row.Table.Columns.Count > 5) int.TryParse(Convert.ToString(row[5]), out level);
                    if (row.Table.Columns.Count > 6) int.TryParse(Convert.ToString(row[6]), out type);
                    accounts.Add(new AccountModel
                    {
                        AccountCode = code,
                        AccountNameAr = row.Table.Columns.Count > 1 ? Convert.ToString(row[1]).Trim() : code,
                        AccountNameEn = row.Table.Columns.Count > 2 && row[2] != DBNull.Value ? Convert.ToString(row[2]).Trim() : null,
                        ParentAccountCode = parent,
                        AccountLevel = level <= 0 ? 1 : level,
                        AccountType = type == 2 ? 2 : 1,
                        IsPostable = type == 2,
                        CurrencyCode = row.Table.Columns.Count > 4 && row[4] != DBNull.Value && !string.IsNullOrWhiteSpace(Convert.ToString(row[4])) ? Convert.ToString(row[4]).Trim() : "YER",
                        AccountGroup = 1,
                        IsActive = true
                    });
                }
                var count = new AccountRepository().ImportAccountsFromList(accounts.OrderBy(a => a.AccountLevel).ThenBy(a => a.AccountCode).ToList());
                MessageBox.Show("تم استيراد " + count + " حساباً مع الحفاظ على علاقة الأب والابن.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                var chart = Owner as ChartOfAccountsWindow;
                if (chart != null) chart.LoadAccountsTree();
                Close();
            }
            catch (Exception ex) { MessageBox.Show("حدث خطأ أثناء استيراد الحسابات: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) { Close(); }
    }
}
