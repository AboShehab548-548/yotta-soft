using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace AccountingSystem.UI
{
    public static class AuthorizationManager
    {
        // قائمة الصلاحيات الخاصة بالمستخدم الحالي
        public static HashSet<string> CurrentUserPermissions { get; set; } = new HashSet<string>();
        public static bool HasPermission(int roleId, string permissionKey)
        {
            return HasPermission(permissionKey);
        }
        // جلب صلاحيات الدور عند تسجيل الدخول
        public static void LoadRolePermissions(int roleId, string connectionString)
        {
            CurrentUserPermissions.Clear();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT p.PermissionKey 
                    FROM Permissions p
                    INNER JOIN RolePermissions rp ON p.PermissionId = rp.PermissionId
                    WHERE rp.RoleId = @RoleId";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@RoleId", roleId);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        CurrentUserPermissions.Add(reader["PermissionKey"].ToString());
                    }
                }
            }
        }

        // فحص وجود الصلاحية
        public static bool HasPermission(string permissionKey)
        {
            return CurrentUserPermissions.Contains(permissionKey);
        }

        // دالة التحقق من كلمة سر المشرف لإجراء حرج (مثل إلغاء فاتورة)
        public static bool VerifySupervisorPassword(string password, string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT COUNT(1) 
                    FROM Users u
                    INNER JOIN Roles r ON u.RoleId = r.RoleId
                    WHERE u.PasswordHash = @Password AND r.RoleName = N'مدير النظام'";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Password", password); // يُفضل التشفير بـ SHA256

                conn.Open();
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }
    }
}