using System;
using System.Windows;
using System.Windows.Controls;
using AccountingSystem.Services;
using accounting_project.Infrastructure;

namespace AccountingSystem.UI
{
    public partial class DashboardControl : UserControl
    {
        private readonly DashboardService _dashboardService;

        public DashboardControl()
        {
            InitializeComponent();

            // استبدل بـ ConnectionString الخاص بمشروعك
            string connStr = DbConnectionFactory.ConnectionString;
            _dashboardService = new DashboardService(connStr);

            this.Loaded += DashboardControl_Loaded;
        }

        private void DashboardControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDashboardData();
        }

        public void LoadDashboardData()
        {
            try
            {
                // 1. تحديث الإحصائيات والأرقام
                var summary = _dashboardService.GetDashboardSummary();
                lblTodaySales.Text = $"{summary.TodaySales:N2} YER";
                lblMonthlySales.Text = $"{summary.MonthlySales:N2} YER";

                // 2. تعبئة جدول الأصناف الأكثر مبيعاً
                dgTopSelling.ItemsSource = _dashboardService.GetTopSellingItems();

                // 3. تعبئة جدول نواقص المخزون
                var lowStock = _dashboardService.GetLowStockItems();
                dgLowStock.ItemsSource = lowStock;
                lblLowStockCount.Text = $"{lowStock.Count} أصناف";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء تحميل بيانات اللوحة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}