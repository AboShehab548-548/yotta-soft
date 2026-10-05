using System;
using System.Linq;
using System.Windows;
using accounting_project.Services;
using accounting_project.Models;

namespace accounting_project
{
    public partial class MainWindow : Window
    {
        private readonly InventoryService _inventoryService = new InventoryService();

        public MainWindow()
        {
            InitializeComponent();
            LoadExpiringItemsNotification();
            CheckExpiredItems();
        }

        private void LoadExpiringItemsNotification()
        {
            try
            {
                var expiringItems = _inventoryService.GetExpiringItems(30);
                if (dgExpiringItems != null) dgExpiringItems.ItemsSource = expiringItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل تنبيهات الصلاحية: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckExpiredItems()
        {
            try
            {
                var expired = _inventoryService.GetExpiringItems(0).Where(x => x.DaysRemaining <= 0).ToList();
                if (expired.Count == 0) return;
                var text = string.Join(Environment.NewLine, expired.Select(x =>
                    string.Format("{0} | دفعة: {1} | الكمية: {2:N2} | تاريخ الانتهاء: {3}",
                        x.ItemName, x.BatchNumber ?? "-", x.QuantityOnHand,
                        x.ExpiryDate.HasValue ? x.ExpiryDate.Value.ToString("yyyy-MM-dd") : "-")));
                MessageBox.Show("تنبيه مهم: توجد (" + expired.Count + ") دفعات منتهية الصلاحية:\n\n" + text,
                    "تنبيه انتهاء الصلاحية", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء فحص تواريخ الصلاحية: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenAddWindow_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddBatchWindow();
            if (addWindow.ShowDialog() == true) LoadExpiringItemsNotification();
        }
    }
}
