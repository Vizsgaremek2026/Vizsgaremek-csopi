using System.Windows;

namespace AdminApp
{
    public partial class MainWindow : Window
    {
        private const string AdminPassword = "admin123";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Password == AdminPassword)
            {
                new AdminWindow().Show();
                Close();
            }
            else
            {
                ErrorText.Visibility = Visibility.Visible;
            }
        }
    }
}

