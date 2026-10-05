using System;
using System.Configuration;
using System.Data.SqlClient;

namespace accounting_project.Infrastructure
{
    public static class DbConnectionFactory
    {
        private const string ConnectionName = "AccountingDb";

        public static string ConnectionString
        {
            get
            {
                var configured = ConfigurationManager.ConnectionStrings[ConnectionName];
                if (configured != null && !string.IsNullOrWhiteSpace(configured.ConnectionString))
                    return configured.ConnectionString;

                // إعداد محلي بسيط للتطوير، ويمكن تغييره من App.config دون إعادة بناء البرنامج.
                return @"Data Source=.\SQLEXPRESS;Initial Catalog=AccountingDB;Integrated Security=True;MultipleActiveResultSets=True;";
            }
        }

        public static SqlConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public static void Validate()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new InvalidOperationException("لم يتم إعداد اتصال قاعدة بيانات AccountingDb.");
        }
    }
}
