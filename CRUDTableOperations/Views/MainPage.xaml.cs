using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

using CRUDTableOperations.ViewModels;
using Microsoft.Data.Sql;

namespace CRUDTableOperations.Views;

public partial class MainPage : Page
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
		LoadServers();
        DataContext = viewModel;
    }

	// Load available SQL servers into the ServerComboBox
	private void LoadServers()
	{
		try
		{
			var servers = SqlDataSourceEnumerator.Instance.GetDataSources();
			foreach (DataRow row in servers.Rows)
			{
				var serverName = row["ServerName"].ToString();
				var instanceName = row["InstanceName"].ToString();
				ServerComboBox.Items.Add(
					string.IsNullOrEmpty(instanceName) ? serverName : $"{serverName}\\{instanceName}");
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error loading servers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	// Populate databases when a server is selected
	private void ServerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		DatabaseComboBox.Items.Clear();

		if (ServerComboBox.SelectedItem != null)
		{
			var server = ServerComboBox.SelectedItem.ToString();
			string connectionString = $"Server={server};Integrated Security=True;";

			try
			{
				using (var connection = new SqlConnection(connectionString))
				{
					connection.Open();
					var command = new SqlCommand("SELECT name FROM sys.databases;", connection);
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							DatabaseComboBox.Items.Add(reader["name"].ToString());
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error loading databases: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}

	// Handle Connect Button Click
	private void ConnectButton_Click(object sender, RoutedEventArgs e)
	{
		if (ServerComboBox.SelectedItem == null || DatabaseComboBox.SelectedItem == null)
		{
			MessageBox.Show("Please select both server and database.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}

		var server = ServerComboBox.SelectedItem.ToString();
		var database = DatabaseComboBox.SelectedItem.ToString();
		string connectionString = $"Server={server};Database={database};Integrated Security=True;";

		// Test connection
		try
		{
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				MessageBox.Show($"Connected to {database} on {server}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

}
