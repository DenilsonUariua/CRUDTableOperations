using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRUDTableOperations.Contracts.ViewModels;
using CRUDTableOperations.Core.Contracts.Services;
using CRUDTableOperations.Core.Models;
using CRUDTableOperations.Core.Services;
using Microsoft.IdentityModel.Tokens;

namespace CRUDTableOperations.ViewModels;

public class DataGrid1ViewModel : ObservableObject, INavigationAware
{
	private readonly EmployeeSQLDataService _sampleDataService;

	public ObservableCollection<Employee> Source { get; } = new ObservableCollection<Employee>();
	public List<Employee> OriginalEmployees { get; set; } = new List<Employee>();

	public DataGrid1ViewModel(EmployeeSQLDataService sampleDataService)
	{
		_sampleDataService = sampleDataService;
	}

	public void OnNavigatedTo(object parameter)
	{
		Source.Clear();

		// Replace this with your actual data
		var data = _sampleDataService.GetAllEmployees();

		OriginalEmployees = data.Select(employee => new Employee
		{
			Id = employee.Id,
			EmployeeNumber = employee.EmployeeNumber,
			Id2 = employee.Id2,
			Surname = employee.Surname,
			Initials = employee.Initials,
			FirstName = employee.FirstName,
			SecondName = employee.SecondName,
			IdNumber = employee.IdNumber,
			GroupJoinDate = employee.GroupJoinDate,
			LastDischargeDate = employee.LastDischargeDate
		}).ToList();

		foreach (var item in data)
		{
			Source.Add(item);
		}
	}

	public void OnSaveButtonClick()
	{
		try
		{
			// Collect changes
			var changedEmployees = Source
				.Where(current =>
					!OriginalEmployees.Any(original =>
						original.Id == current.Id &&
						AreCarPropertiesEqual(original, current)))
				.ToList();
			Debug.WriteLine($"Changes found: {changedEmployees.Count}");

			// Validate changes
			if (!ValidateEmployee(changedEmployees))
			{
				MessageBox.Show("Validation of data failed", "Save Confirmation",
					MessageBoxButton.OK);
				return;
			}

			// Detect deletions
			var deletedEmployees = OriginalEmployees
				.Where(original => !Source.Any(current => current.Id == original.Id))
				.ToList();

			// Prompt for confirmation if deletions are detected
			if (deletedEmployees.Count > 0)
			{
				var confirmation = MessageBox.Show(
					"The following employees will be deleted:\n" +
					string.Join("\n", deletedEmployees.Select(employee => $"{employee.Surname} {employee.FirstName} ({employee.IdNumber})")),
					"Delete Confirmation",
					MessageBoxButton.YesNo,
					MessageBoxImage.Warning);

				if (confirmation == MessageBoxResult.No)
				{
					return;
				}

				// Delete removed employees from the database
				foreach (var employee in deletedEmployees)
				{
					_sampleDataService.DeleteEmployee(employee.Id);
				}
			}

			if (changedEmployees.Count == 0)
			{
				// Refresh original employees list
				OriginalEmployees = new List<Employee>(Source);

				MessageBox.Show("No changes detected or new employees found to save.", "Save Confirmation",
					MessageBoxButton.OK);
				return;
			}

			// Save changes
			foreach (var employee in changedEmployees)
			{
				// If the employee exists in original list, update it
				if (OriginalEmployees.Any(c => c.Id == employee.Id))
				{
					_sampleDataService.UpdateEmployee(employee);
				}
				// If it's a new employee, add it
				else
				{
					_sampleDataService.AddEmployee(employee);
				}
			}

			// Refresh original employees list
			OriginalEmployees = new List<Employee>(Source);

			MessageBox.Show("Data saved successfully", "Save Confirmation",
				MessageBoxButton.OK);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error saving employees: {ex.Message}");
			MessageBox.Show("An error occurred while saving data. Please try again.",
				"Save Error", MessageBoxButton.OK);
		}
	}

	// Handle filtering logic
	public void OnFilterButtonClick(string FilterSurname, string FilterFirstName, string IDNumber)
	{
		var filteredEmployees = OriginalEmployees.AsEnumerable();

		// Filter by Make
		if (!string.IsNullOrEmpty(FilterSurname))
		{
			filteredEmployees = filteredEmployees.Where(employee => employee.Surname.Contains(FilterSurname, StringComparison.OrdinalIgnoreCase));
		}

		// Filter by Model
		if (!string.IsNullOrEmpty(FilterFirstName))
		{
			filteredEmployees = filteredEmployees.Where(employee => employee.FirstName.Contains(FilterFirstName, StringComparison.OrdinalIgnoreCase));
		}

		// Filter by Year (numeric check)
		if (!string.IsNullOrEmpty(IDNumber))
		{
			filteredEmployees = filteredEmployees.Where(employee => employee.IdNumber.Contains(IDNumber, StringComparison.OrdinalIgnoreCase));
		}



		// Update the Source collection with the filtered results
		Source.Clear();
		foreach (var employee in filteredEmployees)
		{
			Source.Add(employee);
		}
	}

	private bool AreCarPropertiesEqual(Employee original, Employee current)
	{
		return original.Surname == current.Surname &&
			   original.FirstName == current.FirstName &&
			   original.IdNumber == current.IdNumber &&
			   original.Initials == current.Initials;
	}

	private bool ValidateEmployee(List<Employee> employees)
	{
		// Add validation logic
		foreach (var employee in employees)
		{
			if (string.IsNullOrWhiteSpace(employee.Surname) ||
				string.IsNullOrWhiteSpace(employee.FirstName) ||
				string.IsNullOrWhiteSpace(employee.IdNumber) ||
				string.IsNullOrWhiteSpace(employee.EmployeeNumber)
				)
			{
				MessageBox.Show("Invalid employee data. Please check your entries.",
					"Validation Error", MessageBoxButton.OK);
				return false;
			}
		}
		return true;
	}

	private void SaveNewEmployees(List<Employee> newEmployees)
	{
		// Use bulk insert or transaction if supported by the service
		foreach (var employee in newEmployees)
		{
			_sampleDataService.AddEmployee(employee);
		}
	}

	public void OnNavigatedFrom()
	{
		
	}
}
