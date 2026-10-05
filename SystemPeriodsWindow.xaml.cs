using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows;
using accounting_project.Infrastructure;

namespace accounting_project
{
    public partial class SystemPeriodsWindow : Window
    {
        private readonly string connStr = DbConnectionFactory.ConnectionString;
        private readonly DataTable dtPeriods = new DataTable();

        public SystemPeriodsWindow()
        {
            InitializeComponent();
            InitializePeriodsTableStructure();
            LoadExistingPeriods();
        }

        private void InitializePeriodsTableStructure()
        {
            dtPeriods.Columns.Add("PeriodID", typeof(int));
            dtPeriods.Columns.Add("FromDate", typeof(DateTime));
            dtPeriods.Columns.Add("ToDate", typeof(DateTime));
            dtPeriods.Columns.Add("PeriodName", typeof(string));
            dtPeriods.Columns.Add("ForeignName", typeof(string));
            dgPeriods.ItemsSource = dtPeriods.DefaultView;
        }

        private void LoadExistingPeriods()
        {
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (var da = new SqlDataAdapter(@"SELECT p.PeriodNo AS PeriodID,p.StartDate AS FromDate,p.EndDate AS ToDate,
                        p.PeriodName,y.YearName AS ForeignName FROM dbo.GL_FiscalPeriods p
                        INNER JOIN dbo.GL_FiscalYears y ON y.FiscalYearID=p.FiscalYearID
                        WHERE y.CompanyID=1 ORDER BY y.StartDate,p.PeriodNo", conn))
                    { dtPeriods.Clear(); da.Fill(dtPeriods); }
                    using (var cmd = new SqlCommand(@"SELECT TOP 1 YearName,StartDate,EndDate FROM dbo.GL_FiscalYears WHERE CompanyID=1 ORDER BY StartDate DESC", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var start = Convert.ToDateTime(reader["StartDate"]); var end = Convert.ToDateTime(reader["EndDate"]);
                            txtFromMonth.Text = start.Month.ToString(); txtFromYear.Text = start.Year.ToString();
                            txtToMonth.Text = end.Month.ToString(); txtToYear.Text = end.Year.ToString();
                            txtNumOfPeriods.Text = dtPeriods.Rows.Count == 0 ? "12" : dtPeriods.Rows.Count.ToString();
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("خطأ أثناء جلب الفترات المالية: " + ex.Message); }
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int number = int.Parse(txtNumOfPeriods.Text.Trim());
                int year = int.Parse(txtFromYear.Text.Trim()); int month = int.Parse(txtFromMonth.Text.Trim());
                if (number < 1 || number > 53 || month < 1 || month > 12) throw new ArgumentException("عدد أو بداية الفترات غير صحيحة.");
                int step = cmbPeriodType.SelectedIndex == 1 ? 3 : 1;
                dtPeriods.Clear();
                var ar = new CultureInfo("ar-SA"); var en = new CultureInfo("en-US");
                for (int i = 1; i <= number; i++)
                {
                    var from = new DateTime(year, month, 1);
                    var toMonth = from.AddMonths(step).AddDays(-1);
                    dtPeriods.Rows.Add(i, from, toMonth, from.ToString("MMMM", ar), from.ToString("MMMM", en));
                    month += step; while (month > 12) { month -= 12; year++; }
                }
                txtToMonth.Text = ((DateTime)dtPeriods.Rows[dtPeriods.Rows.Count - 1]["ToDate"]).Month.ToString();
                txtToYear.Text = ((DateTime)dtPeriods.Rows[dtPeriods.Rows.Count - 1]["ToDate"]).Year.ToString();
                MessageBox.Show("تم توليد الفترات المالية.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("تعذر توليد الفترات: " + ex.Message); }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (dtPeriods.Rows.Count == 0) { MessageBox.Show("ولّد الفترات أولاً."); return; }
            try
            {
                int startYear = Convert.ToDateTime(dtPeriods.Rows[0]["FromDate"]).Year;
                var startDate = Convert.ToDateTime(dtPeriods.Rows[0]["FromDate"]).Date;
                var endDate = Convert.ToDateTime(dtPeriods.Rows[dtPeriods.Rows.Count - 1]["ToDate"]).Date;
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction(IsolationLevel.Serializable))
                    {
                        try
                        {
                            int fiscalYearId;
                            using (var cmd = new SqlCommand(@"SELECT FiscalYearID FROM dbo.GL_FiscalYears WHERE CompanyID=1 AND YearName=@YearName", conn, tx))
                            { cmd.Parameters.Add("@YearName", SqlDbType.NVarChar, 20).Value = startYear.ToString(); var value = cmd.ExecuteScalar(); fiscalYearId = value == null ? 0 : Convert.ToInt32(value); }
                            if (fiscalYearId == 0)
                            {
                                using (var cmd = new SqlCommand(@"INSERT dbo.GL_FiscalYears(CompanyID,YearName,StartDate,EndDate) VALUES(1,@YearName,@StartDate,@EndDate); SELECT CONVERT(int,SCOPE_IDENTITY());", conn, tx))
                                { cmd.Parameters.Add("@YearName", SqlDbType.NVarChar, 20).Value = startYear.ToString(); cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate; cmd.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate; fiscalYearId = Convert.ToInt32(cmd.ExecuteScalar()); }
                            }
                            else
                            {
                                using (var cmd = new SqlCommand("UPDATE dbo.GL_FiscalYears SET StartDate=@StartDate,EndDate=@EndDate WHERE FiscalYearID=@ID AND IsClosed=0", conn, tx))
                                { cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate; cmd.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate; cmd.Parameters.Add("@ID", SqlDbType.Int).Value = fiscalYearId; if (cmd.ExecuteNonQuery() == 0) throw new InvalidOperationException("السنة المالية مغلقة أو غير قابلة للتعديل."); }
                            }
                            foreach (DataRow row in dtPeriods.Rows)
                            {
                                const string sql = @"IF EXISTS(SELECT 1 FROM dbo.GL_FiscalPeriods WHERE FiscalYearID=@YearID AND PeriodNo=@PeriodNo)
UPDATE dbo.GL_FiscalPeriods SET PeriodName=@Name,StartDate=@StartDate,EndDate=@EndDate WHERE FiscalYearID=@YearID AND PeriodNo=@PeriodNo
ELSE INSERT dbo.GL_FiscalPeriods(FiscalYearID,PeriodNo,PeriodName,StartDate,EndDate) VALUES(@YearID,@PeriodNo,@Name,@StartDate,@EndDate);";
                                using (var cmd = new SqlCommand(sql, conn, tx))
                                { cmd.Parameters.Add("@YearID", SqlDbType.Int).Value = fiscalYearId; cmd.Parameters.Add("@PeriodNo", SqlDbType.TinyInt).Value = Convert.ToInt32(row["PeriodID"]); cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = Convert.ToString(row["PeriodName"]); cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = Convert.ToDateTime(row["FromDate"]).Date; cmd.Parameters.Add("@EndDate", SqlDbType.Date).Value = Convert.ToDateTime(row["ToDate"]).Date; cmd.ExecuteNonQuery(); }
                            }
                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
                MessageBox.Show("تم حفظ إعدادات السنة والفترات المالية.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("خطأ أثناء الحفظ: " + ex.Message); }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e) { dtPeriods.Clear(); txtNumOfPeriods.Text = "12"; txtFromMonth.Text = "1"; txtToMonth.Text = "12"; cmbPeriodType.SelectedIndex = 0; cmbTaxPeriodType.SelectedIndex = 0; }
        private void BtnClose_Click(object sender, RoutedEventArgs e) { Close(); }
    }
}
