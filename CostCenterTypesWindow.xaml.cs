using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using accounting_project.Infrastructure;

namespace accounting_project
{
    public partial class CostCentersWindow : Window
    {
private readonly string connStr = DbConnectionFactory.ConnectionString;

        private DataTable dtCostCenters = null; // ذاكرة مؤقتة لتسريع البحث والشجرة

        public CostCentersWindow()
        {
            InitializeComponent();
            LoadComboBoxes();
            LoadCostCentersTree();
        }

        private void LoadComboBoxes()
        {
            cmbCenterType.Items.Add(new { ID = 1, Name = "رئيسي" });
            cmbCenterType.Items.Add(new { ID = 0, Name = "فرعي (يتأثر بالعمليات)" });
            cmbCenterType.DisplayMemberPath = "Name";
            cmbCenterType.SelectedValuePath = "ID";
            cmbCenterType.SelectedIndex = 1;

            cmbCenterGroup.Items.Add(new { ID = 1, Name = "المجموعة العامة" });
            cmbCenterGroup.DisplayMemberPath = "Name";
            cmbCenterGroup.SelectedValuePath = "ID";
            cmbCenterGroup.SelectedIndex = 0;
        }

        public void LoadCostCentersTree(string searchText = "")
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    if (dtCostCenters == null)
                    {
                        string query = "SELECT CostCenterID, CostCenterCode, CostCenterName, CostCenterNameEn, ParentCostCenterID, IsMain, IsActive, CenterRank, StopReason, StopDate FROM GL_CostCenters ORDER BY CenterRank, CostCenterID";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        dtCostCenters = new DataTable();
                        da.Fill(dtCostCenters);
                    }

                    searchText = searchText.Trim();
                    HashSet<int> visibleIDs = new HashSet<int>();

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        foreach (DataRow row in dtCostCenters.Rows)
                        {
                            int id = Convert.ToInt32(row["CostCenterID"]);
                            string code = row["CostCenterCode"].ToString();
                            string name = row["CostCenterName"]?.ToString() ?? "";
                            string nameEn = row["CostCenterNameEn"]?.ToString() ?? "";

                            if (code.Contains(searchText) || name.Contains(searchText) || nameEn.Contains(searchText))
                            {
                                AddWithAncestors(id, visibleIDs);
                            }
                        }
                    }

                    tvCostCenters.Items.Clear();

                    DataRow[] parentRows = dtCostCenters.Select("ParentCostCenterID IS NULL");
                    foreach (DataRow row in parentRows)
                    {
                        int id = Convert.ToInt32(row["CostCenterID"]);
                        if (!string.IsNullOrEmpty(searchText) && !visibleIDs.Contains(id))
                            continue;

                        TreeViewItem parentItem = new TreeViewItem();
                        parentItem.Header = $"{row["CostCenterCode"]} - {row["CostCenterName"]}";
                        parentItem.Tag = id;
                        parentItem.IsExpanded = !string.IsNullOrEmpty(searchText);

                        AddChildCentersFiltered(parentItem, dtCostCenters, id, searchText, visibleIDs);
                        tvCostCenters.Items.Add(parentItem);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل الشجرة: " + ex.Message);
            }
        }

        private void AddWithAncestors(int centerID, HashSet<int> visibleIDs)
        {
            if (visibleIDs.Contains(centerID)) return;
            visibleIDs.Add(centerID);

            if (dtCostCenters != null)
            {
                DataRow[] rows = dtCostCenters.Select($"CostCenterID = {centerID}");
                if (rows.Length > 0 && rows[0]["ParentCostCenterID"] != DBNull.Value)
                {
                    int parentID = Convert.ToInt32(rows[0]["ParentCostCenterID"]);
                    AddWithAncestors(parentID, visibleIDs);
                }
            }
        }

        private void AddChildCentersFiltered(TreeViewItem parentItem, DataTable dt, int parentID, string searchText, HashSet<int> visibleIDs)
        {
            DataRow[] childRows = dt.Select($"ParentCostCenterID = {parentID}");
            foreach (DataRow row in childRows)
            {
                int id = Convert.ToInt32(row["CostCenterID"]);
                if (!string.IsNullOrEmpty(searchText) && !visibleIDs.Contains(id))
                    continue;

                TreeViewItem childItem = new TreeViewItem();
                childItem.Header = $"{row["CostCenterCode"]} - {row["CostCenterName"]}";
                childItem.Tag = id;
                childItem.IsExpanded = !string.IsNullOrEmpty(searchText);

                AddChildCentersFiltered(childItem, dt, id, searchText, visibleIDs);
                parentItem.Items.Add(childItem);
            }
        }

        private void TxtSearchCostCenter_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadCostCentersTree(txtSearchCostCenter.Text);
        }

        private void TvCostCenters_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (tvCostCenters.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag != null)
            {
                int centerID = Convert.ToInt32(selectedItem.Tag);
                FetchCenterDetails(centerID);
            }
        }

        private void FetchCenterDetails(int centerID)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT * FROM GL_CostCenters WHERE CostCenterID = @ID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", centerID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtCenterCode.Text = reader["CostCenterCode"]?.ToString() ?? "";
                                txtCenterNameAr.Text = reader["CostCenterName"]?.ToString() ?? "";
                                txtCenterNameEn.Text = reader["CostCenterNameEn"]?.ToString() ?? "";
                                txtRank.Text = reader["CenterRank"]?.ToString() ?? "1";

                                object parentIdObj = reader["ParentCostCenterID"];
                                txtParentCode.Text = parentIdObj != DBNull.Value ? parentIdObj.ToString() : "";

                                bool isMain = reader["IsMain"] != DBNull.Value && Convert.ToBoolean(reader["IsMain"]);
                                cmbCenterType.SelectedValue = isMain ? 1 : 0;

                                bool isActive = reader["IsActive"] == DBNull.Value || Convert.ToBoolean(reader["IsActive"]);
                                chkStopTransactions.IsChecked = !isActive;

                                txtStopReason.Text = reader["StopReason"]?.ToString() ?? "";
                                txtStopDate.Text = reader["StopDate"] != DBNull.Value ? Convert.ToDateTime(reader["StopDate"]).ToString("yyyy-MM-dd") : "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء جلب التفاصيل: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            if (tvCostCenters.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag != null)
            {
                txtParentCode.Text = selectedItem.Tag.ToString();
            }
            else
            {
                txtParentCode.Clear();
            }

            txtCenterCode.Clear();
            txtCenterNameAr.Clear();
            txtCenterNameEn.Clear();
            txtRank.Text = "1";
            chkStopTransactions.IsChecked = false;
            txtStopReason.Clear();
            txtStopDate.Clear();
            txtCenterCode.Focus();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCenterCode.Text) || string.IsNullOrWhiteSpace(txtCenterNameAr.Text))
            {
                MessageBox.Show("يرجى إدخال رقم واسم مركز التكلفة كحد أدنى.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    bool isMain = (int)(cmbCenterType.SelectedValue ?? 0) == 1;
                    bool isActive = !(chkStopTransactions.IsChecked ?? false);
                    object parentId = string.IsNullOrEmpty(txtParentCode.Text) ? (object)DBNull.Value : Convert.ToInt32(txtParentCode.Text);
                    int rank = int.TryParse(txtRank.Text, out int r) ? r : 1;
                    object stopDate = (chkStopTransactions.IsChecked ?? false) ? (object)DateTime.Now : DBNull.Value;

                    string checkQuery = "SELECT CostCenterID FROM GL_CostCenters WHERE CostCenterCode = @Code";
                    int? existingID = null;
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Code", txtCenterCode.Text.Trim());
                        object res = checkCmd.ExecuteScalar();
                        if (res != null) existingID = Convert.ToInt32(res);
                    }

                    string query = "";
                    if (existingID.HasValue)
                    {
                        query = @"UPDATE GL_CostCenters 
                                  SET CostCenterName = @Name, CostCenterNameEn = @NameEn, ParentCostCenterID = @ParentID, 
                                      IsMain = @IsMain, IsActive = @IsActive, CenterRank = @Rank, StopReason = @Reason, StopDate = @StopDate
                                  WHERE CostCenterCode = @Code";
                    }
                    else
                    {
                        query = @"INSERT INTO GL_CostCenters (CostCenterCode, CostCenterName, CostCenterNameEn, ParentCostCenterID, IsMain, IsActive, CenterRank, StopReason, StopDate)
                                  VALUES (@Code, @Name, @NameEn, @ParentID, @IsMain, @IsActive, @Rank, @Reason, @StopDate)";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", txtCenterCode.Text.Trim());
                        cmd.Parameters.AddWithValue("@Name", txtCenterNameAr.Text.Trim());
                        cmd.Parameters.AddWithValue("@NameEn", string.IsNullOrEmpty(txtCenterNameEn.Text) ? (object)DBNull.Value : txtCenterNameEn.Text.Trim());
                        cmd.Parameters.AddWithValue("@ParentID", parentId);
                        cmd.Parameters.AddWithValue("@IsMain", isMain);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@Rank", rank);
                        cmd.Parameters.AddWithValue("@Reason", string.IsNullOrEmpty(txtStopReason.Text) ? (object)DBNull.Value : txtStopReason.Text.Trim());
                        cmd.Parameters.AddWithValue("@StopDate", stopDate);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("تم حفظ مركز التكلفة بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                dtCostCenters = null;
                LoadCostCentersTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الحفظ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCenterCode.Text)) return;

            if (MessageBox.Show("هل أنت متأكد من حذف مركز التكلفة هذا؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connStr))
                    {
                        conn.Open();
                        string query = "DELETE FROM GL_CostCenters WHERE CostCenterCode = @Code";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@Code", txtCenterCode.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("تم حذف مركز التكلفة بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    dtCostCenters = null;
                    LoadCostCentersTree();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ أثناء الحذف: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}