using System.Windows;
using System.Windows.Controls;
using CRUDTableOperations.Core.Models;
using CRUDTableOperations.ViewModels;

namespace CRUDTableOperations.Views;

public partial class DataGrid1Page : Page
{
    public DataGrid1Page(DataGrid1ViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

	private void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		// Commit any pending edits before saving
		employeesDataGrid.CommitEdit();
		// Call the method on the ViewModel
		(DataContext as DataGrid1ViewModel)?.OnSaveButtonClick();
	}

	private void FilterButton_Click(object sender, RoutedEventArgs e)
	{
		// Call the method on the ViewModel
		(DataContext as DataGrid1ViewModel)?.OnFilterButtonClick(
			FilterSurnameTextBox.Text,
			FilterFirstNameTextBox.Text,
			FilterIDNumberTextBox.Text
			);
	}
}
