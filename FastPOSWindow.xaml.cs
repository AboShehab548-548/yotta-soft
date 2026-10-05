using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using accounting_project.Infrastructure;

namespace AccountingSystem.UI
{
    public partial class FastPOSWindow : Window
    {
        public ObservableCollection<CartItemDto> CartItems { get; } = new ObservableCollection<CartItemDto>();
        private readonly string connectionString = DbConnectionFactory.ConnectionString;
        private const int CompanyId = 1;
        private const int BranchId = 1;
        private bool isShiftOpened;
        public int CurrentShiftID { get; private set; }
        public decimal OpeningBalance { get; private set; }
        private string currentCurrency = "YER";
        private string companyName = "الشركة الافتراضية";

        public FastPOSWindow()
        {
            InitializeComponent(); dgCart.ItemsSource = CartItems; CartItems.CollectionChanged += CartItems_CollectionChanged;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSystemSettings(); PromptOpeningBalance(); txtBarcode.Focus();
        }

        private void CartItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null) foreach (CartItemDto item in e.NewItems) item.PropertyChanged += CartItem_PropertyChanged;
            if (e.OldItems != null) foreach (CartItemDto item in e.OldItems) item.PropertyChanged -= CartItem_PropertyChanged;
            CalculateTotals();
        }

        private void CartItem_PropertyChanged(object sender, PropertyChangedEventArgs e) { if (e.PropertyName == nameof(CartItemDto.Quantity) || e.PropertyName == nameof(CartItemDto.UnitPrice) || e.PropertyName == nameof(CartItemDto.Total)) CalculateTotals(); }

        public void CalculateTotals()
        {
            decimal total = CartItems.Sum(i => i.Total), discount = 0; decimal.TryParse(txtDiscount?.Text, out discount); if (discount < 0) discount = 0; if (discount > total) discount = total;
            decimal net = total - discount;
            if (lblTotalSales != null) lblTotalSales.Text = total.ToString("N2"); if (lblNetAmount != null) lblNetAmount.Text = net.ToString("N2") + " " + currentCurrency; if (txtGrandTotal != null) txtGrandTotal.Text = net.ToString("N2");
            if (txtItemsCount != null) txtItemsCount.Text = CartItems.Count.ToString(); if (txtTotalQuantity != null) txtTotalQuantity.Text = CartItems.Sum(i => i.Quantity).ToString("G29");
        }

        private void PromptOpeningBalance()
        {
            if (isShiftOpened) return;
            string input = Microsoft.VisualBasic.Interaction.InputBox("أدخل مبلغ النقدية الافتتاحي في درج الكاشير:", "افتتاح الوردية", "0.00");
            if (!decimal.TryParse(input, out var balance) || balance < 0) balance = 0; OpeningBalance = balance;
            try
            {
                using (var conn = new SqlConnection(connectionString)) using (var cmd = new SqlCommand(@"INSERT dbo.Shifts(CompanyID,BranchID,CashierName,StartTime,OpeningBalance,IsClosed) VALUES(@CompanyID,@BranchID,@Cashier,GETDATE(),@Opening,0); SELECT CONVERT(int,SCOPE_IDENTITY());", conn))
                { Add(cmd, "@CompanyID", SqlDbType.Int, CompanyId); Add(cmd, "@BranchID", SqlDbType.Int, BranchId); Add(cmd, "@Cashier", SqlDbType.NVarChar, "المستخدم الحالي"); AddDecimal(cmd, "@Opening", OpeningBalance); conn.Open(); CurrentShiftID = Convert.ToInt32(cmd.ExecuteScalar()); isShiftOpened = true; }
            }
            catch (Exception ex) { MessageBox.Show("تعذر افتتاح الوردية: " + ex.Message); }
        }

        private void LoadSystemSettings()
        {
            try
            {
                using (var conn = new SqlConnection(connectionString)) using (var cmd = new SqlCommand(@"SELECT TOP 1 c.CurrencyCode,c.CurrencyNameAr FROM dbo.System_Currencies c WHERE c.IsBase=1 AND c.IsActive=1 ORDER BY c.CurrencyID", conn))
                { conn.Open(); using (var r = cmd.ExecuteReader()) if (r.Read()) { currentCurrency = Convert.ToString(r["CurrencyCode"]); txtCurrencyLabel.Text = "العملة: " + currentCurrency; } }
            }
            catch { txtCurrencyLabel.Text = "العملة: " + currentCurrency; }
        }

        private void txtBarcode_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(txtBarcode.Text)) { ProcessBarcodeScan(txtBarcode.Text.Trim()); txtBarcode.Clear(); txtBarcode.Focus(); } }

        private void ProcessBarcodeScan(string barcode)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString)) using (var cmd = new SqlCommand(@"SELECT i.ItemID,i.ItemName,i.UnitName,i.UnitPrice,i.ReorderLevel,ISNULL(SUM(b.CurrentQuantity),0) AS Available FROM dbo.Items i LEFT JOIN dbo.ItemBatches b ON b.ItemID=i.ItemID AND b.BranchID=@BranchID WHERE (i.Barcode=@Code OR i.ItemCode=@Code) AND i.IsActive=1 GROUP BY i.ItemID,i.ItemName,i.UnitName,i.UnitPrice,i.ReorderLevel", conn))
                { Add(cmd, "@BranchID", SqlDbType.Int, BranchId); Add(cmd, "@Code", SqlDbType.NVarChar, barcode); conn.Open(); using (var r = cmd.ExecuteReader()) { if (!r.Read()) { MessageBox.Show("الصنف غير موجود أو غير نشط."); return; } var stock = Convert.ToDecimal(r["Available"]); var name = Convert.ToString(r["ItemName"]); if (stock <= 0) { MessageBox.Show("الصنف [" + name + "] غير متوفر في دفعات هذا الفرع."); return; } if (stock <= Convert.ToDecimal(r["ReorderLevel"])) MessageBox.Show("تنبيه: وصل الصنف للحد الأدنى.", "المخزون", MessageBoxButton.OK, MessageBoxImage.Warning); AddItemToGrid(Convert.ToInt32(r["ItemID"]), name, Convert.ToString(r["UnitName"]), Convert.ToDecimal(r["UnitPrice"]), stock, barcode); } }
            }
            catch (Exception ex) { MessageBox.Show("خطأ في قراءة الصنف: " + ex.Message); }
        }

        private void AddItemToGrid(int itemId, string name, string unit, decimal price, decimal stock, string barcode)
        {
            var existing = CartItems.FirstOrDefault(i => i.ItemID == itemId);
            if (existing != null) { if (existing.Quantity + 1 > stock) { MessageBox.Show("الكمية المتاحة غير كافية."); return; } existing.Quantity += 1; }
            else CartItems.Add(new CartItemDto { ItemID = itemId, ItemName = name, UnitName = string.IsNullOrWhiteSpace(unit) ? "حبة" : unit, UnitPrice = price, Quantity = 1, Barcode = barcode });
        }

        private void txtDiscount_TextChanged(object sender, TextChangedEventArgs e) { CalculateTotals(); }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (CartItems.Count == 0) { MessageBox.Show("لا يمكن حفظ فاتورة فارغة."); return; }
            if (CurrentShiftID <= 0) { MessageBox.Show("لا توجد وردية مفتوحة."); return; }
            decimal total = CartItems.Sum(i => i.Total), discount = 0; decimal.TryParse(txtDiscount.Text, out discount); if (discount < 0 || discount > total) { MessageBox.Show("قيمة الخصم غير صحيحة."); return; }
            decimal net = total - discount; int paymentType = rbCash.IsChecked == true ? 1 : 2;
            try
            {
                using (var conn = new SqlConnection(connectionString))
                { conn.Open(); using (var tx = conn.BeginTransaction(IsolationLevel.Serializable)) { try { ResolveContext(conn, tx, out var yearId, out var periodId); string invoiceNo = DateTime.Now.ToString("yyyyMMddHHmmssfff"); long invoiceId;
                        using (var cmd = new SqlCommand(@"INSERT dbo.SalesInvoices(CompanyID,BranchID,CustomerID,InvoiceNumber,InvoiceDate,FiscalYearID,FiscalPeriodID,TotalAmount,PaidAmount,ShiftID,DiscountAmount,NetAmount,PaymentType) VALUES(@CompanyID,@BranchID,NULL,@No,GETDATE(),@YearID,@PeriodID,@Total,@Paid,@ShiftID,@Discount,@Net,@Payment); SELECT CONVERT(bigint,SCOPE_IDENTITY());", conn, tx))
                        { Add(cmd, "@CompanyID", SqlDbType.Int, CompanyId); Add(cmd, "@BranchID", SqlDbType.Int, BranchId); Add(cmd, "@No", SqlDbType.NVarChar, invoiceNo); Add(cmd, "@YearID", SqlDbType.Int, yearId); Add(cmd, "@PeriodID", SqlDbType.Int, periodId); AddDecimal(cmd, "@Total", total); AddDecimal(cmd, "@Paid", net); Add(cmd, "@ShiftID", SqlDbType.Int, CurrentShiftID); AddDecimal(cmd, "@Discount", discount); AddDecimal(cmd, "@Net", net); Add(cmd, "@Payment", SqlDbType.TinyInt, paymentType); invoiceId = Convert.ToInt64(cmd.ExecuteScalar()); }
                        int lineNo = 0; foreach (var item in CartItems) { decimal remaining = item.Quantity; while (remaining > 0) { long batchId; decimal take, cost; using (var cmd = new SqlCommand(@"SELECT TOP 1 BatchID,CurrentQuantity,CostPrice FROM dbo.ItemBatches WITH(UPDLOCK,ROWLOCK) WHERE ItemID=@ItemID AND BranchID=@BranchID AND CurrentQuantity>0 ORDER BY CASE WHEN ExpiryDate IS NULL THEN 1 ELSE 0 END,ExpiryDate,BatchID", conn, tx)) { Add(cmd, "@ItemID", SqlDbType.Int, item.ItemID); Add(cmd, "@BranchID", SqlDbType.Int, BranchId); using (var r = cmd.ExecuteReader()) { if (!r.Read()) throw new InvalidOperationException("لا توجد كمية كافية في دفعات الصنف: " + item.ItemName); batchId = Convert.ToInt64(r["BatchID"]); var available = Convert.ToDecimal(r["CurrentQuantity"]); cost = Convert.ToDecimal(r["CostPrice"]); take = Math.Min(remaining, available); } }
                                using (var cmd = new SqlCommand("UPDATE dbo.ItemBatches SET CurrentQuantity=CurrentQuantity-@Qty WHERE BatchID=@BatchID AND CurrentQuantity>=@Qty", conn, tx)) { AddDecimal(cmd, "@Qty", take); Add(cmd, "@BatchID", SqlDbType.BigInt, batchId); if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("تعذر حجز كمية الدفعة."); }
                                using (var cmd = new SqlCommand(@"INSERT dbo.SalesInvoiceDetails(SalesInvoiceID,ItemID,Quantity,UnitPrice,BatchID,CostPrice) VALUES(@InvoiceID,@ItemID,@Qty,@Price,@BatchID,@Cost);", conn, tx))
                                { Add(cmd, "@InvoiceID", SqlDbType.BigInt, invoiceId); Add(cmd, "@ItemID", SqlDbType.Int, item.ItemID); AddDecimal(cmd, "@Qty", take); AddDecimal(cmd, "@Price", item.UnitPrice); Add(cmd, "@BatchID", SqlDbType.BigInt, batchId); AddDecimal(cmd, "@Cost", cost); cmd.ExecuteNonQuery(); }
                                using (var cmd = new SqlCommand(@"INSERT dbo.InventoryMovements(ItemID,BatchID,BranchID,MovementDate,MovementType,QuantityOut,UnitCost,SourceType,SourceID,Notes) VALUES(@ItemID,@BatchID,@BranchID,GETDATE(),2,@Qty,@Cost,N'SALES',@InvoiceID,N'بيع نقطة البيع');", conn, tx))
                                { Add(cmd, "@ItemID", SqlDbType.Int, item.ItemID); Add(cmd, "@BatchID", SqlDbType.BigInt, batchId); Add(cmd, "@BranchID", SqlDbType.Int, BranchId); AddDecimal(cmd, "@Qty", take); AddDecimal(cmd, "@Cost", cost); Add(cmd, "@InvoiceID", SqlDbType.BigInt, invoiceId); cmd.ExecuteNonQuery(); }
                                using (var cmd = new SqlCommand("UPDATE dbo.Items SET StockQuantity=StockQuantity-@Qty WHERE ItemID=@ItemID AND StockQuantity>=@Qty", conn, tx))
                                { Add(cmd, "@ItemID", SqlDbType.Int, item.ItemID); AddDecimal(cmd, "@Qty", take); if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("رصيد المخزون العام للصنف أقل من الكمية المباعة."); }
                                remaining -= take; lineNo++;
                            } }
                        using (var cmd = new SqlCommand("UPDATE dbo.Shifts SET CashSales=CashSales+CASE WHEN @Payment=1 THEN @Net ELSE 0 END,CardSales=CardSales+CASE WHEN @Payment=2 THEN @Net ELSE 0 END,ExpectedCash=OpeningBalance+CashSales+CASE WHEN @Payment=1 THEN @Net ELSE 0 END WHERE ShiftID=@ShiftID AND IsClosed=0", conn, tx)) { Add(cmd, "@Payment", SqlDbType.TinyInt, paymentType); AddDecimal(cmd, "@Net", net); Add(cmd, "@ShiftID", SqlDbType.Int, CurrentShiftID); cmd.ExecuteNonQuery(); }
                        tx.Commit(); PrintThermalReceipt(invoiceId, total, discount, net, paymentType == 1 ? "نقداً" : "شبكة"); ResetForm(); } catch { tx.Rollback(); throw; } } }
            }
            catch (Exception ex) { MessageBox.Show("حدث خطأ أثناء حفظ الفاتورة: " + ex.Message); }
        }

        private static void ResolveContext(SqlConnection conn, SqlTransaction tx, out int yearId, out int periodId)
        {
            using (var cmd = new SqlCommand(@"SELECT TOP 1 y.FiscalYearID,p.FiscalPeriodID FROM dbo.GL_FiscalYears y INNER JOIN dbo.GL_FiscalPeriods p ON p.FiscalYearID=y.FiscalYearID WHERE CAST(GETDATE() AS date) BETWEEN y.StartDate AND y.EndDate AND CAST(GETDATE() AS date) BETWEEN p.StartDate AND p.EndDate AND y.IsClosed=0 AND p.IsClosed=0 ORDER BY p.PeriodNo", conn, tx)) using (var r = cmd.ExecuteReader()) { if (!r.Read()) throw new InvalidOperationException("لا توجد سنة أو فترة مالية مفتوحة تغطي تاريخ اليوم."); yearId = Convert.ToInt32(r["FiscalYearID"]); periodId = Convert.ToInt32(r["FiscalPeriodID"]); }
        }

        private void OpenCloseShiftWindow()
        {
            try
            {
                decimal cash = 0, card = 0; using (var conn = new SqlConnection(connectionString)) using (var cmd = new SqlCommand("SELECT CashSales,CardSales FROM dbo.Shifts WHERE ShiftID=@ID", conn)) { Add(cmd, "@ID", SqlDbType.Int, CurrentShiftID); conn.Open(); using (var r = cmd.ExecuteReader()) if (r.Read()) { cash = Convert.ToDecimal(r["CashSales"]); card = Convert.ToDecimal(r["CardSales"]); } }
                var close = new CloseShiftWindow(CurrentShiftID, "المستخدم الحالي", DateTime.Now, OpeningBalance, cash, card, 0); close.Owner = this; close.ShowDialog(); if (close.IsShiftClosedSuccessfully) Close();
            }
            catch (Exception ex) { MessageBox.Show("تعذر تحميل بيانات الوردية: " + ex.Message); }
        }

        private void OpenReturnWindow() { var win = new ReturnItemWindow(CurrentShiftID); win.Owner = this; win.ShowDialog(); }
        private void HoldCurrentInvoice() { MessageBox.Show("تعليق الفاتورة متاح من شاشة الفواتير المعلقة بعد تهيئة الجداول."); }
        private void RecallPendingInvoice() { var win = new PendingInvoicesWindow(CurrentShiftID); win.Owner = this; win.ShowDialog(); }
        private void BtnCancelInvoice_Click(object sender, RoutedEventArgs e) { CartItems.Clear(); CalculateTotals(); }
        private void OpenSearchWindow() { var win = new ItemSearchWindow(connectionString); win.Owner = this; if (win.ShowDialog() == true && win.SelectedItem != null) AddItemToGrid(win.SelectedItem.ItemID, win.SelectedItem.ItemName, win.SelectedItem.UnitName, win.SelectedItem.UnitPrice, decimal.MaxValue, win.SelectedItem.Barcode); }
        private void BtnSearch_Click(object sender, RoutedEventArgs e) { OpenSearchWindow(); }
        private void Button_Click(object sender, RoutedEventArgs e) { OpenReturnWindow(); }
        private void dgItems_MouseDoubleClick(object sender, MouseButtonEventArgs e) { OpenSearchWindow(); }
        private void BtnApplyDiscount_Click(object sender, RoutedEventArgs e) { txtDiscount.IsEnabled = true; txtDiscount.Focus(); txtDiscount.SelectAll(); }
        private void RecallInvoice_Click(object sender, RoutedEventArgs e) { RecallPendingInvoice(); }
        private void HoldInvoice_Click(object sender, RoutedEventArgs e) { HoldCurrentInvoice(); }
        private void TxtBarcode_TextChanged(object sender, TextChangedEventArgs e) { }

        private void dgCart_PreviewKeyDown(object sender, KeyEventArgs e) { if (dgCart.SelectedItem is CartItemDto item) { if (e.Key == Key.Add || e.Key == Key.OemPlus) { item.Quantity++; e.Handled = true; } else if ((e.Key == Key.Subtract || e.Key == Key.OemMinus) && item.Quantity > 1) { item.Quantity--; e.Handled = true; } } }
        private void Window_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.F7) OpenReturnWindow(); else if (e.Key == Key.F3) BtnApplyDiscount_Click(sender, e); else if (e.Key == Key.F5) HoldCurrentInvoice(); else if (e.Key == Key.F6) RecallPendingInvoice(); else if (e.Key == Key.F8) OpenCloseShiftWindow(); else if (e.Key == Key.F4) OpenSearchWindow(); else if (e.Key == Key.F12) BtnSave_Click(sender, e); else if (e.Key == Key.F1) rbCash.IsChecked = true; else if (e.Key == Key.F2) rbCard.IsChecked = true; }

        private void PrintThermalReceipt(long invoiceId, decimal total, decimal discount, decimal net, string method)
        {
            try { var pd = new PrintDialog(); if (pd.ShowDialog() != true) return; var doc = new FlowDocument { PageWidth = 280, PagePadding = new Thickness(10), FlowDirection = FlowDirection.RightToLeft, FontFamily = new System.Windows.Media.FontFamily("Arial") }; doc.Blocks.Add(new Paragraph(new Run(companyName + "\nفاتورة مبيعات\nرقم: " + invoiceId + "\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n--------------------")) { TextAlignment = TextAlignment.Center }); var table = new Table(); table.Columns.Add(new TableColumn()); table.Columns.Add(new TableColumn()); var group = new TableRowGroup(); foreach (var item in CartItems) { var row = new TableRow(); row.Cells.Add(new TableCell(new Paragraph(new Run(item.ItemName + " x " + item.Quantity)))); row.Cells.Add(new TableCell(new Paragraph(new Run(item.Total.ToString("N2"))))); group.Rows.Add(row); } table.RowGroups.Add(group); doc.Blocks.Add(table); doc.Blocks.Add(new Paragraph(new Run("--------------------\nالإجمالي: " + total.ToString("N2") + " " + currentCurrency + "\nالخصم: " + discount.ToString("N2") + "\nالصافي: " + net.ToString("N2") + "\nالدفع: " + method))); pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Invoice_" + invoiceId); } catch (Exception ex) { MessageBox.Show("تعذر الطباعة: " + ex.Message); }
        }

        private void ResetForm() { CartItems.Clear(); txtDiscount.Text = "0.00"; txtDiscount.IsEnabled = false; CalculateTotals(); txtBarcode.Focus(); }
        private static void Add(SqlCommand cmd, string name, SqlDbType type, object value) { var p = cmd.Parameters.Add(name, type); p.Size = 200; p.Value = value ?? DBNull.Value; }
        private static void AddDecimal(SqlCommand cmd, string name, decimal value) { var p = cmd.Parameters.Add(name, SqlDbType.Decimal); p.Precision = 19; p.Scale = 6; p.Value = value; }
    }
}
