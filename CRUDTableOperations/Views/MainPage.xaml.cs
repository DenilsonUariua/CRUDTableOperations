using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using CRUDTableOperations.Helpers;
using CRUDTableOperations.ViewModels;

namespace CRUDTableOperations.Views
{
	public partial class MainPage : Page
	{
		// Current selected table and connection details
		private string CurrentServer { get; set; }
		private string CurrentDatabase { get; set; }
		private string CurrentTable { get; set; }
		private bool _isWindowsAuth = true;
		// DataTable to track changes
		private DataTable CurrentDataTable { get; set; }

		public MainPage(MainViewModel viewModel)
		{
			InitializeComponent();
			DataContext = viewModel;
			InitializeCRUDButtons();
			DatabaseComboBox.IsEnabled = false;
			TableComboBox.IsEnabled = false;
			TableComboBox.SelectionChanged += TableComboBox_SelectionChanged;
			ConnectButton.IsEnabled = false;
			//DataGridResults.Visibility = Visibility.Collapsed;
			FilterPanel.Visibility = Visibility.Collapsed;
			FilterTextBlock.Visibility = Visibility.Collapsed;
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

		private void InitializeCRUDButtons()
		{
			// Initially disable CRUD buttons
			btnCreate.IsEnabled = false;
			btnUpdate.IsEnabled = false;
			btnDelete.IsEnabled = false;
			btnSave.IsEnabled = false;
			btnCancel.IsEnabled = false;
			btnRefresh.IsEnabled = false;
		}

		public void ServerTextBox_TextChanged(object sender, TextChangedEventArgs e)
			=> ConnectButton.IsEnabled = !string.IsNullOrEmpty(ServerTextBox.Text);

		// Handle Connect Button Click (previous implementation remains the same)
		private void ConnectButton_Click(object sender, RoutedEventArgs e)
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
				if (string.IsNullOrEmpty(txtUsername.Text)) {
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
					MessageBox.Show($"Connected on {server}. The connection status is {connection.State}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

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

					// Enable DatabaseComboBox and disable ConnectButton
					ConnectButton.IsEnabled = false;
					DatabaseComboBox.IsEnabled = true;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		// Handle Database ComboBox Selection Changed
		private void DatabaseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (DatabaseComboBox.SelectedItem == null) return;

			//DataGridResults.Visibility = Visibility.Collapsed;
			FilterPanel.Visibility = Visibility.Collapsed;
			FilterTextBlock.Visibility = Visibility.Collapsed;

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

		// New method to handle table selection and generate DataGrid
		private void TableComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TableComboBox.SelectedItem == null) return;

			try
			{
				// Store current selections
				CurrentServer = ServerTextBox.Text;
				CurrentDatabase = DatabaseComboBox.SelectedItem.ToString();
				CurrentTable = TableComboBox.SelectedItem.ToString();

				// Construct connection string
				string connectionString = $"Server={CurrentServer};Database={CurrentDatabase};Integrated Security=True;";
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

					connectionString = $"Server={CurrentServer};Database={CurrentDatabase};User Id={txtUsername.Text};Password={txtPassword.Password};";

				}
				// Create DataTable to hold query results
				CurrentDataTable = new DataTable();

				// Use SqlDataAdapter to fill the DataTable
				using (var connection = new SqlConnection(connectionString))
				{
					string query = $"SELECT * FROM [{CurrentTable}]";
					SqlDataAdapter adapter = new SqlDataAdapter(query, connection);

					// Create a command builder to help with updates
					SqlCommandBuilder builder = new SqlCommandBuilder(adapter);

					// Fill the DataTable
					adapter.Fill(CurrentDataTable);

					// Set the DataGrid's ItemsSource to the DataTable
					DataGridResults.ItemsSource = CurrentDataTable.DefaultView;

					// Enable CRUD buttons
					btnCreate.IsEnabled = true;
					btnUpdate.IsEnabled = true;
					btnDelete.IsEnabled = true;
					btnRefresh.IsEnabled = true;
				}

				PopulateColumnFilters();
				//DataGridResults.Visibility = Visibility.Visible;
				FilterPanel.Visibility = Visibility.Visible;
				FilterTextBlock.Visibility = Visibility.Visible;

				// Optional: Show number of rows retrieved
				MessageBox.Show($"Retrieved {CurrentDataTable.Rows.Count} rows from {CurrentTable}",
					"Data Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error retrieving table data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		// Create new row
		private void btnCreate_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentDataTable == null)
			{
				MessageBox.Show("Please select a table first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// Add a new row to the DataTable
			DataRow newRow = CurrentDataTable.NewRow();
			CurrentDataTable.Rows.Add(newRow);

			// Scroll to the new row
			DataGridResults.ScrollIntoView(newRow);
			DataGridResults.SelectedItem = newRow;

			// Enable save and cancel buttons
			btnSave.IsEnabled = true;
			btnCancel.IsEnabled = true;
		}

		// Update existing row
		private void btnUpdate_Click(object sender, RoutedEventArgs e)
		{
			if (DataGridResults.SelectedItem == null)
			{
				MessageBox.Show("Please select a row to update.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// Allow editing of the selected row
			DataGridResults.BeginEdit();

			// Enable save and cancel buttons
			btnSave.IsEnabled = true;
			btnCancel.IsEnabled = true;
		}

		// Delete selected row
		private void btnDelete_Click(object sender, RoutedEventArgs e)
		{
			if (DataGridResults.SelectedItem == null)
			{
				MessageBox.Show("Please select a row to delete.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// Confirm deletion
			var result = MessageBox.Show("Are you sure you want to delete the selected row?", "Confirm Deletion",
				MessageBoxButton.YesNo, MessageBoxImage.Question);

			if (result == MessageBoxResult.Yes)
			{
				// Remove the selected row
				((DataRowView)DataGridResults.SelectedItem).Row.Delete();

				// Enable save and cancel buttons
				btnSave.IsEnabled = true;
				btnCancel.IsEnabled = true;
			}
		}

		// Save changes to database
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentDataTable == null)
			{
				MessageBox.Show("No changes to save.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			try
			{
				// Construct connection string
				string connectionString = $"Server={CurrentServer};Database={CurrentDatabase};Integrated Security=True;";
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
					connectionString = $"Server={CurrentServer};Database={CurrentDatabase};User Id={txtUsername.Text};Password={txtPassword.Password};";
				}

				using (var connection = new SqlConnection(connectionString))
				{
					connection.Open();

					// Check for existing primary key
					string keyQuery = @"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + QUOTENAME(CONSTRAINT_NAME)), 'IsPrimaryKey') = 1
                AND TABLE_NAME = @TableName";

					string primaryKey = null;
					using (var keyCommand = new SqlCommand(keyQuery, connection))
					{
						keyCommand.Parameters.AddWithValue("@TableName", CurrentTable);
						using (var reader = keyCommand.ExecuteReader())
						{
							if (reader.Read())
							{
								primaryKey = reader.GetString(0);
							}
						}
					}

					// If no primary key found, ask user to select one
					if (primaryKey == null)
					{
						var selectionWindow = new ColumnSelectionWindow(CurrentDataTable);
						if (selectionWindow.ShowDialog() == true)
						{
							primaryKey = selectionWindow.SelectedColumn;
							DefinePrimaryKey(connection, CurrentTable, primaryKey);
						}
						else
						{
							return; // User cancelled the operation
						}
					}

					// Create SqlDataAdapter with the original query
					string query = $"SELECT * FROM [{CurrentTable}]";
					SqlDataAdapter adapter = new SqlDataAdapter(query, connection);

					// Create CommandBuilder to generate update, insert, and delete commands
					SqlCommandBuilder builder = new SqlCommandBuilder(adapter);

					// Update the database with changes from the DataTable
					adapter.Update(CurrentDataTable);

					// Clear any pending changes
					CurrentDataTable.AcceptChanges();

					// Disable save and cancel buttons
					btnSave.IsEnabled = false;
					btnCancel.IsEnabled = false;

					MessageBox.Show("Changes saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error saving changes: {ex.Message}");
				MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void DefinePrimaryKey(SqlConnection connection, string tableName, string columnName)
		{
			try
			{
				// Step 1: Ensure the column is non-nullable
				string alterColumnQuery = $"ALTER TABLE [{tableName}] ALTER COLUMN [{columnName}] INT NOT NULL";
				using (var alterColumnCommand = new SqlCommand(alterColumnQuery, connection))
				{
					alterColumnCommand.ExecuteNonQuery();
				}

				// Step 2: Add the primary key constraint
				string addPrimaryKeyQuery = $"ALTER TABLE [{tableName}] ADD CONSTRAINT PK_{tableName} PRIMARY KEY ({columnName})";
				using (var addPrimaryKeyCommand = new SqlCommand(addPrimaryKeyQuery, connection))
				{
					addPrimaryKeyCommand.ExecuteNonQuery();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error defining primary key: {ex.Message}");
				throw new InvalidOperationException($"Error defining primary key for table '{tableName}': {ex.Message}", ex);
			}
		}

		private void ApplyFilter(object sender, TextChangedEventArgs e)
		{
			if (CurrentDataTable == null) return;

			// Create a view of the data source
			DataView dataView = new DataView(CurrentDataTable);

			// Build filter string
			string filterString = BuildFilterString();

			try
			{
				// Apply the filter
				dataView.RowFilter = filterString;

				// Update DataGrid with filtered results
				DataGridResults.ItemsSource = dataView;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error applying filter: {ex.Message}", "Filter Error",
					MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Build filter string from multiple filter text boxes
		private string BuildFilterString()
		{
			// Create an array of filter conditions
			var filters = new[]
			{
				BuildSingleColumnFilter(txtFilter1, cmbColumn1),
				BuildSingleColumnFilter(txtFilter2, cmbColumn2),
				BuildSingleColumnFilter(txtFilter3, cmbColumn3)
			};

			// Remove empty conditions and join with AND
			var activeFilters = filters.Where(f => !string.IsNullOrWhiteSpace(f));
			return string.Join(" AND ", activeFilters);
		}

		// Build filter for a single column
		private string BuildSingleColumnFilter(TextBox filterTextBox, ComboBox columnComboBox)
		{
			// If no text or no column selected, return empty
			if (string.IsNullOrWhiteSpace(filterTextBox.Text) ||
				columnComboBox.SelectedItem == null)
				return string.Empty;

			string columnName = columnComboBox.SelectedItem.ToString();
			string filterText = filterTextBox.Text.Trim();

			// Determine column type for appropriate filtering
			DataColumn column = CurrentDataTable.Columns[columnName];

			// Handle different data types
			if (column.DataType == typeof(string))
			{
				// String contains filter (case-insensitive)
				return $"[{columnName}] LIKE '%{filterText}%'";
			}
			else if (column.DataType == typeof(int) ||
					 column.DataType == typeof(decimal) ||
					 column.DataType == typeof(double))
			{
				// Numeric exact match or range
				return $"[{columnName}] = {filterText}";
			}
			else if (column.DataType == typeof(DateTime))
			{
				// Date filtering
				return $"[{columnName}] = '{filterText}'";
			}

			// Default to string contains for unknown types
			return $"[{columnName}] LIKE '%{filterText}%'";
		}

		// Populate column selection ComboBoxes when table is loaded
		private void PopulateColumnFilters()
		{
			if (CurrentDataTable == null) return;

			// Clear existing items
			cmbColumn1.Items.Clear();
			cmbColumn2.Items.Clear();
			cmbColumn3.Items.Clear();

			// Add column names to ComboBoxes
			foreach (DataColumn column in CurrentDataTable.Columns)
			{
				cmbColumn1.Items.Add(column.ColumnName);
				cmbColumn2.Items.Add(column.ColumnName);
				cmbColumn3.Items.Add(column.ColumnName);
			}

			// Enable filter controls
			txtFilter1.IsEnabled = true;
			txtFilter2.IsEnabled = true;
			txtFilter3.IsEnabled = true;
			cmbColumn1.IsEnabled = true;
			cmbColumn2.IsEnabled = true;
			cmbColumn3.IsEnabled = true;
		}

		private void btnRefresh_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentTable == null)
			{
				MessageBox.Show("Please select a table first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			try
			{
				// Construct connection string
				string connectionString = $"Server={CurrentServer};Database={CurrentDatabase};Integrated Security=True;";
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

					connectionString = $"Server={CurrentServer};Database={CurrentDatabase};User Id={txtUsername.Text};Password={txtPassword.Password};";
				}

				// Create new DataTable to hold refreshed data
				CurrentDataTable = new DataTable();

				// Use SqlDataAdapter to fill the DataTable
				using (var connection = new SqlConnection(connectionString))
				{
					string query = $"SELECT * FROM [{CurrentTable}]";
					SqlDataAdapter adapter = new SqlDataAdapter(query, connection);

					// Create a command builder to help with updates
					SqlCommandBuilder builder = new SqlCommandBuilder(adapter);

					// Fill the DataTable
					adapter.Fill(CurrentDataTable);

					// Set the DataGrid's ItemsSource to the refreshed DataTable
					DataGridResults.ItemsSource = CurrentDataTable.DefaultView;

					// Clear filters
					txtFilter1.Clear();
					txtFilter2.Clear();
					txtFilter3.Clear();
					cmbColumn1.SelectedItem = null;
					cmbColumn2.SelectedItem = null;
					cmbColumn3.SelectedItem = null;

					// Repopulate column filters
					PopulateColumnFilters();

					// Disable save and cancel buttons as we have fresh data
					btnSave.IsEnabled = false;
					btnCancel.IsEnabled = false;

					MessageBox.Show($"Table refreshed successfully. Retrieved {CurrentDataTable.Rows.Count} rows from {CurrentTable}",
						"Refresh Complete", MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}

			catch (Exception ex)
			{
				MessageBox.Show($"Error refreshing table data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		// Cancel changes
		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentDataTable != null)
			{
				// Revert any changes
				CurrentDataTable.RejectChanges();

				// Refresh the DataGrid
				DataGridResults.ItemsSource = null;
				DataGridResults.ItemsSource = CurrentDataTable.DefaultView;

				// Disable save and cancel buttons
				btnSave.IsEnabled = false;
				btnCancel.IsEnabled = false;
			}
		}
		private void btnClearFilter_Click(object sender, RoutedEventArgs e)
		{
			// Clear filter text boxes
			txtFilter1.Clear();
			txtFilter2.Clear();
			txtFilter3.Clear();

			// Clear column selections
			cmbColumn1.SelectedItem = null;
			cmbColumn2.SelectedItem = null;
			cmbColumn3.SelectedItem = null;

			// Restore original data source
			if (CurrentDataTable != null)
			{
				DataGridResults.ItemsSource = CurrentDataTable.DefaultView;
			}
		}
	}
}