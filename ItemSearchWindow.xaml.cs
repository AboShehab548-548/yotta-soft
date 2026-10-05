using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AccountingSystem.UI
{
    public partial class ItemSearchWindow : Window
    {
        // قائمة بحفظ جميع الأصناف المحددة (دعم التحديد المتعدد)
        public List<CartItemDto> SelectedItems { get; set; } = new List<CartItemDto>();

        // عنصر واحد للتوافق في حال الاعتماد على التحديد المفرد
        public CartItemDto SelectedItem { get; private set; }

        private readonly string _connectionString;

        // 1. المشيد الافتراضي بدون برامترات (ضروري جداً لمُعالج XAML لتفادي الخطأ)
        public ItemSearchWindow()
        {
            InitializeComponent();
        }

        // 2. المشيد الرئيسي الذي يستقبل نص الاتصال ويرتبط بالمشيد الافتراضي
        public ItemSearchWindow(string connectionString) : this()
        {
            _connectionString = connectionString;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtSearch.Focus();
            LoadItems("");
        }

        // دالة تأكيد الاختيار الموحدة (تعمل مع التحديد المفرد والمتعدد)
        private void ConfirmSelection()
        {
            SelectedItems.Clear();

            if (dgItems.SelectedItems != null && dgItems.SelectedItems.Count > 0)
            {
                foreach (var selectedObj in dgItems.SelectedItems)
                {
                    if (selectedObj is DataRowView row)
                    {
                        var dto = new CartItemDto
                        {
                            ItemID = Convert.ToInt32(row["ItemID"]),
                            Barcode = row["Barcode"] != DBNull.Value ? row["Barcode"].ToString() : "",
                            ItemName = row["ItemName"] != DBNull.Value ? row["ItemName"].ToString() : "",
                            UnitName = row["UnitName"] != DBNull.Value ? row["UnitName"].ToString() : "",
                            UnitPrice = row["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(row["UnitPrice"]) : 0,
                            CostPrice = row["CostPrice"] != DBNull.Value ? Convert.ToDecimal(row["CostPrice"]) : 0,
                            Quantity = 1,
                            Total = row["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(row["UnitPrice"]) : 0
                        };

                        SelectedItems.Add(dto);
                    }
                }
            }

            if (SelectedItems.Count > 0)
            {
                SelectedItem = SelectedItems.First(); // تعبئة العنصر المفرد للتوافق
                DialogResult = true; // تعيين القيمة يغلق النافذة المنبثقة تلقائياً
            }
            else
            {
                MessageBox.Show("يرجى اختيار صنف واحد على الأقل من القائمة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // عند الضغط على زر الاختيار / إضافة
        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            ConfirmSelection();
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            ConfirmSelection();
        }

        // عند الضغط مرتين بالماوس على صنف معين
        private void dgItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConfirmSelection();
        }

        // جلب الأصناف من SQL Server مع الفلترة السريعة
        private void LoadItems(string searchText)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = @"
                        SELECT TOP 50 ItemID, Barcode, ItemName, UnitName, CostPrice, UnitPrice, StockQuantity 
                        FROM dbo.Items 
                        WHERE IsActive = 1 AND (ItemName LIKE @Search OR Barcode LIKE @Search OR ItemCode LIKE @Search)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Search", "%" + searchText.Trim() + "%");

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    dgItems.ItemsSource = dt.DefaultView;

                    if (dgItems.Items.Count > 0)
                    {
                        dgItems.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء جلب الأصناف: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadItems(txtSearch.Text);
        }

        // التنقل بالأسهم داخل الجدول من حقل النص مباشرة
        private void txtSearch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (dgItems.SelectedIndex < dgItems.Items.Count - 1)
                {
                    dgItems.SelectedIndex++;
                    dgItems.ScrollIntoView(dgItems.SelectedItem);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (dgItems.SelectedIndex > 0)
                {
                    dgItems.SelectedIndex--;
                    dgItems.ScrollIntoView(dgItems.SelectedItem);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                ConfirmSelection();
                e.Handled = true;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
        }
    }
}