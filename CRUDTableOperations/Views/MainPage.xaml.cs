using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using CRUDTableOperations.ViewModels;

namespace CRUDTableOperations.Views
{
	public partial class MainPage : Page
	{
		// Current selected table and connection details
		private string CurrentServer { get; set; }
		private string CurrentDatabase { get; set; }
		private string CurrentTable { get; set; }

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
		}

		private void InitializeCRUDButtons()
		{
			// Initially disable CRUD buttons
			btnCreate.IsEnabled = false;
			btnUpdate.IsEnabled = false;
			btnDelete.IsEnabled = false;
			btnSave.IsEnabled = false;
			btnCancel.IsEnabled = false;
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

			try
			{
				using (var connection = new SqlConnection(connectionString))
				{
					connection.Open();
					MessageBox.Show($"Connected on {server}. The connection status is {connection.State}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

					// Populate databases
					DatabaseComboBox.Items.Clear();
					using (var command = new SqlCommand("SELECT name FROM sys.databases WHERE database_id > 4", connection))
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

			try
			{
				// Populate tables
				TableComboBox.Items.Clear();
				using (var connection = new SqlConnection($"Server={ServerTextBox.Text};Database={DatabaseComboBox.SelectedItem};Integrated Security=True;"))
				{
					connection.Open();
					using (var command = new SqlCommand("SELECT name FROM sys.tables", connection))
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
				}

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

				using (var connection = new SqlConnection(connectionString))
				{
					connection.Open();

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
				MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
	}
}