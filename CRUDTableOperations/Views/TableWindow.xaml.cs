using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace CRUDTableOperations.Views
{
    public partial class TableWindow : Window
    {
        private string ServerName { get; set; }
        private string DatabaseName { get; set; }
        private string TableName { get; set; }
        private bool IsWindowsAuth { get; set; }
        private string Username { get; set; }
        private string Password { get; set; }
        private DataTable CurrentDataTable { get; set; }
        private HashSet<string> PrimaryKeyColumns { get; set; }

        // Pagination
        private int CurrentPage { get; set; } = 1;
        private const int PageSize = 15;
        private int TotalRecords { get; set; }

        public TableWindow(string server, string database, string table, bool isWindowsAuth, string username, string password)
        {
            InitializeComponent();

            ServerName = server;
            DatabaseName = database;
            TableName = table;
            IsWindowsAuth = isWindowsAuth;
            Username = username;
            Password = password;

            Title = $"{database} - {table}";

            LoadPrimaryKeyColumns();
            InitializeCRUDButtons();
            LoadPagedData(true);
            PopulateColumnFilters();

            FilterPanel.Visibility = Visibility.Visible;
            FilterTextBlock.Visibility = Visibility.Visible;
        }

        private void InitializeCRUDButtons()
        {
            btnCreate.IsEnabled = true;
            btnUpdate.IsEnabled = true;
            btnDelete.IsEnabled = true;
            btnSave.IsEnabled = false;
            btnCancel.IsEnabled = false;
            btnRefresh.IsEnabled = true;
        }

        private void LoadPrimaryKeyColumns()
        {
            PrimaryKeyColumns = new HashSet<string>();
            string connectionString = GetConnectionString();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string keyQuery = @"
                        SELECT COLUMN_NAME
                        FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                        WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + QUOTENAME(CONSTRAINT_NAME)), 'IsPrimaryKey') = 1
                        AND TABLE_NAME = @TableName";

                    using (var keyCommand = new SqlCommand(keyQuery, connection))
                    {
                        keyCommand.Parameters.AddWithValue("@TableName", TableName);
                        using (var reader = keyCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                PrimaryKeyColumns.Add(reader.GetString(0));
                            }
                        }
                    }
                }

                Debug.WriteLine($"Loaded {PrimaryKeyColumns.Count} primary key columns for table {TableName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading primary key columns: {ex.Message}");
            }
        }

        private void LoadPagedData(bool showMessage = false)
        {
            string connectionString = GetConnectionString();
            CurrentDataTable = new DataTable();

            using (var connection = new SqlConnection(connectionString))
            {
                // First, get total count
                string countQuery = $"SELECT COUNT(*) FROM [{TableName}]";
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
                    FROM [{TableName}]
                ) AS Paged
                WHERE RowNum BETWEEN ({CurrentPage - 1} * {PageSize} + 1) AND ({CurrentPage} * {PageSize})";

                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                SqlCommandBuilder builder = new SqlCommandBuilder(adapter);
                adapter.Fill(CurrentDataTable);

                DataGridResults.ItemsSource = CurrentDataTable.DefaultView;

                // Update status message
                UpdatePaginationStatus(showMessage);
            }
        }

        private string GetConnectionString()
        {
            string connectionString = $"Server={ServerName};Database={DatabaseName};Integrated Security=True;";
            if (!IsWindowsAuth)
            {
                if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    throw new InvalidOperationException("Username and password are required for SQL Server authentication.");
                }
                connectionString = $"Server={ServerName};Database={DatabaseName};User Id={Username};Password={Password};";
            }
            return connectionString;
        }

        private void UpdatePaginationStatus(bool showMessage = false)
        {
            int totalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
            txtPageInfo.Text = $"Page {CurrentPage} of {totalPages} (Total: {TotalRecords} records)";

            if (showMessage)
            {
                MessageBox.Show($"Showing page {CurrentPage} of {totalPages} (Total records: {TotalRecords})",
                    "Data Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

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

        // Create new row - EXCLUDING PRIMARY KEY COLUMNS AND ROWNUM
        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDataTable == null)
            {
                MessageBox.Show("Unable to create record.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create a filtered DataTable without primary key columns and RowNum
            DataTable filteredTable = CurrentDataTable.Clone();

            // Remove primary key columns
            foreach (string pkColumn in PrimaryKeyColumns)
            {
                if (filteredTable.Columns.Contains(pkColumn))
                {
                    filteredTable.Columns.Remove(pkColumn);
                }
            }

            // Remove RowNum column if it exists
            if (filteredTable.Columns.Contains("RowNum"))
            {
                filteredTable.Columns.Remove("RowNum");
            }

            var formWindow = new RecordFormWindow(filteredTable)
            {
                Owner = this
            };

            if (formWindow.ShowDialog() == true)
            {
                try
                {
                    DataRow newRow = CurrentDataTable.NewRow();

                    // Copy values from form, excluding primary key columns and RowNum
                    foreach (DataColumn col in filteredTable.Columns)
                    {
                        if (CurrentDataTable.Columns.Contains(col.ColumnName))
                        {
                            newRow[col.ColumnName] = formWindow.NewRow[col.ColumnName];
                        }
                    }

                    CurrentDataTable.Rows.Add(newRow);
                    btnSave.IsEnabled = true;
                    btnCancel.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding new record: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridResults.SelectedItem == null)
            {
                MessageBox.Show("Please select a row to update.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataGridResults.BeginEdit();
            btnSave.IsEnabled = true;
            btnCancel.IsEnabled = true;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridResults.SelectedItem == null)
            {
                MessageBox.Show("Please select a row to delete.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Are you sure you want to delete the selected row?", "Confirm Deletion",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ((DataRowView)DataGridResults.SelectedItem).Row.Delete();
                btnSave.IsEnabled = true;
                btnCancel.IsEnabled = true;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDataTable == null)
            {
                MessageBox.Show("No changes to save.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string connectionString = GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Get identity columns
                    var identityColumns = new HashSet<string>();
                    string identityQuery = @"
                        SELECT c.name
                        FROM sys.columns c
                        INNER JOIN sys.tables t ON c.object_id = t.object_id
                        WHERE t.name = @TableName AND c.is_identity = 1";

                    using (var identityCommand = new SqlCommand(identityQuery, connection))
                    {
                        identityCommand.Parameters.AddWithValue("@TableName", TableName);
                        using (var reader = identityCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                identityColumns.Add(reader.GetString(0));
                            }
                        }
                    }

                    // Get non-nullable columns
                    var nonNullableColumns = new HashSet<string>();
                    string schemaQuery = @"
                        SELECT COLUMN_NAME 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = @TableName 
                        AND IS_NULLABLE = 'NO'";

                    using (var schemaCommand = new SqlCommand(schemaQuery, connection))
                    {
                        schemaCommand.Parameters.AddWithValue("@TableName", TableName);
                        using (var reader = schemaCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                nonNullableColumns.Add(reader.GetString(0));
                            }
                        }
                    }

                    // Auto-increment logic for non-PK, non-identity integer columns
                    var maxColumnValues = new Dictionary<string, int>();
                    var columnsToAutoIncrement = new List<DataColumn>();

                    foreach (DataColumn column in CurrentDataTable.Columns)
                    {
                        bool isIntegerType = column.DataType == typeof(int) ||
                                             column.DataType == typeof(long) ||
                                             column.DataType == typeof(short);
                        bool doesNotAllowNull = nonNullableColumns.Contains(column.ColumnName);
                        bool isNotIdentity = !identityColumns.Contains(column.ColumnName);
                        bool isNotPrimaryKey = !PrimaryKeyColumns.Contains(column.ColumnName);

                        if (isIntegerType && doesNotAllowNull && isNotIdentity && isNotPrimaryKey)
                        {
                            columnsToAutoIncrement.Add(column);

                            string maxValQuery = $"SELECT ISNULL(MAX([{column.ColumnName}]), 0) FROM [{TableName}]";
                            using (var maxValCommand = new SqlCommand(maxValQuery, connection))
                            {
                                object result = maxValCommand.ExecuteScalar();
                                if (result != DBNull.Value && result != null)
                                {
                                    maxColumnValues[column.ColumnName] = Convert.ToInt32(result);
                                }
                                else
                                {
                                    maxColumnValues[column.ColumnName] = 0;
                                }
                            }
                        }
                    }

                    // Process new rows
                    foreach (DataRow row in CurrentDataTable.Rows)
                    {
                        if (row.RowState == DataRowState.Added)
                        {
                            foreach (DataColumn column in columnsToAutoIncrement)
                            {
                                if (row[column.ColumnName] == DBNull.Value ||
                                    (row[column.ColumnName] is int && (int)row[column.ColumnName] == 0) ||
                                    string.IsNullOrEmpty(row[column.ColumnName]?.ToString()))
                                {
                                    maxColumnValues[column.ColumnName]++;
                                    row[column.ColumnName] = maxColumnValues[column.ColumnName];
                                }
                            }
                        }
                    }

                    // Update database
                    string query = $"SELECT * FROM [{TableName}]";
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    SqlCommandBuilder builder = new SqlCommandBuilder(adapter);
                    adapter.Update(CurrentDataTable);
                    CurrentDataTable.AcceptChanges();

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

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            CurrentPage = 1;
            LoadPagedData(true);

            // Clear filters
            txtFilter1.Clear();
            txtFilter2.Clear();
            txtFilter3.Clear();
            cmbColumn1.SelectedItem = null;
            cmbColumn2.SelectedItem = null;
            cmbColumn3.SelectedItem = null;

            btnSave.IsEnabled = false;
            btnCancel.IsEnabled = false;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDataTable != null)
            {
                CurrentDataTable.RejectChanges();
                DataGridResults.ItemsSource = null;
                DataGridResults.ItemsSource = CurrentDataTable.DefaultView;
                btnSave.IsEnabled = false;
                btnCancel.IsEnabled = false;
            }
        }

        private void ApplyFilter(object sender, TextChangedEventArgs e)
        {
            if (CurrentDataTable == null) return;

            try
            {
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

        private void LoadFilteredData()
        {
            string connectionString = GetConnectionString();
            CurrentDataTable = new DataTable();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var (whereClause, parameters) = BuildDatabaseFilterString();

                string countQuery = $"SELECT COUNT(*) FROM [{TableName}]";
                if (!string.IsNullOrWhiteSpace(whereClause))
                {
                    countQuery += $" WHERE {whereClause}";
                }

                using (var countCommand = new SqlCommand(countQuery, connection))
                {
                    foreach (SqlParameter parameter in parameters)
                    {
                        countCommand.Parameters.Add(CloneSqlParameter(parameter));
                    }
                    TotalRecords = (int)countCommand.ExecuteScalar();
                }

                string query = $@"
                    SELECT *
                    FROM (
                        SELECT *, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS RowNum
                        FROM [{TableName}]
                        {(string.IsNullOrWhiteSpace(whereClause) ? "" : $"WHERE {whereClause}")}
                    ) AS Paged
                    WHERE RowNum BETWEEN ({CurrentPage - 1} * {PageSize} + 1) AND ({CurrentPage} * {PageSize})";

                using (var command = new SqlCommand(query, connection))
                {
                    foreach (SqlParameter parameter in parameters)
                    {
                        command.Parameters.Add(CloneSqlParameter(parameter));
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

            string connectionString = GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand($"SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName", connection))
                {
                    command.Parameters.AddWithValue("@TableName", TableName);
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

        private void PopulateColumnFilters()
        {
            if (CurrentDataTable == null) return;

            cmbColumn1.Items.Clear();
            cmbColumn2.Items.Clear();
            cmbColumn3.Items.Clear();

            foreach (DataColumn column in CurrentDataTable.Columns)
            {
                cmbColumn1.Items.Add(column.ColumnName);
                cmbColumn2.Items.Add(column.ColumnName);
                cmbColumn3.Items.Add(column.ColumnName);
            }

            txtFilter1.IsEnabled = true;
            txtFilter2.IsEnabled = true;
            txtFilter3.IsEnabled = true;
            cmbColumn1.IsEnabled = true;
            cmbColumn2.IsEnabled = true;
            cmbColumn3.IsEnabled = true;
        }

        private void btnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            txtFilter1.Clear();
            txtFilter2.Clear();
            txtFilter3.Clear();
            cmbColumn1.SelectedItem = null;
            cmbColumn2.SelectedItem = null;
            cmbColumn3.SelectedItem = null;

            CurrentPage = 1;
            LoadPagedData();
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
    }
}