using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows;

namespace CRUDTableOperations.Helpers
{
	public class ColumnSelectionWindow : Window
	{
		public string SelectedColumn { get; private set; }

		public ColumnSelectionWindow(DataTable table)
		{
			Title = "Select Primary Key Column";
			Width = 400;
			Height = 300;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			Style = (Style)Application.Current.Resources["MahApps.Styles.Window"];

			var grid = new Grid
			{
				Margin = new Thickness(10)
			};
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			// Instructions
			var instructionsText = new TextBlock
			{
				Text = "Select a column to use as the primary key:",
				Style = (Style)Application.Current.Resources["PageTitleStyle"],
				Margin = new Thickness(0, 0, 0, 10)
			};
			Grid.SetRow(instructionsText, 0);
			grid.Children.Add(instructionsText);

			// Column list
			var stackPanel = new StackPanel
			{
				Margin = new Thickness(0, 5, 0, 5)
			};

			var radioButtonGroup = "PrimaryKeyGroup";
			foreach (DataColumn column in table.Columns)
			{
				var radioButton = new RadioButton
				{
					Content = column.ColumnName,
					Margin = new Thickness(5),
					GroupName = radioButtonGroup,
					Style = (Style)Application.Current.Resources["MahApps.Styles.RadioButton"]
				};
				stackPanel.Children.Add(radioButton);
			}

			var scrollViewer = new ScrollViewer
			{
				Content = stackPanel,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto
			};
			Grid.SetRow(scrollViewer, 1);
			grid.Children.Add(scrollViewer);

			// Buttons
			var buttonPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Right,
				Margin = new Thickness(0, 10, 0, 0)
			};
			Grid.SetRow(buttonPanel, 2);

			var okButton = new Button
			{
				Content = "OK",
				Width = 75,
				Margin = new Thickness(5, 0, 5, 0),
				IsDefault = true,
				Style = (Style)Application.Current.Resources["MahApps.Styles.Button"]
			};
			okButton.Click += OkButton_Click;

			var cancelButton = new Button
			{
				Content = "Cancel",
				Width = 75,
				Margin = new Thickness(5, 0, 0, 0),
				IsCancel = true,
				Style = (Style)Application.Current.Resources["MahApps.Styles.Button"]
			};

			buttonPanel.Children.Add(okButton);
			buttonPanel.Children.Add(cancelButton);
			grid.Children.Add(buttonPanel);

			Content = grid;
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			var panel = ((ScrollViewer)((Grid)Content).Children[1]).Content as StackPanel;
			var selectedRadioButton = panel.Children.OfType<RadioButton>().FirstOrDefault(rb => rb.IsChecked == true);

			if (selectedRadioButton == null)
			{
				MessageBox.Show("Please select a column to use as the primary key.", "Warning",
					MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			SelectedColumn = selectedRadioButton.Content.ToString();
			DialogResult = true;
		}
	}

}
