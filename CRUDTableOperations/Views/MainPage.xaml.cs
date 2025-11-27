using CRUDTableOperations.ViewModels;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace CRUDTableOperations.Views
{
    public partial class MainPage : Page
    {
        // Current selected table and connection details
        private bool _isWindowsAuth = true;
        private Dictionary<string, Window> openTableWindows = new Dictionary<string, Window>();

        // Track connection state
        private bool _isConnected = false;

        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            DatabaseComboBox.IsEnabled = false;
            TableComboBox.IsEnabled = false;
            TableComboBox.SelectionChanged += TableComboBox_SelectionChanged;
            ConnectButton.IsEnabled = false;

            // Initialize connection status
            UpdateConnectionStatus(false);
        }

        // Handle authentication method radio button change
        private void AuthMethod_Checked(object sender, RoutedEventArgs e)
        {
            if (rdoWindowsAuth.IsChecked == true)
            {
                _isWindowsAuth = true;

                if (SqlAuthPanel == null) return;

                SqlAuthPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                _isWindowsAuth = false;
                SqlAuthPanel.Visibility = Visibility.Visible;
            }
        }


        public void ServerTextBox_TextChanged(object sender, TextChangedEventArgs e)
            => ConnectButton.IsEnabled = !string.IsNullOrEmpty(ServerTextBox.Text);

        // Handle Connect/Disconnect Button Click
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnected)
            {
                // Disconnect
                DisconnectFromServer();
            }
            else
            {
                // Connect
                ConnectToServer();
            }
        }

        private void ConnectToServer()
        {
            if (string.IsNullOrEmpty(ServerTextBox.Text))
            {
                MessageBox.Show("Please enter the server name.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var server = ServerTextBox.Text;
            string connectionString = $"Server={server};Integrated Security=True;";

            if (!_isWindowsAuth)
            {
                if (string.IsNullOrEmpty(txtUsername.Text))
                {
                    MessageBox.Show($"Please enter a user name", "User name missing", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(txtPassword.Password))
                {
                    MessageBox.Show($"Please enter a password", "Password Missing", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                connectionString = $"Server={server};User Id={txtUsername.Text};Password={txtPassword.Password};";
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Populate databases
                    DatabaseComboBox.Items.Clear();
                    using (var command = new SqlCommand("SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name ASC", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DatabaseComboBox.Items.Add(reader.GetString(0));
                            }
                        }
                    }

                    // Update connection state
                    _isConnected = true;
                    UpdateConnectionStatus(true);

                    // Enable DatabaseComboBox and disable connection fields
                    DatabaseComboBox.IsEnabled = true;
                    ServerTextBox.IsEnabled = false;
                    rdoWindowsAuth.IsEnabled = false;
                    rdoSqlAuth.IsEnabled = false;
                    txtUsername.IsEnabled = false;
                    txtPassword.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateConnectionStatus(false);
            }
        }

        private void DisconnectFromServer()
        {
            var result = MessageBox.Show(
                "Are you sure you want to disconnect? All open table windows will be closed.",
                "Confirm Disconnect",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Close all open table windows
                var windowsToClose = openTableWindows.Values.ToList();
                foreach (var window in windowsToClose)
                {
                    window?.Close();
                }
                openTableWindows.Clear();

                // Reset UI state
                DatabaseComboBox.Items.Clear();
                TableComboBox.Items.Clear();
                DatabaseComboBox.IsEnabled = false;
                TableComboBox.IsEnabled = false;

                // Re-enable connection fields
                ServerTextBox.IsEnabled = true;
                rdoWindowsAuth.IsEnabled = true;
                rdoSqlAuth.IsEnabled = true;
                txtUsername.IsEnabled = true;
                txtPassword.IsEnabled = true;

                // Update connection state
                _isConnected = false;
                UpdateConnectionStatus(false);
            }
        }

        private void UpdateConnectionStatus(bool isConnected)
        {
            if (isConnected)
            {
                ConnectButton.Content = "Disconnect";
                ConnectButton.Background = System.Windows.Media.Brushes.DarkRed;
                ConnectionStatusBadge.Fill = System.Windows.Media.Brushes.LimeGreen;
                ConnectionStatusText.Text = "Connected";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;
            }
            else
            {
                ConnectButton.Content = "Connect";
                ConnectButton.Background = (System.Windows.Media.Brush)FindResource("MahApps.Brushes.Accent");
                ConnectionStatusBadge.Fill = System.Windows.Media.Brushes.Red;
                ConnectionStatusText.Text = "Disconnected";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }


        // Handle Database ComboBox Selection Changed
        private void DatabaseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatabaseComboBox.SelectedItem == null) return;

            string connectionString = $"Server={ServerTextBox.Text};Database={DatabaseComboBox.SelectedItem};Integrated Security=True;";
            if (!_isWindowsAuth)
            {
                if (string.IsNullOrEmpty(txtUsername.Text))
                {
                    MessageBox.Show($"Please enter a user name", "User name missing", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(txtPassword.Password))
                {
                    MessageBox.Show($"Please enter a password", "Password Missing", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                connectionString = $"Server={ServerTextBox.Text};Database={DatabaseComboBox.SelectedItem};User Id={txtUsername.Text};Password={txtPassword.Password};";
            }

            try
            {
                // Populate tables
                TableComboBox.Items.Clear();
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand("SELECT name FROM sys.tables ORDER BY name ASC", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                TableComboBox.Items.Add(reader.GetString(0));
                            }
                        }
                    }

                    // Enable TableComboBox
                    TableComboBox.IsEnabled = true;
                    MessageBox.Show($"Connected to {DatabaseComboBox.SelectedItem}. Select your table to continue.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Modified TableComboBox_SelectionChanged to open new window
        private void TableComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TableComboBox.SelectedItem == null) return;

            try
            {
                string selectedTable = TableComboBox.SelectedItem.ToString();
                string windowKey = $"{DatabaseComboBox.SelectedItem}_{selectedTable}";

                // Check if window is already open
                if (openTableWindows.ContainsKey(windowKey) && openTableWindows[windowKey] != null)
                {
                    // Bring existing window to front
                    openTableWindows[windowKey].Activate();
                    openTableWindows[windowKey].Focus();
                    return;
                }

                // Create new window for the table
                var tableWindow = new TableWindow(
                    ServerTextBox.Text,
                    DatabaseComboBox.SelectedItem.ToString(),
                    selectedTable,
                    _isWindowsAuth,
                    txtUsername.Text,
                    txtPassword.Password
                );

                // Track the window
                openTableWindows[windowKey] = tableWindow;

                // Remove from dictionary when window closes
                tableWindow.Closed += (s, args) =>
                {
                    openTableWindows.Remove(windowKey);
                };

                tableWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening table window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}