using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CRUDTableOperations.Contracts.ViewModels;
using CRUDTableOperations.Core.Models;
using CRUDTableOperations.Core.Services;

namespace CRUDTableOperations.ViewModels;

public class DataGridViewModel : ObservableObject, INavigationAware
{
	private readonly SQLDataService _sampleDataService;

	public ObservableCollection<Car> Source { get; } = new ObservableCollection<Car>();
	public List<Car> OriginalCars { get; set; } = new List<Car>();

	public DataGridViewModel(SQLDataService sampleDataService)
	{
		_sampleDataService = sampleDataService;
	}

	public void OnNavigatedTo(object parameter)
	{
		Source.Clear();
		OriginalCars.Clear();

		// Replace this with your actual data
		var data = _sampleDataService.GetAllCars();

		OriginalCars = data.Select(car => new Car
		{
			CarID = car.CarID,
			Make = car.Make,
			Model = car.Model,
			Year = car.Year,
			Price = car.Price
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
			var changedCars = Source
				.Where(current =>
					!OriginalCars.Any(original =>
						original.CarID == current.CarID &&
						AreCarPropertiesEqual(original, current)))
				.ToList();
			Debug.WriteLine($"Changes found: {changedCars.Count}");

			// Validate changes
			if (!ValidateCars(changedCars))
			{
				MessageBox.Show("Validation of data failed", "Save Confirmation",
					MessageBoxButton.OK);
				return;
			}

			// Detect deletions
			var deletedCars = OriginalCars
				.Where(original => !Source.Any(current => current.CarID == original.CarID))
				.ToList();

			// Prompt for confirmation if deletions are detected
			if (deletedCars.Count > 0)
			{
				var confirmation = MessageBox.Show(
					"The following cars will be deleted:\n" +
					string.Join("\n", deletedCars.Select(car => $"{car.Make} {car.Model} ({car.Year})")),
					"Delete Confirmation",
					MessageBoxButton.YesNo,
					MessageBoxImage.Warning);

				if (confirmation == MessageBoxResult.No)
				{
					return;
				}

				// Delete removed cars from the database
				foreach (var car in deletedCars)
				{
					_sampleDataService.DeleteCar(car.CarID);
				}
			}

			if (changedCars.Count == 0)
			{
				// Refresh original cars list
				OriginalCars = new List<Car>(Source);

				MessageBox.Show("No changes detected or new cars found to save.", "Save Confirmation",
					MessageBoxButton.OK);
				return;
			}

			// Save changes
			foreach (var car in changedCars)
			{
				// If the car exists in original list, update it
				if (OriginalCars.Any(c => c.CarID == car.CarID))
				{
					_sampleDataService.UpdateCar(car.CarID, car.Make, car.Model, car.Year, car.Price);
				}
				// If it's a new car, add it
				else
				{
					_sampleDataService.AddCar(car.Make, car.Model, car.Year, car.Price);
				}
			}

			// Refresh original cars list
			OriginalCars = new List<Car>(Source);

			MessageBox.Show("Data saved successfully", "Save Confirmation",
				MessageBoxButton.OK);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error saving cars: {ex.Message}");
			MessageBox.Show("An error occurred while saving data. Please try again.",
				"Save Error", MessageBoxButton.OK);
		}
	}
	// Handle filtering logic
	public void OnFilterButtonClick(string FilterMake, string FilterModel, string FilterYear, string FilterPrice)
	{
		var filteredCars = OriginalCars.AsEnumerable();

		// Filter by Make
		if (!string.IsNullOrEmpty(FilterMake))
		{
			filteredCars = filteredCars.Where(car => car.Make.Contains(FilterMake, StringComparison.OrdinalIgnoreCase));
		}

		// Filter by Model
		if (!string.IsNullOrEmpty(FilterModel))
		{
			filteredCars = filteredCars.Where(car => car.Model.Contains(FilterModel, StringComparison.OrdinalIgnoreCase));
		}

		// Filter by Year (numeric check)
		if (int.TryParse(FilterYear, out int year))
		{
			filteredCars = filteredCars.Where(car => car.Year == year);
		}

		// Filter by Price (numeric check)
		if (decimal.TryParse(FilterPrice, out decimal price))
		{
			filteredCars = filteredCars.Where(car => car.Price == price);
		}

		// Update the Source collection with the filtered results
		Source.Clear();
		foreach (var car in filteredCars)
		{
			Source.Add(car);
		}
	}

	private bool AreCarPropertiesEqual(Car original, Car current)
	{
		return original.Make == current.Make &&
			   original.Model == current.Model &&
			   original.Year == current.Year &&
			   original.Price == current.Price;
	}

	private bool ValidateCars(List<Car> cars)
	{
		// Add validation logic
		foreach (var car in cars)
		{
			if (string.IsNullOrWhiteSpace(car.Make) ||
				string.IsNullOrWhiteSpace(car.Model) ||
				car.Year <= 0 ||
				car.Price < 0)
			{
				MessageBox.Show("Invalid car data. Please check your entries.",
					"Validation Error", MessageBoxButton.OK);
				return false;
			}
		}
		return true;
	}

	private void SaveNewCars(List<Car> newCars)
	{
		// Use bulk insert or transaction if supported by the service
		foreach (var car in newCars)
		{
			_sampleDataService.AddCar(car.Make, car.Model, car.Year, car.Price);
		}
	}

	public void OnNavigatedFrom()
	{
	}
}
