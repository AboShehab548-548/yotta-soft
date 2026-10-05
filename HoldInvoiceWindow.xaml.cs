using System.Windows;
using System.Windows.Input;

namespace AccountingSystem.UI
{
    public partial class HoldInvoiceWindow : Window
    {
        public string CustomerName { get; private set; } = string.Empty;

        public HoldInvoiceWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => txtCustomerName.Focus(); // توجيه التركيز بعد اكتمال تحميل عناصر النافذة
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            CustomerName = txtCustomerName.Text.Trim();
            this.DialogResult = true; // يغلق النافذة تلقائياً ويُرجع true
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // يغلق النافذة تلقائياً ويُرجع false (سطر واحد يكفي)
        }

        private void TxtCustomerName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnConfirm_Click(sender, e);
            }
        }
    }
}