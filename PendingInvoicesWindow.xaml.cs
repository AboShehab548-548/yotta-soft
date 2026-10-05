using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Windows;
using accounting_project.Infrastructure;


namespace AccountingSystem.UI
{
    public partial class PendingInvoicesWindow : Window
    {
        // 1. إضافة المشيد الافتراضي المتوافق مع XAML
        public PendingInvoicesWindow()
        {
            InitializeComponent();
        }

        private readonly string _connectionString = DbConnectionFactory.ConnectionString;
        public int ShiftID { get; set; }
        public ObservableCollection<PendingInvoiceHeader> PendingList { get; set; } = new ObservableCollection<PendingInvoiceHeader>();
        public PendingInvoiceHeader SelectedPendingInvoice { get; private set; }

        public PendingInvoicesWindow(int shiftId)
        {
            InitializeComponent();
            ShiftID = shiftId;
            dgPendingInvoices.ItemsSource = PendingList;
            LoadPendingInvoices();
        }

        private void LoadPendingInvoices()
        {
            PendingList.Clear();
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    // أُضيف p.CustomerName في الاستعلام ومجموعة الـ GROUP BY
                    string query = @"
                        SELECT p.PendingInvoiceID, p.CustomerName, p.HoldDate, p.TotalAmount, COUNT(d.PendingInvoiceDetailID) AS ItemCount
                        FROM dbo.PendingInvoices p
                        LEFT JOIN dbo.PendingInvoiceDetails d ON p.PendingInvoiceID = d.PendingInvoiceID
                        WHERE p.ShiftID = @ShiftID
                        GROUP BY p.PendingInvoiceID, p.CustomerName, p.HoldDate, p.TotalAmount
                        ORDER BY p.PendingInvoiceID DESC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ShiftID", ShiftID);

                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        PendingList.Add(new PendingInvoiceHeader
                        {
                            PendingInvoiceID = Convert.ToInt64(reader["PendingInvoiceID"]),
                            // قراءة اسم العميل مع التعويض بـ "عميل عام" في حال كانت الخانة فارغة
                            CustomerName = reader["CustomerName"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["CustomerName"].ToString())
                                            ? reader["CustomerName"].ToString()
                                            : "عميل عام",
                            HoldDate = Convert.ToDateTime(reader["HoldDate"]),
                            TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                            ItemCount = Convert.ToInt32(reader["ItemCount"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء تحميل الفواتير المعلقة: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRecall_Click(object sender, RoutedEventArgs e)
        {
            if (dgPendingInvoices.SelectedItem is PendingInvoiceHeader selected)
            {
                SelectedPendingInvoice = selected;
                LoadPendingItems(selected);

                // حذف الفاتورة المعلقة من الجدول بعد اختيارها للاسترجاع
                DeletePendingInvoiceFromDb(selected.PendingInvoiceID);

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("يرجى اختيار فاتورة من القائمة أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadPendingItems(PendingInvoiceHeader header)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT d.ItemID, i.ItemName, d.Quantity, d.UnitPrice
                        FROM dbo.PendingInvoiceDetails d
                        INNER JOIN dbo.Items i ON d.ItemID = i.ItemID
                        WHERE d.PendingInvoiceID = @PendingInvoiceID";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@PendingInvoiceID", header.PendingInvoiceID);

                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        header.Items.Add(new CartItemDto
                        {
                            ItemID = Convert.ToInt32(reader["ItemID"]),
                            ItemName = reader["ItemName"].ToString(),
                            Quantity = Convert.ToDecimal(reader["Quantity"]),
                            UnitPrice = Convert.ToDecimal(reader["UnitPrice"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء جلب تفاصيل الفاتورة المعلقة: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgPendingInvoices.SelectedItem is PendingInvoiceHeader selected)
            {
                if (MessageBox.Show("هل أنت تأكد من حذف هذه الفاتورة المعلقة؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    DeletePendingInvoiceFromDb(selected.PendingInvoiceID);
                    PendingList.Remove(selected);
                }
            }
        }

        private void DeletePendingInvoiceFromDb(long pendingInvoiceId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM dbo.PendingInvoices WHERE PendingInvoiceID = @PendingInvoiceID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@PendingInvoiceID", pendingInvoiceId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء مسح الفاتورة المعلقة: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class PendingInvoiceHeader
    {
        public long PendingInvoiceID { get; set; }
        public string CustomerName { get; set; }
        public DateTime HoldDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
    }
}