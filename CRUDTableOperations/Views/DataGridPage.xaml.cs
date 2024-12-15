using System.Windows;
using System.Windows.Controls;

using CRUDTableOperations.ViewModels;

namespace CRUDTableOperations.Views;

public partial class DataGridPage : Page
{
	public DataGridPage(DataGridViewModel viewModel)
	{
		InitializeComponent();
		DataContext = viewModel;
	}

	private void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		// Commit any pending edits before saving
		carDataGrid.CommitEdit();
		// Call the method on the ViewModel
		(DataContext as DataGridViewModel)?.OnSaveButtonClick();
	}

	private void FilterButton_Click(object sender, RoutedEventArgs e)
	{
		// Call the method on the ViewModel
		(DataContext as DataGridViewModel)?.OnFilterButtonClick(
			FilterMakeTextBox.Text,
			FilterModelTextBox.Text,
			FilterYearTextBox.Text,
			FilterPriceTextBox.Text
			);
	}
}
