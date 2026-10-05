using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using accounting_project.Infrastructure;

namespace AccountingSystem.UI
{
    public partial class CloseShiftWindow : Window
    {
        private readonly string _connectionString = DbConnectionFactory.ConnectionString;

        public int ShiftId { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalCashSales { get; set; }
        public decimal TotalCardSales { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal ExpectedCash { get; set; }
        public bool IsShiftClosedSuccessfully { get; private set; } = false;

        public CloseShiftWindow()
        {
            InitializeComponent();
        }

        public CloseShiftWindow(int shiftId, string cashierName, DateTime startTime, decimal openingBalance, decimal cashSales, decimal cardSales, decimal returns = 0) : this()
        {
            ShiftId = shiftId;
            OpeningBalance = openingBalance;
            TotalCashSales = cashSales;
            TotalCardSales = cardSales;

            // جلب المرتجعات تلقائياً من قاعدة البيانات إذا لم يتم إرسالها
            TotalReturns = returns > 0 ? returns : GetTotalReturnsForShift(shiftId);

            ExpectedCash = (OpeningBalance + TotalCashSales) - TotalReturns;

            txtCashierName.Text = cashierName;
            txtStartTime.Text = startTime.ToString("yyyy/MM/dd hh:mm tt");
            txtOpeningBalance.Text = OpeningBalance.ToString("N2");
            txtCashSales.Text = TotalCashSales.ToString("N2");
            txtCardSales.Text = TotalCardSales.ToString("N2");
            txtReturns.Text = TotalReturns.ToString("N2");
            txtExpectedCash.Text = ExpectedCash.ToString("N2");

            txtActualCash.Focus();
        }

        // دالة خاصة ومحمية لحساب المرتجعات من قاعدة البيانات
        private decimal GetTotalReturnsForShift(int shiftId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string returnQuery = "SELECT ISNULL(SUM(r.TotalAmount), 0) FROM dbo.SalesReturns r INNER JOIN dbo.SalesInvoices i ON i.SalesInvoiceID = r.SalesInvoiceID WHERE i.ShiftID = @ShiftID";
                    SqlCommand cmdReturn = new SqlCommand(returnQuery, conn);
                    cmdReturn.Parameters.AddWithValue("@ShiftID", shiftId);
                    return Convert.ToDecimal(cmdReturn.ExecuteScalar() ?? 0);
                }
            }
            catch
            {
                // إذا لم يتم إنشاء جدول المرتجعات بعد، يتم إرجاع 0 دون إيقاف البرنامج
                return 0;
            }
        }

        private void TxtActualCash_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TryParseDecimal(txtActualCash.Text, out decimal actualCash))
            {
                decimal diff = actualCash - ExpectedCash;
                txtDifference.Text = diff.ToString("N2");

                if (diff < 0)
                {
                    txtDifference.Foreground = System.Windows.Media.Brushes.Red;
                }
                else if (diff > 0)
                {
                    txtDifference.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    txtDifference.Foreground = System.Windows.Media.Brushes.Black;
                }
            }
            else
            {
                txtDifference.Text = "0.00";
                txtDifference.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void BtnCloseShift_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseDecimal(txtActualCash.Text, out decimal actualCash))
            {
                MessageBox.Show("يرجى إدخال المبلغ الفعلي الموجود في الدرج بشكل صحيح.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtActualCash.Focus();
                txtActualCash.SelectAll();
                return;
            }

            decimal difference = actualCash - ExpectedCash;

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    string query = @"UPDATE dbo.Shifts 
                                     SET EndTime = GETDATE(), 
CashSales = @CashSales, 
                                         CardSales = @CardSales, 
                                         ReturnsAmount = @Returns, 
                                         ExpectedCash = @ExpectedCash, 
                                         ActualCash = @ActualCash, 
                                         DifferenceAmount = @Difference, 
                                         IsClosed = 1 
                                     WHERE ShiftID = @ShiftID";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@CashSales", TotalCashSales);
                    cmd.Parameters.AddWithValue("@CardSales", TotalCardSales);
                    cmd.Parameters.AddWithValue("@Returns", TotalReturns);
                    cmd.Parameters.AddWithValue("@ExpectedCash", ExpectedCash);
                    cmd.Parameters.AddWithValue("@ActualCash", actualCash);
                    cmd.Parameters.AddWithValue("@Difference", difference);
                    cmd.Parameters.AddWithValue("@ShiftID", ShiftId);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                // طباعة تقرير الوردية Z-Report
                PrintZReport(actualCash, difference);

                MessageBox.Show("تم تقفيل الوردية وطباعة التقرير بنجاح!", "إغلاق الوردية", MessageBoxButton.OK, MessageBoxImage.Information);
                IsShiftClosedSuccessfully = true;
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء حفظ التقفيل في قاعدة البيانات: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #region Print Z-Report Logic

        private void PrintZReport(decimal actualCash, decimal difference)
        {
            try
            {
                PrintDocument pd = new PrintDocument();

                pd.PrintPage += (sender, e) =>
                {
                    Font titleFont = new Font("Arial", 11, System.Drawing.FontStyle.Bold);
                    Font bodyFont = new Font("Arial", 9, System.Drawing.FontStyle.Regular);
                    Font boldFont = new Font("Arial", 9, System.Drawing.FontStyle.Bold);

                    float y = 10;
                    float leftMargin = 5;
                    float width = 280;

                    StringFormat centerFormat = new StringFormat { Alignment = StringAlignment.Center };

                    e.Graphics.DrawString("تقرير إغلاق الوردية (Z-Report)", titleFont, System.Drawing.Brushes.Black, new RectangleF(0, y, width, 20), centerFormat);
                    y += 22;

                    e.Graphics.DrawString("------------------------------------------", bodyFont, System.Drawing.Brushes.Black, leftMargin, y);
                    y += 15;

                    e.Graphics.DrawString($"اسم الكاشير: {txtCashierName.Text}", bodyFont, System.Drawing.Brushes.Black, leftMargin, y);
                    y += 18;
                    e.Graphics.DrawString($"رقم الوردية: {ShiftId}", bodyFont, System.Drawing.Brushes.Black, leftMargin, y);
                    y += 18;
                    e.Graphics.DrawString($"وقت الفتح: {txtStartTime.Text}", bodyFont, System.Drawing.Brushes.Black, leftMargin, y);
                    y += 18;
                    e.Graphics.DrawString($"وقت الإغلاق: {DateTime.Now:yyyy/MM/dd hh:mm tt}", bodyFont, System.Drawing.Brushes.Black, leftMargin, y);
                    y += 20;

                    e.Graphics.DrawString("------------------------------------------", bodyFont, System.Drawing.Brushes.Black, leftMargin, y);
                    y += 15;

                    e.Graphics.DrawString($"الرصيد الافتتاحي (الفكّة): {OpeningBalance:N2}", bodyFont, System.Drawing.Brushes.Black, leftMargin, y);
                    y += 18;
                    e.Graphics.DrawString($"إجمالي مبيعات الكاش: {TotalCashSales:N2}", bodyFont, System.Drawing.Brushes.Black, leftMargin, y);
                    y += 18;
                    e.Graphics.DrawString($"إجمالي مبيعات الشبكة: {TotalCardSales:N2}", bodyFont, System.Drawing.Brushes.Black, leftMargin, y);
                    y += 18;
                    e.Graphics.DrawString($"إجمالي المرتجعات: {TotalReturns:N2}", bodyFont, System.Drawing.Brushes.Black, leftMargin, y);
                    y += 18;
                    e.Graphics.DrawString($"المبلغ المتوقع بالدرج: {ExpectedCash:N2}", boldFont, System.Drawing.Brushes.Black, leftMargin, y);
                    y += 20;

                    e.Graphics.DrawString("------------------------------------------", bodyFont, System.Drawing.Brushes.Black, leftMargin, y);
                    y += 15;

                    e.Graphics.DrawString($"المبلغ الفعلي بالدرج: {actualCash:N2}", boldFont, System.Drawing.Brushes.Black, leftMargin, y);
                    y += 18;

                    string diffStatus = difference == 0 ? "متطابق (0.00)" : (difference > 0 ? $"زيادة (+{difference:N2})" : $"عجز ({difference:N2})");
                    e.Graphics.DrawString($"الفرق: {diffStatus}", boldFont, System.Drawing.Brushes.Black, leftMargin, y);
                    y += 25;

                    e.Graphics.DrawString("تم تقفيل الصندوق وتسليمه بنجاح", bodyFont, System.Drawing.Brushes.Black, new RectangleF(0, y, width, 20), centerFormat);
                };

                pd.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show("تم حفظ التقفيل بنجاح، لكن تعذرت الطباعة الحرارية: " + ex.Message, "تنبيه الطباعة", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Helper Methods

        private bool TryParseDecimal(string input, out decimal result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;

            string cleanInput = input.Trim();

            cleanInput = cleanInput.Replace('٠', '0')
                                   .Replace('١', '1')
                                   .Replace('٢', '2')
                                   .Replace('٣', '3')
                                   .Replace('٤', '4')
                                   .Replace('٥', '5')
                                   .Replace('٦', '6')
                                   .Replace('٧', '7')
                                   .Replace('٨', '8')
                                   .Replace('٩', '9');

            cleanInput = cleanInput.Replace('٫', '.').Replace(',', '.');

            if (decimal.TryParse(cleanInput, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return true;

            return decimal.TryParse(cleanInput, NumberStyles.Any, CultureInfo.CurrentCulture, out result);
        }

        #endregion
    }
}