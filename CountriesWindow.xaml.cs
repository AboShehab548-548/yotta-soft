using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using accounting_project.Infrastructure;

namespace accounting_project
{
    public partial class CountriesWindow : Window
    {
        private readonly string connStr = DbConnectionFactory.ConnectionString;

        private int currentCountryID = 0;

        public CountriesWindow()
        {
            InitializeComponent();
            LoadFirstCountry();
        }

        // جلب أول دولة عند فتح الشاشة
        private void LoadFirstCountry()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT TOP 1 * FROM System_Countries ORDER BY CountryID ASC";
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
                MessageBox.Show("خطأ أثناء جلب البيانات: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillFields(SqlDataReader reader)
        {
            currentCountryID = Convert.ToInt32(reader["CountryID"]);
            txtCountryID.Text = currentCountryID.ToString();
            txtCountryName.Text = reader["CountryName"]?.ToString();
            txtForeignName.Text = reader["ForeignName"]?.ToString();
            txtCountryShortName.Text = reader["CountryShortName"]?.ToString();
            txtLocalCurrency.Text = reader["LocalCurrency"]?.ToString();
            txtRegionID.Text = reader["RegionID"] != DBNull.Value ? reader["RegionID"].ToString() : "";
            txtContinent.Text = reader["Continent"]?.ToString();
            txtNationality.Text = reader["Nationality"]?.ToString();
            txtShortName.Text = reader["ShortName"]?.ToString();
            txtSubForeign.Text = reader["ForeignShortName"]?.ToString(); // تمثيل تقريبي للاختصار الأجنبي
            txtCountryCode.Text = reader["CountryCode"]?.ToString();
            txtPhoneKey.Text = reader["PhoneKey"]?.ToString();
            txtLanguage.Text = reader["LanguageCode"]?.ToString();
            chkBlockDealing.IsChecked = Convert.ToBoolean(reader["BlockDealing"]);
        }

        // زر جديد (تفريغ الحقول للإضافة)
        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            currentCountryID = 0;
            txtCountryID.Text = "(تلقائي)";
            txtCountryName.Clear();
            txtForeignName.Clear();
            txtCountryShortName.Clear();
            txtLocalCurrency.Clear();
            txtRegionID.Clear();
            txtContinent.Clear();
            txtNationality.Clear();
            txtShortName.Clear();
            txtSubForeign.Clear();
            txtCountryCode.Clear();
            txtPhoneKey.Clear();
            txtLanguage.Clear();
            chkBlockDealing.IsChecked = false;
            txtCountryName.Focus();
        }

        // زر الحفظ (إضافة أو تعديل)
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCountryName.Text))
            {
                MessageBox.Show("يرجى إدخال اسم الدولة كحد أدنى.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query = "";

                    if (currentCountryID == 0)
                    {
                        // إدخال جديد
                        query = @"INSERT INTO System_Countries (CountryName, ForeignName, CountryShortName, LocalCurrency, RegionID, Continent, Nationality, ShortName, ForeignShortName, CountryCode, PhoneKey, LanguageCode, BlockDealing)
                                  VALUES (@Name, @FName, @CShort, @Currency, @Region, @Continent, @Nationality, @SName, @FSName, @CCode, @PKey, @Lang, @Block)";
                    }
                    else
                    {
                        // تحديث
                        query = @"UPDATE System_Countries SET 
                                  CountryName = @Name, ForeignName = @FName, CountryShortName = @CShort, LocalCurrency = @Currency, 
                                  RegionID = @Region, Continent = @Continent, Nationality = @Nationality, ShortName = @SName, 
                                  ForeignShortName = @FSName, CountryCode = @CCode, PhoneKey = @PKey, LanguageCode = @Lang, BlockDealing = @Block
                                  WHERE CountryID = @ID";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (currentCountryID > 0)
                            cmd.Parameters.AddWithValue("@ID", currentCountryID);

                        cmd.Parameters.AddWithValue("@Name", txtCountryName.Text.Trim());
                        cmd.Parameters.AddWithValue("@FName", string.IsNullOrEmpty(txtForeignName.Text) ? (object)DBNull.Value : txtForeignName.Text.Trim());
                        cmd.Parameters.AddWithValue("@CShort", string.IsNullOrEmpty(txtCountryShortName.Text) ? (object)DBNull.Value : txtCountryShortName.Text.Trim());
                        cmd.Parameters.AddWithValue("@Currency", string.IsNullOrEmpty(txtLocalCurrency.Text) ? (object)DBNull.Value : txtLocalCurrency.Text.Trim());
                        cmd.Parameters.AddWithValue("@Region", string.IsNullOrEmpty(txtRegionID.Text) ? (object)DBNull.Value : Convert.ToInt32(txtRegionID.Text.Trim()));
                        cmd.Parameters.AddWithValue("@Continent", string.IsNullOrEmpty(txtContinent.Text) ? (object)DBNull.Value : txtContinent.Text.Trim());
                        cmd.Parameters.AddWithValue("@Nationality", string.IsNullOrEmpty(txtNationality.Text) ? (object)DBNull.Value : txtNationality.Text.Trim());
                        cmd.Parameters.AddWithValue("@SName", string.IsNullOrEmpty(txtShortName.Text) ? (object)DBNull.Value : txtShortName.Text.Trim());
                        cmd.Parameters.AddWithValue("@FSName", string.IsNullOrEmpty(txtSubForeign.Text) ? (object)DBNull.Value : txtSubForeign.Text.Trim());
                        cmd.Parameters.AddWithValue("@CCode", string.IsNullOrEmpty(txtCountryCode.Text) ? (object)DBNull.Value : txtCountryCode.Text.Trim());
                        cmd.Parameters.AddWithValue("@PKey", string.IsNullOrEmpty(txtPhoneKey.Text) ? (object)DBNull.Value : txtPhoneKey.Text.Trim());
                        cmd.Parameters.AddWithValue("@Lang", string.IsNullOrEmpty(txtLanguage.Text) ? (object)DBNull.Value : txtLanguage.Text.Trim());
                        cmd.Parameters.AddWithValue("@Block", chkBlockDealing.IsChecked ?? false);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("تم حفظ بيانات الدولة بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الحفظ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // زر البحث (محاكاة قائمة الدول المنسدلة للبحث واختيار الدولة)
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("ميزة البحث السريع: تتيح لك كتابة جزء من اسم الدولة واستعراض القائمة تماماً كما في أنظمة الأونكس ERP.", "بحث الدول", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}