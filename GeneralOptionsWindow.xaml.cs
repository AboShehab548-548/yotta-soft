using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using accounting_project.Infrastructure;

namespace accounting_project
{
    public partial class GeneralOptionsWindow : Window
    {
private readonly string connStr = DbConnectionFactory.ConnectionString;

        public GeneralOptionsWindow()
        {
            InitializeComponent();
            LoadGeneralOptions();
        }

        // جلب القيم الحالية من قاعدة البيانات وعرضها في الشاشة
        private void LoadGeneralOptions()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT TOP 1 SubAccountRank, UseForeignCurrencies, DebitCreditSequence, EmployeeNameFormat, MinEmpIDLength, MaxEmpIDLength FROM System_GeneralOptions";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtSubAccountRank.Text = reader["SubAccountRank"].ToString();
                                chkUseForeignCurrencies.IsChecked = Convert.ToBoolean(reader["UseForeignCurrencies"]);

                                int seq = Convert.ToInt32(reader["DebitCreditSequence"]);
                                cmbDebitCreditSequence.SelectedIndex = (seq > 0) ? seq - 1 : 0;

                                int empFormat = Convert.ToInt32(reader["EmployeeNameFormat"]);
                                chkEmpTitleAtStart.IsChecked = (empFormat == 1);

                                txtMinEmpID.Text = reader["MinEmpIDLength"] != DBNull.Value ? reader["MinEmpIDLength"].ToString() : "";
                                txtMaxEmpID.Text = reader["MaxEmpIDLength"] != DBNull.Value ? reader["MaxEmpIDLength"].ToString() : "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء جلب المتغيرات العامة: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // حفظ أو تحديث المتغيرات العامة
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSubAccountRank.Text))
            {
                MessageBox.Show("يرجى تحديد رتبة الحساب الفرعي كحد أدنى.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    // نستخدم تحديث السجل الأول (بما أن شاشة المتغيرات العامة عادة صف إعدادات واحد رئيسي للمنشأة)
                    string query = @"UPDATE System_GeneralOptions 
                                     SET SubAccountRank = @SubAccountRank, 
                                         UseForeignCurrencies = @UseForeignCurrencies, 
                                         DebitCreditSequence = @DebitCreditSequence, 
                                         EmployeeNameFormat = @EmployeeNameFormat, 
                                         MinEmpIDLength = @MinEmpID, 
                                         MaxEmpIDLength = @MaxEmpID;";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SubAccountRank", Convert.ToInt32(txtSubAccountRank.Text.Trim()));
                        cmd.Parameters.AddWithValue("@UseForeignCurrencies", chkUseForeignCurrencies.IsChecked ?? false);
                        cmd.Parameters.AddWithValue("@DebitCreditSequence", cmbDebitCreditSequence.SelectedIndex + 1);
                        cmd.Parameters.AddWithValue("@EmployeeNameFormat", (chkEmpTitleAtStart.IsChecked ?? false) ? 1 : 0);
                        cmd.Parameters.AddWithValue("@MinEmpID", string.IsNullOrEmpty(txtMinEmpID.Text) ? (object)DBNull.Value : Convert.ToInt32(txtMinEmpID.Text.Trim()));
                        cmd.Parameters.AddWithValue("@MaxEmpID", string.IsNullOrEmpty(txtMaxEmpID.Text) ? (object)DBNull.Value : Convert.ToInt32(txtMaxEmpID.Text.Trim()));

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("تم حفظ المتغيرات العامة وتحديث تأثيرات النظام بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الحفظ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}