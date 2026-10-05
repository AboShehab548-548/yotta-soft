using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using accounting_project.Infrastructure;

namespace accounting_project
{
    public partial class BranchesWindow : Window
    {
private readonly string connStr = DbConnectionFactory.ConnectionString;

        private int currentBranchID = 0;

        public BranchesWindow()
        {
            InitializeComponent();
            LoadCompanies();
            LoadFirstBranch();
        }

        private void LoadCompanies()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT CompanyID, CompanyName FROM System_Companies";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            cmbCompany.Items.Clear();
                            while (reader.Read())
                            {
                                cmbCompany.Items.Add(new { ID = reader["CompanyID"], Name = reader["CompanyID"] + " - " + reader["CompanyName"] });
                            }
                            cmbCompany.DisplayMemberPath = "Name";
                            cmbCompany.SelectedValuePath = "ID";
                            if (cmbCompany.Items.Count > 0) cmbCompany.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل الشركات: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFirstBranch()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT TOP 1 * FROM System_Branches ORDER BY BranchID ASC";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                FillFields(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء جلب بيانات الفرع: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillFields(SqlDataReader reader)
        {
            currentBranchID = reader["BranchID"] != DBNull.Value ? Convert.ToInt32(reader["BranchID"]) : 0;
            txtBranchID.Text = currentBranchID.ToString();

            if (reader["CompanyID"] != DBNull.Value)
                cmbCompany.SelectedValue = Convert.ToInt32(reader["CompanyID"]);

            txtBranchName.Text = reader["BranchName"]?.ToString() ?? "";
            txtForeignName.Text = reader["ForeignName"]?.ToString() ?? "";
            txtYear.Text = reader["Year"] != DBNull.Value ? reader["Year"].ToString() : "2026";
            txtGroupID.Text = reader["GroupID"] != DBNull.Value ? reader["GroupID"].ToString() : "1";
            chkIsMain.IsChecked = reader["IsMain"] != DBNull.Value && Convert.ToBoolean(reader["IsMain"]);

            // بيانات العنوان والاتصال
            txtAddress.Text = reader["Address"]?.ToString() ?? "";
            txtTeleNo.Text = reader["TeleNo"]?.ToString() ?? "";
            txtFaxNo.Text = reader["FaxNo"]?.ToString() ?? "";
            txtPOBox.Text = reader["POBox"]?.ToString() ?? "";
            txtTaxNumber.Text = reader["TaxNumber"]?.ToString() ?? "";
            txtLatitude.Text = reader["Latitude"]?.ToString() ?? "";
            txtLongitude.Text = reader["Longitude"]?.ToString() ?? "";
            txtCommercialRegister.Text = reader["CommercialRegister"]?.ToString() ?? "";

            // ترويسة التقارير
            txtHeader1.Text = reader["HeaderLine1"]?.ToString() ?? "";
            txtHeader2.Text = reader["HeaderLine2"]?.ToString() ?? "";
            txtHeader3.Text = reader["HeaderLine3"]?.ToString() ?? "";
            txtSpecifications.Text = reader["Specifications"]?.ToString() ?? "";
            txtWebsite.Text = reader["Website"]?.ToString() ?? "";
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            currentBranchID = 0;
            txtBranchID.Text = "(تلقائي)";
            txtBranchName.Clear();
            txtForeignName.Clear();
            txtAddress.Clear();
            txtTeleNo.Clear();
            txtFaxNo.Clear();
            txtPOBox.Clear();
            txtTaxNumber.Clear();
            txtLatitude.Clear();
            txtLongitude.Clear();
            txtCommercialRegister.Clear();
            txtHeader1.Clear();
            txtHeader2.Clear();
            txtHeader3.Clear();
            txtSpecifications.Clear();
            txtWebsite.Clear();
            chkIsMain.IsChecked = false;
            txtBranchName.Focus();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBranchName.Text))
            {
                MessageBox.Show("يرجى إدخال اسم الفرع كحد أدنى.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query = "";

                    if (currentBranchID == 0)
                    {
                        query = @"INSERT INTO System_Branches (CompanyID, BranchName, ForeignName, IsMain, GroupID, Year, Address, TeleNo, FaxNo, POBox, TaxNumber, Latitude, Longitude, CommercialRegister, HeaderLine1, HeaderLine2, HeaderLine3, Specifications, Website)
                                  VALUES (@CompanyID, @Name, @FName, @IsMain, @GroupID, @Year, @Address, @TeleNo, @FaxNo, @POBox, @TaxNumber, @Latitude, @Longitude, @CommercialRegister, @H1, @H2, @H3, @Specs, @Web)";
                    }
                    else
                    {
                        query = @"UPDATE System_Branches SET 
                                  CompanyID = @CompanyID, BranchName = @Name, ForeignName = @FName, IsMain = @IsMain, 
                                  GroupID = @GroupID, Year = @Year, Address = @Address, TeleNo = @TeleNo, FaxNo = @FaxNo, 
                                  POBox = @POBox, TaxNumber = @TaxNumber, Latitude = @Latitude, Longitude = @Longitude, 
                                  CommercialRegister = @CommercialRegister, HeaderLine1 = @H1, HeaderLine2 = @H2, HeaderLine3 = @H3, 
                                  Specifications = @Specs, Website = @Web
                                  WHERE BranchID = @ID";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (currentBranchID > 0)
                            cmd.Parameters.AddWithValue("@ID", currentBranchID);

                        cmd.Parameters.AddWithValue("@CompanyID", cmbCompany.SelectedValue ?? 1);
                        cmd.Parameters.AddWithValue("@Name", txtBranchName.Text.Trim());
                        cmd.Parameters.AddWithValue("@FName", string.IsNullOrEmpty(txtForeignName.Text) ? (object)DBNull.Value : txtForeignName.Text.Trim());
                        cmd.Parameters.AddWithValue("@IsMain", chkIsMain.IsChecked ?? false);
                        cmd.Parameters.AddWithValue("@GroupID", string.IsNullOrEmpty(txtGroupID.Text) ? 1 : Convert.ToInt32(txtGroupID.Text.Trim()));
                        cmd.Parameters.AddWithValue("@Year", string.IsNullOrEmpty(txtYear.Text) ? 2026 : Convert.ToInt32(txtYear.Text.Trim()));

                        cmd.Parameters.AddWithValue("@Address", string.IsNullOrEmpty(txtAddress.Text) ? (object)DBNull.Value : txtAddress.Text.Trim());
                        cmd.Parameters.AddWithValue("@TeleNo", string.IsNullOrEmpty(txtTeleNo.Text) ? (object)DBNull.Value : txtTeleNo.Text.Trim());
                        cmd.Parameters.AddWithValue("@FaxNo", string.IsNullOrEmpty(txtFaxNo.Text) ? (object)DBNull.Value : txtFaxNo.Text.Trim());
                        cmd.Parameters.AddWithValue("@POBox", string.IsNullOrEmpty(txtPOBox.Text) ? (object)DBNull.Value : txtPOBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@TaxNumber", string.IsNullOrEmpty(txtTaxNumber.Text) ? (object)DBNull.Value : txtTaxNumber.Text.Trim());
                        cmd.Parameters.AddWithValue("@Latitude", string.IsNullOrEmpty(txtLatitude.Text) ? (object)DBNull.Value : txtLatitude.Text.Trim());
                        cmd.Parameters.AddWithValue("@Longitude", string.IsNullOrEmpty(txtLongitude.Text) ? (object)DBNull.Value : txtLongitude.Text.Trim());
                        cmd.Parameters.AddWithValue("@CommercialRegister", string.IsNullOrEmpty(txtCommercialRegister.Text) ? (object)DBNull.Value : txtCommercialRegister.Text.Trim());

                        cmd.Parameters.AddWithValue("@H1", string.IsNullOrEmpty(txtHeader1.Text) ? (object)DBNull.Value : txtHeader1.Text.Trim());
                        cmd.Parameters.AddWithValue("@H2", string.IsNullOrEmpty(txtHeader2.Text) ? (object)DBNull.Value : txtHeader2.Text.Trim());
                        cmd.Parameters.AddWithValue("@H3", string.IsNullOrEmpty(txtHeader3.Text) ? (object)DBNull.Value : txtHeader3.Text.Trim());
                        cmd.Parameters.AddWithValue("@Specs", string.IsNullOrEmpty(txtSpecifications.Text) ? (object)DBNull.Value : txtSpecifications.Text.Trim());
                        cmd.Parameters.AddWithValue("@Web", string.IsNullOrEmpty(txtWebsite.Text) ? (object)DBNull.Value : txtWebsite.Text.Trim());

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("تم حفظ بيانات الفرع وترويسة التقارير بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الحفظ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("فتح نافذة بحث الفروع المتاحة.", "بحث", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}