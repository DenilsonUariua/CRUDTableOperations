using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace CRUDTableOperations.Views
{
	public partial class RecordFormWindow : Window
	{
		private readonly Dictionary<string, Control> _formControls;
		private readonly DataTable _dataTable;

		public DataRow NewRow { get; private set; }

		public RecordFormWindow(DataTable dataTable)
		{
			InitializeComponent();
			_formControls = new Dictionary<string, Control>();
			_dataTable = dataTable;
			CreateFormFields();
		}

		private void CreateFormFields()
		{
			foreach (DataColumn column in _dataTable.Columns)
			{
				// Skip auto-increment columns
				if (column.AutoIncrement)
					continue;

				// Create field label
				var label = new Label
				{
					Content = column.ColumnName,
					FontWeight = FontWeights.SemiBold,
					Margin = new Thickness(0, 5, 0, 2)
				};
				FormFields.Children.Add(label);

				// Create input control based on column type
				Control inputControl = CreateInputControl(column);
				_formControls.Add(column.ColumnName, inputControl);
				FormFields.Children.Add(inputControl);
			}
		}

		private Control CreateInputControl(DataColumn column)
		{
			switch (Type.GetTypeCode(column.DataType))
			{
				case TypeCode.Boolean:
					return new CheckBox
					{
						Margin = new Thickness(0, 0, 0, 10)
					};

				case TypeCode.DateTime:
					return new DatePicker
					{
						Margin = new Thickness(0, 0, 0, 10)
					};

				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Decimal:
				case TypeCode.Double:
					return new TextBox
					{
						Margin = new Thickness(0, 0, 0, 10),
						Tag = "numeric"
					};

				default:
					return new TextBox
					{
						Margin = new Thickness(0, 0, 0, 10)
					};
			}
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				NewRow = _dataTable.NewRow();

				foreach (var control in _formControls)
				{
					string columnName = control.Key;
					object value = GetControlValue(control.Value);

					if (value != null)
					{
						try
						{
							NewRow[columnName] = value;
						}
						catch (Exception ex)
						{
							MessageBox.Show($"Error setting value for {columnName}: {ex.Message}",
								"Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
							return;
						}
					}
					else if (!_dataTable.Columns[columnName].AllowDBNull)
					{
						MessageBox.Show($"Field '{columnName}' cannot be empty.",
							"Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
						return;
					}
				}

				DialogResult = true;
				Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error saving record: {ex.Message}",
					"Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private object GetControlValue(Control control)
		{
			switch (control)
			{
				case TextBox textBox:
					if (string.IsNullOrWhiteSpace(textBox.Text))
						return null;

					if (textBox.Tag?.ToString() == "numeric")
					{
						if (decimal.TryParse(textBox.Text, out decimal numericValue))
							return numericValue;
						throw new FormatException($"'{textBox.Text}' is not a valid number.");
					}

					return textBox.Text;

				case CheckBox checkBox:
					return checkBox.IsChecked;

				case DatePicker datePicker:
					return datePicker.SelectedDate;

				default:
					return null;
			}
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}
}