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

		// Add pagination properties
		private int CurrentPage { get; set; } = 1;
		private const int PageSize = 15;
		private int TotalRecords { get; set; }

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

		private void LoadPagedData( bool showMessage = false)
		{
			string connectionString = GetConnectionString();
			CurrentDataTable = new DataTable();

			using (var connection = new SqlConnection(connectionString))
			{
				// First, get total count
				string countQuery = $"SELECT COUNT(*) FROM [{CurrentTable}]";
				using (var countCommand = new SqlCommand(countQuery, connection))
				{
					connection.Open();
					TotalRecords = (int)countCommand.ExecuteScalar();
				}

				// Then get paged data
				string query = $@"
                SELECT *
                FROM (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS RowNum
                    FROM [{CurrentTable}]
                ) AS Paged
                WHERE RowNum BETWEEN ({CurrentPage - 1} * {PageSize} + 1) AND ({CurrentPage} * {PageSize})";

				SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
				SqlCommandBuilder builder = new SqlCommandBuilder(adapter);
				adapter.Fill(CurrentDataTable);

				DataGridResults.ItemsSource = CurrentDataTable.DefaultView;

				// Enable CRUD buttons
				btnCreate.IsEnabled = true;
				btnUpdate.IsEnabled = true;
				btnDelete.IsEnabled = true;
				btnRefresh.IsEnabled = true;

				// Update status message
				UpdatePaginationStatus(showMessage);
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

		// Modify TableComboBox_SelectionChanged method
		private void TableComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TableComboBox.SelectedItem == null) return;

			try
			{
				CurrentServer = ServerTextBox.Text;
				CurrentDatabase = DatabaseComboBox.SelectedItem.ToString();
				CurrentTable = TableComboBox.SelectedItem.ToString();
				CurrentPage = 1; // Reset to first page when table changes

				LoadPagedData(true);

				PopulateColumnFilters();
				FilterPanel.Visibility = Visibility.Visible;
				FilterTextBlock.Visibility = Visibility.Visible;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error retrieving table data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}


		// Add method to get connection string
		private string GetConnectionString()
		{
			string connectionString = $"Server={CurrentServer};Database={CurrentDatabase};Integrated Security=True;";
			if (!_isWindowsAuth)
			{
				if (string.IsNullOrEmpty(txtUsername.Text) || string.IsNullOrEmpty(txtPassword.Password))
				{
					throw new InvalidOperationException("Username and password are required for SQL Server authentication.");
				}
				connectionString = $"Server={CurrentServer};Database={CurrentDatabase};User Id={txtUsername.Text};Password={txtPassword.Password};";
			}
			return connectionString;
		}

		// Add method to update pagination status
		private void UpdatePaginationStatus(bool showMessage = false)
		{
			int totalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
			if (showMessage)
			{
				MessageBox.Show($"Showing page {CurrentPage} of {totalPages} (Total records: {TotalRecords})",
					"Data Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		// Add navigation methods
		private void btnNextPage_Click(object sender, RoutedEventArgs e)
		{
			int totalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
			if (CurrentPage < totalPages)
			{
				CurrentPage++;
				LoadPagedData();
			}
		}

		private void btnPreviousPage_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentPage > 1)
			{
				CurrentPage--;
				LoadPagedData();
			}
		}

		//// Modify the refresh method
		//private void btnRefresh_Click(object sender, RoutedEventArgs e)
		//{
		//	if (CurrentTable == null)
		//	{
		//		MessageBox.Show("Please select a table first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
		//		return;
		//	}

		//	try
		//	{
		//		LoadPagedData();

		//		// Clear filters
		//		txtFilter1.Clear();
		//		txtFilter2.Clear();
		//		txtFilter3.Clear();
		//		cmbColumn1.SelectedItem = null;
		//		cmbColumn2.SelectedItem = null;
		//		cmbColumn3.SelectedItem = null;

		//		PopulateColumnFilters();
		//		btnSave.IsEnabled = false;
		//		btnCancel.IsEnabled = false;
		//	}
		//	catch (Exception ex)
		//	{
		//		MessageBox.Show($"Error refreshing table data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		//	}
		//}

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

			try
			{
				// Reset to first page when applying new filters
				CurrentPage = 1;
				LoadFilteredData();
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error applying filter: {ex}");
				MessageBox.Show($"Error applying filter: {ex.Message}", "Filter Error",
					MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		public static SqlParameter CloneSqlParameter(SqlParameter parameter)
		{
			return new SqlParameter
			{
				ParameterName = parameter.ParameterName,
				SqlDbType = parameter.SqlDbType,
				Direction = parameter.Direction,
				IsNullable = parameter.IsNullable,
				Size = parameter.Size,
				Precision = parameter.Precision,
				Scale = parameter.Scale,
				SourceColumn = parameter.SourceColumn,
				SourceVersion = parameter.SourceVersion,
				Value = parameter.Value
			};
		}


		private void LoadFilteredData()
		{
			string connectionString = GetConnectionString();
			CurrentDataTable = new DataTable();

			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();

				// Get filter conditions and parameters
				var (whereClause, parameters) = BuildDatabaseFilterString();

				// Get total filtered count
				string countQuery = $"SELECT COUNT(*) FROM [{CurrentTable}]";
				if (!string.IsNullOrWhiteSpace(whereClause))
				{
					countQuery += $" WHERE {whereClause}";
				}

				using (var countCommand = new SqlCommand(countQuery, connection))
				{
					// Add parameters to count command
					foreach (SqlParameter parameter in parameters)
					{
						var clonedParameter = CloneSqlParameter(parameter);
						countCommand.Parameters.Add(clonedParameter);
					}
					TotalRecords = (int)countCommand.ExecuteScalar();
				}

				// Then get paged data with filters
				string query = $@"
            SELECT *
            FROM (
                SELECT *, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS RowNum
                FROM [{CurrentTable}]
                {(string.IsNullOrWhiteSpace(whereClause) ? "" : $"WHERE {whereClause}")}
            ) AS Paged
            WHERE RowNum BETWEEN ({CurrentPage - 1} * {PageSize} + 1) AND ({CurrentPage} * {PageSize})";

				using (var command = new SqlCommand(query, connection))
				{
					// Add parameters to main query command
					foreach (SqlParameter parameter in parameters)
					{
						var clonedParameter = CloneSqlParameter(parameter);
						command.Parameters.Add(clonedParameter);
					}

					SqlDataAdapter adapter = new SqlDataAdapter(command);
					adapter.Fill(CurrentDataTable);
				}

				DataGridResults.ItemsSource = CurrentDataTable.DefaultView;
				UpdatePaginationStatus();
			}
		}

		private (string whereClause, List<SqlParameter> parameters) BuildDatabaseFilterString()
		{
			var filterConditions = new List<string>();
			var parameters = new List<SqlParameter>();

			AddFilterCondition(filterConditions, parameters, txtFilter1, cmbColumn1);
			AddFilterCondition(filterConditions, parameters, txtFilter2, cmbColumn2);
			AddFilterCondition(filterConditions, parameters, txtFilter3, cmbColumn3);

			string whereClause = string.Join(" AND ", filterConditions.Where(f => !string.IsNullOrWhiteSpace(f)));
			return (whereClause, parameters);
		}

		private void AddFilterCondition(List<string> conditions, List<SqlParameter> parameters, TextBox filterTextBox, ComboBox columnComboBox)
		{
			if (string.IsNullOrWhiteSpace(filterTextBox.Text) || columnComboBox.SelectedItem == null)
				return;

			string columnName = columnComboBox.SelectedItem.ToString();
			string filterText = filterTextBox.Text.Trim();
			string paramName = $"@param_{parameters.Count}";

			// Get column type from schema
			string connectionString = GetConnectionString();
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				using (var command = new SqlCommand($"SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName", connection))
				{
					command.Parameters.AddWithValue("@TableName", CurrentTable);
					command.Parameters.AddWithValue("@ColumnName", columnName);
					string dataType = (string)command.ExecuteScalar();

					switch (dataType.ToLower())
					{
						case "nvarchar":
						case "varchar":
						case "char":
						case "nchar":
						case "text":
						case "ntext":
							conditions.Add($"[{columnName}] LIKE '%' + {paramName} + '%'");
							parameters.Add(new SqlParameter(paramName, filterText));
							break;

						case "int":
						case "bigint":
						case "smallint":
						case "tinyint":
							if (int.TryParse(filterText, out int intValue))
							{
								conditions.Add($"[{columnName}] = {paramName}");
								parameters.Add(new SqlParameter(paramName, intValue));
							}
							break;

						case "decimal":
						case "numeric":
						case "float":
						case "real":
							if (decimal.TryParse(filterText, out decimal decimalValue))
							{
								conditions.Add($"[{columnName}] = {paramName}");
								parameters.Add(new SqlParameter(paramName, decimalValue));
							}
							break;

						case "datetime":
						case "date":
							if (DateTime.TryParse(filterText, out DateTime dateValue))
							{
								conditions.Add($"[{columnName}] = {paramName}");
								parameters.Add(new SqlParameter(paramName, dateValue));
							}
							break;

						default:
							conditions.Add($"CONVERT(NVARCHAR(MAX), [{columnName}]) LIKE '%' + {paramName} + '%'");
							parameters.Add(new SqlParameter(paramName, filterText));
							break;
					}
				}
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
		// Update the clear filter method to reload unfiltered data
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

			// Reset page and reload data
			CurrentPage = 1;
			LoadPagedData();
		}
	}
}