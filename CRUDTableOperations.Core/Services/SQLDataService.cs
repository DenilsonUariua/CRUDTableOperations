using CRUDTableOperations.Core.Models;
using System.Data.SqlClient;

namespace CRUDTableOperations.Core.Services
{
	public class SQLDataService
	{
		private static string _connectionString;

		public SQLDataService()
		{
			// Retrieve the connection string from environment variables
			_connectionString= "Server=(localdb)\\TestDB;Database=master;Trusted_Connection=True;"
				?? throw new InvalidOperationException("Database connection string is not configured.");
		}

		public static void SetConnectionString(string connectionString)
		{
			_connectionString = connectionString;
		}

		// Method to establish a database connection
		private SqlConnection GetConnection()
		{
			return new SqlConnection(_connectionString);
		}

		// CREATE Operation: Add a new car
		public void AddCar(string make, string model, int year, decimal price)
		{
			using (var connection = GetConnection())
			{
				connection.Open();
				var query = "INSERT INTO Cars (Make, Model, Year, Price) VALUES (@Make, @Model, @Year, @Price)";
				using (var command = new SqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@Make", make);
					command.Parameters.AddWithValue("@Model", model);
					command.Parameters.AddWithValue("@Year", year);
					command.Parameters.AddWithValue("@Price", price);
					command.ExecuteNonQuery();
				}
			}
		}

		// READ Operation: Get all cars
		public List<Car> GetAllCars()
		{
			var cars = new List<Car>();

			using (var connection = GetConnection())
			{
				connection.Open();
				var query = "SELECT CarID, Make, Model, Year, Price FROM Cars";
				using (var command = new SqlCommand(query, connection))
				{
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							cars.Add(new Car
							{
								CarID = reader.GetInt32(0),
								Make = reader.GetString(1),
								Model = reader.GetString(2),
								Year = reader.GetInt32(3),
								Price = reader.GetDecimal(4)
							});
						}
					}
				}
			}

			return cars;
		}

		// UPDATE Operation: Update car details
		public void UpdateCar(int carId, string make, string model, int year, decimal price)
		{
			using (var connection = GetConnection())
			{
				connection.Open();
				var query = "UPDATE Cars SET Make = @Make, Model = @Model, Year = @Year, Price = @Price WHERE CarID = @CarID";
				using (var command = new SqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@CarID", carId);
					command.Parameters.AddWithValue("@Make", make);
					command.Parameters.AddWithValue("@Model", model);
					command.Parameters.AddWithValue("@Year", year);
					command.Parameters.AddWithValue("@Price", price);
					command.ExecuteNonQuery();
				}
			}
		}

		// DELETE Operation: Remove a car by ID
		public void DeleteCar(int carId)
		{
			using (var connection = GetConnection())
			{
				connection.Open();
				var query = "DELETE FROM Cars WHERE CarID = @CarID";
				using (var command = new SqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@CarID", carId);
					command.ExecuteNonQuery();
				}
			}
		}
	}

}
