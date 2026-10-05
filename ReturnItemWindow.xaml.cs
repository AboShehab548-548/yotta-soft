using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AccountingSystem.UI
{
    public partial class ReturnItemWindow : Window
    {
        private readonly string _connectionString = accounting_project.Infrastructure.DbConnectionFactory.ConnectionString;
        public int CurrentShiftID { get; set; }
        public ObservableCollection<ReturnItemModel> InvoiceItems { get; set; } = new ObservableCollection<ReturnItemModel>();

        public ReturnItemWindow(int currentShiftId)
        {
            InitializeComponent();
            CurrentShiftID = currentShiftId;
            dgInvoiceItems.ItemsSource = InvoiceItems;
        }

        // البحث عند ضغط Enter أو زر البحث
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            LoadInvoiceData();
        }

        private void TxtInvoiceID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoadInvoiceData();
            }
        }

        // جلب تفاصيل الفاتورة من قاعدة البيانات
        private void LoadInvoiceData()
        {
            // 1. تفريغ القائمة وتصفير المجموع فوراً عند البدء لضمان مسح البيانات القديمة
            UnsubscribeItemEvents();
            InvoiceItems.Clear();
            lblTotalRefund.Text = "0.00";

            // 2. التحقق من صحة رقم الفاتورة
            if (!int.TryParse(txtInvoiceID.Text.Trim(), out int invoiceId) || invoiceId <= 0)
            {
                MessageBox.Show("يرجى إدخال رقم فاتورة صحيح أكبر من الصفر.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                                                        d.ItemID,
                            i.ItemName,
                            d.UnitPrice,
                            d.Quantity AS SoldQty,
                            ISNULL((SELECT SUM(rd.Quantity)
                                FROM dbo.SalesReturns r
                                INNER JOIN dbo.SalesReturnDetails rd ON rd.SalesReturnID=r.SalesReturnID
                                WHERE r.SalesInvoiceID=d.SalesInvoiceID AND rd.ItemID=d.ItemID), 0) AS ReturnedQty
                        FROM dbo.SalesInvoiceDetails d
                        INNER JOIN dbo.Items i ON d.ItemID = i.ItemID
                        WHERE d.SalesInvoiceID = @InvoiceID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@InvoiceID", SqlDbType.Int).Value = invoiceId;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new ReturnItemModel
                                {
                                    ItemID = Convert.ToInt32(reader["ItemID"]),
                                    ItemName = reader["ItemName"].ToString(),
                                    UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                                    SoldQty = Convert.ToDecimal(reader["SoldQty"]),
                                    ReturnedQty = Convert.ToDecimal(reader["ReturnedQty"]),
                                    ReturnQty = 0
                                };

                                item.PropertyChanged += Item_PropertyChanged;
                                InvoiceItems.Add(item);
                            }
                        }
                    }

                    if (InvoiceItems.Count == 0)
                    {
                        MessageBox.Show($"لم يتم العثور على الفاتورة رقم ({invoiceId}) أو أنها لا تحتوي على أصناف!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء جلب الفاتورة: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            CalculateTotalRefund();
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ReturnItemModel.ReturnQty))
            {
                CalculateTotalRefund();
            }
        }

        private void UnsubscribeItemEvents()
        {
            foreach (var item in InvoiceItems)
            {
                item.PropertyChanged -= Item_PropertyChanged;
            }
        }

        private void CalculateTotalRefund()
        {
            decimal totalRefund = InvoiceItems.Where(i => i.ReturnQty > 0).Sum(i => i.ReturnQty * i.UnitPrice);
            lblTotalRefund.Text = totalRefund.ToString("N2");
        }

        // تنفيذ المرتجع وتحديث المخزون في قاعدة البيانات
        private void BtnConfirmReturn_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtInvoiceID.Text.Trim(), out int invoiceId)) return;

            var itemsToReturn = InvoiceItems.Where(i => i.ReturnQty > 0).ToList();

            if (itemsToReturn.Count == 0)
            {
                MessageBox.Show("يرجى تحديد كمية مسترجعة أكبر من الصفر لصنف واحد على الأقل!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // التحقق من أن الكميات المسترجعة لا تتجاوز المسموح به
            foreach (var item in itemsToReturn)
            {
                if (item.ReturnQty > item.RemainingQty)
                {
                    MessageBox.Show($"الكمية المسترجعة للصنف ({item.ItemName}) تتجاوز المسموح! المتبقي الممكن إرجاعه هو [{item.RemainingQty}].", "خطأ كمية", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            foreach (var item in itemsToReturn)
                            {
                                decimal returnAmount = item.ReturnQty * item.UnitPrice;

                                // كل سطر مرتجع ينشئ رأساً وتفصيلاً وحركة مخزنية مترابطة.
                                string insertReturnQuery = @"
                                    INSERT dbo.SalesReturns(SalesInvoiceID,ReturnDate,TotalAmount)
                                    VALUES(@InvoiceID,SYSUTCDATETIME(),@ReturnAmount);
                                    SELECT CONVERT(bigint,SCOPE_IDENTITY());";
                                long returnId;
                                using (SqlCommand cmdReturn = new SqlCommand(insertReturnQuery, conn, transaction))
                                {
                                    cmdReturn.Parameters.Add("@InvoiceID", SqlDbType.BigInt).Value = invoiceId;
                                    cmdReturn.Parameters.Add("@ReturnAmount", SqlDbType.Decimal).Value = returnAmount;
                                    returnId = Convert.ToInt64(cmdReturn.ExecuteScalar());
                                }

                                string detailAndMovement = @"
                                    INSERT dbo.SalesReturnDetails(SalesReturnID,ItemID,Quantity,UnitPrice,CostPrice)
                                    VALUES(@ReturnID,@ItemID,@ReturnQty,@UnitPrice,@UnitPrice);
                                    UPDATE dbo.Items SET StockQuantity=StockQuantity+@ReturnQty WHERE ItemID=@ItemID;
                                    INSERT dbo.InventoryMovements(ItemID,BranchID,MovementDate,MovementType,QuantityIn,UnitCost,SourceType,SourceID)
                                    VALUES(@ItemID,1,SYSUTCDATETIME(),3,@ReturnQty,@UnitPrice,N'SalesReturn',@ReturnID);";
                                using (SqlCommand cmdStock = new SqlCommand(detailAndMovement, conn, transaction))
                                {
                                    cmdStock.Parameters.Add("@ReturnID", SqlDbType.BigInt).Value = returnId;
                                    cmdStock.Parameters.Add("@ItemID", SqlDbType.Int).Value = item.ItemID;
                                    cmdStock.Parameters.Add("@ReturnQty", SqlDbType.Decimal).Value = item.ReturnQty;
                                    cmdStock.Parameters.Add("@UnitPrice", SqlDbType.Decimal).Value = item.UnitPrice;
                                    cmdStock.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();

                            PrintReturnReceipt(invoiceId, itemsToReturn, itemsToReturn.Sum(i => i.ReturnQty * i.UnitPrice));
                            MessageBox.Show("تمت عملية الإرجاع وتحديث المخزون بنجاح!", "نجاح العملية", MessageBoxButton.OK, MessageBoxImage.Information);
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show("حدث خطأ أثناء معالجة المرتجع: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ اتصالات قاعدة البيانات: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintReturnReceipt(int invoiceId, List<ReturnItemModel> returnedItems, decimal totalRefund)
        {
            try
            {
                PrintDocument pd = new PrintDocument();

                pd.DefaultPageSettings.PaperSize = new PaperSize("Thermal 80mm", 315, 1000);
                pd.DefaultPageSettings.Margins = new Margins(5, 5, 5, 5);

                pd.PrintPage += (sender, e) =>
                {
                    using (Font headerFont = new Font("Tahoma", 12, System.Drawing.FontStyle.Bold))
                    using (Font titleFont = new Font("Tahoma", 10, System.Drawing.FontStyle.Bold))
                    using (Font bodyFont = new Font("Tahoma", 8.5f, System.Drawing.FontStyle.Regular))
                    using (Font boldFont = new Font("Tahoma", 8.5f, System.Drawing.FontStyle.Bold))
                    using (Pen dashPen = new Pen(Color.Black, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    {
                        float y = 10;
                        float startX = 5;
                        float printableWidth = 275;

                        StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center };
                        StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far, FormatFlags = StringFormatFlags.DirectionRightToLeft };
                        StringFormat sfLeft = new StringFormat { Alignment = StringAlignment.Near };

                        // 1. ترويسة
                        e.Graphics.DrawString("نظام المبيعات", headerFont, Brushes.Black, new RectangleF(startX, y, printableWidth, 22), sfCenter);
                        y += 22;

                        e.Graphics.DrawString("*** إشعار مرتجع مبيعات ***", titleFont, Brushes.Black, new RectangleF(startX, y, printableWidth, 20), sfCenter);
                        y += 22;

                        e.Graphics.DrawLine(dashPen, startX, y, startX + printableWidth, y);
                        y += 8;

                        // 2. معلومات
                        e.Graphics.DrawString($"رقم الفاتورة الأصلي: #{invoiceId}", bodyFont, Brushes.Black, new RectangleF(startX, y, printableWidth, 18), sfRight);
                        y += 18;
                        e.Graphics.DrawString($"التاريخ والوقت: {DateTime.Now:yyyy/MM/dd hh:mm tt}", bodyFont, Brushes.Black, new RectangleF(startX, y, printableWidth, 18), sfRight);
                        y += 18;
                        e.Graphics.DrawString($"رقم الوردية: {CurrentShiftID}", bodyFont, Brushes.Black, new RectangleF(startX, y, printableWidth, 18), sfRight);
                        y += 22;

                        e.Graphics.DrawLine(dashPen, startX, y, startX + printableWidth, y);
                        y += 8;

                        // 3. الجدول
                        e.Graphics.DrawString("الصنف", boldFont, Brushes.Black, new RectangleF(startX + 150, y, 125, 18), sfRight);
                        e.Graphics.DrawString("الكمية", boldFont, Brushes.Black, new RectangleF(startX + 105, y, 40, 18), sfCenter);
                        e.Graphics.DrawString("السعر", boldFont, Brushes.Black, new RectangleF(startX + 55, y, 45, 18), sfCenter);
                        e.Graphics.DrawString("الإجمالي", boldFont, Brushes.Black, new RectangleF(startX, y, 50, 18), sfLeft);
                        y += 20;

                        e.Graphics.DrawLine(Pens.Black, startX, y, startX + printableWidth, y);
                        y += 6;

                        // 4. التفاصيل
                        foreach (var item in returnedItems)
                        {
                            decimal lineTotal = item.ReturnQty * item.UnitPrice;

                            e.Graphics.DrawString(item.ItemName, bodyFont, Brushes.Black, new RectangleF(startX + 150, y, 125, 18), sfRight);
                            e.Graphics.DrawString(item.ReturnQty.ToString("G29"), bodyFont, Brushes.Black, new RectangleF(startX + 105, y, 40, 18), sfCenter);
                            e.Graphics.DrawString(item.UnitPrice.ToString("N2"), bodyFont, Brushes.Black, new RectangleF(startX + 55, y, 45, 18), sfCenter);
                            e.Graphics.DrawString(lineTotal.ToString("N2"), bodyFont, Brushes.Black, new RectangleF(startX, y, 50, 18), sfLeft);

                            y += 18;
                        }

                        y += 4;
                        e.Graphics.DrawLine(dashPen, startX, y, startX + printableWidth, y);
                        y += 8;

                        // 5. الإجمالي والتوقيع
                        e.Graphics.DrawString("إجمالي المبلغ المسترد:", boldFont, Brushes.Black, new RectangleF(startX + 100, y, 175, 20), sfRight);
                        e.Graphics.DrawString($"{totalRefund:N2} ر.ي", boldFont, Brushes.Black, new RectangleF(startX, y, 100, 20), sfLeft);
                        y += 25;

                        e.Graphics.DrawLine(dashPen, startX, y, startX + printableWidth, y);
                        y += 12;

                        e.Graphics.DrawString("توقيع العميل: ...................................", bodyFont, Brushes.Black, new RectangleF(startX, y, printableWidth, 18), sfRight);
                        y += 25;

                        e.Graphics.DrawString("احتفظ بهذا الإشعار للمراجعة", bodyFont, Brushes.Black, new RectangleF(startX, y, printableWidth, 18), sfCenter);
                    }
                };

                pd.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show("تم حفظ المرتجع بنجاح، لكن تعذرت طباعة الإيصال: " + ex.Message, "تنبيه الطباعة", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void DecimalValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$");
            string fullText = ((TextBox)sender).Text.Insert(((TextBox)sender).SelectionStart, e.Text);
            e.Handled = !regex.IsMatch(fullText);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    // كلاس تمثيل البيانات مع القيود التلقائية والخصائص المحسوبة
    public class ReturnItemModel : INotifyPropertyChanged
    {
        private decimal _returnQty;

        public int ItemID { get; set; }
        public string ItemName { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SoldQty { get; set; }
        public decimal ReturnedQty { get; set; }

        // الكمية المتبقية الممكن إرجاعها
        public decimal RemainingQty => SoldQty - ReturnedQty;

        // حالة إرجاع الصنف بالكامل
        public bool IsFullyReturned => RemainingQty <= 0;

        public decimal ReturnQty
        {
            get => _returnQty;
            set
            {
                // منع القيم السالبة
                if (value < 0) value = 0;

                // منع تجاوز الكمية المتبقية
                if (value > RemainingQty) value = RemainingQty;

                if (_returnQty != value)
                {
                    _returnQty = value;
                    OnPropertyChanged(nameof(ReturnQty));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}