using System.Windows;

namespace AccountingSystem.UI
{
    public partial class SupervisorApprovalDialog : Window
    {
        public bool IsApproved { get; private set; } = false;

        public SupervisorApprovalDialog()
        {
            InitializeComponent();
            txtUsername.Focus();
        }

        private void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            // تحقق بسيط من بيانات المشرف (يمكن ربطه بقاعدة البيانات)
            if (txtUsername.Text == "admin" && txtPassword.Password == "1234")
            {
                IsApproved = true;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("اسم المستخدم أو كلمة المرور غير صحيحة!", "خطأ اعتمادات", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            IsApproved = false;
            DialogResult = false;
            Close();
        }
    }
}