using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Media.Imaging;
using accounting_project.Infrastructure;

using Microsoft.Win32;

namespace accounting_project
{
    public partial class CompaniesWindow : Window
    {
private readonly string connStr = DbConnectionFactory.ConnectionString;

        private int currentCompanyID = 0;
        private string currentLogoPath = "";

        public CompaniesWindow()
        {
            InitializeComponent();
            LoadCountries();
            LoadFirstCompany();
        }

        // تعبئة قائمة الدول المتاحة
        private void LoadCountries()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT CountryID, CountryName FROM System_Countries";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            cmbCountry.Items.Clear();
                            while (reader.Read())
                            {
                                cmbCountry.Items.Add(new { ID = reader["CountryID"], Name = reader["CountryID"] + " - " + reader["CountryName"] });
                            }
                            cmbCountry.DisplayMemberPath = "Name";
                            cmbCountry.SelectedValuePath = "ID";
                            if (cmbCountry.Items.Count > 0) cmbCountry.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل الدول: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // جلب أول شركة مسجلة
        private void LoadFirstCompany()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT TOP 1 * FROM System_Companies ORDER BY CompanyID ASC";
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
                MessageBox.Show("خطأ أثناء جلب بيانات الشركة: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillFields(SqlDataReader reader)
        {
            currentCompanyID = reader["CompanyID"] != DBNull.Value ? Convert.ToInt32(reader["CompanyID"]) : 0;
            txtCompanyID.Text = currentCompanyID.ToString();

            txtCompanyName.Text = reader["CompanyName"] != DBNull.Value ? reader["CompanyName"].ToString() : "";
            txtForeignName.Text = reader["ForeignName"] != DBNull.Value ? reader["ForeignName"].ToString() : "";
            txtShortName.Text = reader["ShortName"] != DBNull.Value ? reader["ShortName"].ToString() : "";
            txtForeignShort.Text = reader["ForeignShortName"] != DBNull.Value ? reader["ForeignShortName"].ToString() : "";
            txtNotes.Text = reader["Notes"] != DBNull.Value ? reader["Notes"].ToString() : "";
            txtGroupID.Text = reader["GroupID"] != DBNull.Value ? reader["GroupID"].ToString() : "1";

            chkIsMain.IsChecked = reader["IsMain"] != DBNull.Value && Convert.ToBoolean(reader["IsMain"]);

            if (reader["CountryID"] != DBNull.Value && reader["CountryID"] != DBNull.Value)
            {
                cmbCountry.SelectedValue = Convert.ToInt32(reader["CountryID"]);
            }
            else if (cmbCountry.Items.Count > 0)
            {
                cmbCountry.SelectedIndex = 0;
            }

            currentLogoPath = reader["LogoPath"] != DBNull.Value ? reader["LogoPath"].ToString() : "";
            LoadLogoImage(currentLogoPath);
        }

        private void LoadLogoImage(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    imgLogo.Source = bitmap;
                }
                else
                {
                    imgLogo.Source = null;
                }
            }
            catch
            {
                imgLogo.Source = null;
            }
        }

        // زر اختيار شعار الشركة
        private void BtnUploadLogo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";
            if (openFileDialog.ShowDialog() == true)
            {
                currentLogoPath = openFileDialog.FileName;
                LoadLogoImage(currentLogoPath);
            }
        }

        // زر جديد / إضافة شركة جديدة ضمن الوحدة المحاسبية
        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            currentCompanyID = 0;
            txtCompanyID.Text = "(تلقائي)";
            txtCompanyName.Clear();
            txtForeignName.Clear();
            txtShortName.Clear();
            txtForeignShort.Clear();
            txtNotes.Clear();
            txtGroupID.Text = "1";
            chkIsMain.IsChecked = false;
            currentLogoPath = "";
            imgLogo.Source = null;
            txtCompanyName.Focus();
        }

        // زر الحفظ (إضافة أو تعديل)
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCompanyName.Text))
            {
                MessageBox.Show("يرجى إدخال اسم الشركة كحد أدنى.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query = "";

                    if (currentCompanyID == 0)
                    {
                        // إدخال شركة جديدة
                        query = @"INSERT INTO System_Companies (CountryID, CompanyName, ForeignName, ShortName, ForeignShortName, Notes, GroupID, IsMain, LogoPath)
                                  VALUES (@CountryID, @Name, @FName, @SName, @FSName, @Notes, @GroupID, @IsMain, @LogoPath)";
                    }
                    else
                    {
                        // تحديث بيانات الشركة
                        query = @"UPDATE System_Companies SET 
                                  CountryID = @CountryID, CompanyName = @Name, ForeignName = @FName, 
                                  ShortName = @SName, ForeignShortName = @FSName, Notes = @Notes, 
                                  GroupID = @GroupID, IsMain = @IsMain, LogoPath = @LogoPath
                                  WHERE CompanyID = @ID";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (currentCompanyID > 0)
                            cmd.Parameters.AddWithValue("@ID", currentCompanyID);

                        cmd.Parameters.AddWithValue("@CountryID", cmbCountry.SelectedValue != null ? Convert.ToInt32(cmbCountry.SelectedValue) : 1);
                        cmd.Parameters.AddWithValue("@Name", txtCompanyName.Text.Trim());
                        cmd.Parameters.AddWithValue("@FName", string.IsNullOrEmpty(txtForeignName.Text) ? (object)DBNull.Value : txtForeignName.Text.Trim());
                        cmd.Parameters.AddWithValue("@SName", string.IsNullOrEmpty(txtShortName.Text) ? (object)DBNull.Value : txtShortName.Text.Trim());
                        cmd.Parameters.AddWithValue("@FSName", string.IsNullOrEmpty(txtForeignShort.Text) ? (object)DBNull.Value : txtForeignShort.Text.Trim());
                        cmd.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(txtNotes.Text) ? (object)DBNull.Value : txtNotes.Text.Trim());
                        cmd.Parameters.AddWithValue("@GroupID", string.IsNullOrEmpty(txtGroupID.Text) ? 1 : Convert.ToInt32(txtGroupID.Text.Trim()));
                        cmd.Parameters.AddWithValue("@IsMain", chkIsMain.IsChecked ?? false);
                        cmd.Parameters.AddWithValue("@LogoPath", string.IsNullOrEmpty(currentLogoPath) ? (object)DBNull.Value : currentLogoPath);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("تم حفظ بيانات الشركة بنجاح! \n(ملاحظة: قم بإعادة تسجيل الدخول للنظام ليتم ظهور اسم الشركة الجديد في أعلى الواجهة الرئيسية).", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الحفظ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("استعراض قائمة الشركات ضمن الوحدة المحاسبية.", "بحث", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}