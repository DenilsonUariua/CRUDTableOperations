using CRUDTableOperations.Core.Models;
using System.Data.SqlClient;

namespace CRUDTableOperations.Core.Services
{
	public class EmployeeSQLDataService
	{
		private static string _connectionString;

		public EmployeeSQLDataService()
		{
			// Retrieve the connection string from environment variables
			_connectionString = "Server=MFILESDB01\\TDWPRODUCTION,49170;Database=Namib Mills;Trusted_Connection=True;"
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

		/// <summary>
		/// Adds a new employee to the Emp_List table
		/// </summary>
		/// <param name="employee">Employee object containing details to be inserted</param>
		/// <returns>The ID of the newly inserted employee</returns>
		public int AddEmployee(Employee employee)
		{
			using (var connection = GetConnection())
			{
				connection.Open();

				var query = @"
                    INSERT INTO [Namib Mills].[dbo].[Emp_List] 
                    (
                        [Surname], 
                        [First Name], 
                        [Second Name], 
                        [ID Number], 
                        [Group Join Date], 
                        [Last Discharge Date], 
                        [Initials],
                        [EmployeeNumber]
                    ) 
                    OUTPUT INSERTED.ID
                    VALUES 
                    (
                        @Surname, 
                        @FirstName, 
                        @SecondName, 
                        @IdNumber, 
                        @GroupJoinDate, 
                        @LastDischargeDate, 
                        @Initials,
                        @EmployeeNumber
                    )";

				using (var command = new SqlCommand(query, connection))
				{
					// Add parameters with null handling
					command.Parameters.AddWithValue("@Surname", employee.Surname ?? (object)DBNull.Value);
					command.Parameters.AddWithValue("@FirstName", employee.FirstName ?? (object)DBNull.Value);
					command.Parameters.AddWithValue("@SecondName", employee.SecondName ?? (object)DBNull.Value);
					command.Parameters.AddWithValue("@IdNumber", employee.IdNumber ?? (object)DBNull.Value);
					command.Parameters.AddWithValue("@GroupJoinDate", employee.GroupJoinDate ?? (object)DBNull.Value);
					command.Parameters.AddWithValue("@LastDischargeDate", employee.LastDischargeDate ?? (object)DBNull.Value);
					command.Parameters.AddWithValue("@Initials", employee.Initials ?? (object)DBNull.Value);
					command.Parameters.AddWithValue("@EmployeeNumber", employee.EmployeeNumber ?? (object)DBNull.Value);

					// Execute and return the new ID
					return Convert.ToInt32(command.ExecuteScalar());
				}
			}
		}

		public List<Employee> GetAllEmployees()
		{
			var employees = new List<Employee>();
			using (var connection = GetConnection())
			{
				connection.Open();
				var query = @"SELECT [ID]
                    ,[ID2]
                    ,[EmployeeNumber]
                    ,[Surname]
                    ,[First Name]
                    ,[Second Name]
                    ,[ID Number]
                    ,[Group Join Date]
                    ,[Last Discharge Date]
                    ,[Initials]
                FROM [Namib Mills].[dbo].[Emp_List]";

				using (var command = new SqlCommand(query, connection))
				{
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							employees.Add(new Employee
							{
								Id = reader.GetInt32(0),
								Id2 = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
								EmployeeNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
								Surname = reader.IsDBNull(3) ? null : reader.GetString(3),
								FirstName = reader.IsDBNull(4) ? null : reader.GetString(4),
								SecondName = reader.IsDBNull(5) ? null : reader.GetString(5),
								IdNumber = reader.IsDBNull(6) ? null : reader.GetString(6),
								GroupJoinDate = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7),
								LastDischargeDate = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8),
								Initials = reader.IsDBNull(9) ? null : reader.GetString(9)
							});
						}
					}
				}
			}
			return employees;
		}

		/// <summary>
		/// Updates an existing employee's details in the database
		/// </summary>
		/// <param name="employee">Employee object with updated information</param>
		/// <returns>Number of rows affected</returns>
		public int UpdateEmployee(Employee employee)
		{
			using (var connection = GetConnection())
			{
				connection.Open();
				var query = @"
                    UPDATE [Namib Mills].[dbo].[Emp_List] 
                    SET 
                        [Surname] = @Surname, 
                        [First Name] = @FirstName, 
                        [Second Name] = @SecondName, 
                        [ID Number] = @IdNumber, 
                        [Group Join Date] = @GroupJoinDate, 
                        [Last Discharge Date] = @LastDischargeDate, 
                        [Initials] = @Initials,
                        [EmployeeNumber] = @EmployeeNumber
                    WHERE [ID] = @ID";

				using (var command = new SqlCommand(query, connection))
				{
					// Add parameters with null handling
					command.Parameters.AddWithValue("@Surname", employee.Surname ?? (object)DBNull.Value);
					command.Parameters.AddWithValue("@FirstName", employee.FirstName ?? (object)DBNull.Value);
					command.Parameters.AddWithValue("@SecondName", employee.SecondName ?? (object)DBNull.Value);
					command.Parameters.AddWithValue("@IdNumber", employee.IdNumber ?? (object)DBNull.Value);
					command.Parameters.AddWithValue("@GroupJoinDate", employee.GroupJoinDate ?? (object)DBNull.Value);
					command.Parameters.AddWithValue("@LastDischargeDate", employee.LastDischargeDate ?? (object)DBNull.Value);
					command.Parameters.AddWithValue("@Initials", employee.Initials ?? (object)DBNull.Value);
					command.Parameters.AddWithValue("@EmployeeNumber", employee.EmployeeNumber ?? (object)DBNull.Value);
					command.Parameters.AddWithValue("@ID", employee.Id);

					// Return the number of rows affected
					return command.ExecuteNonQuery();
				}
			}
		}

		/// <summary>
		/// Deletes an employee from the database by their ID
		/// </summary>
		/// <param name="employeeId">The unique identifier of the employee to delete</param>
		/// <returns>The number of rows affected by the delete operation</returns>
		/// <exception cref="ArgumentException">Thrown when the employee ID is invalid</exception>
		public int DeleteEmployee(int employeeId)
		{
			// Validate input
			if (employeeId <= 0)
			{
				throw new ArgumentException("Invalid employee ID. ID must be a positive integer.", nameof(employeeId));
			}

			try
			{
				using (var connection = GetConnection())
				{
					connection.Open();

					// First, check if the employee exists
					string checkQuery = "SELECT COUNT(*) FROM [Namib Mills].[dbo].[Emp_List] WHERE ID = @ID";

					using (var checkCommand = new SqlCommand(checkQuery, connection))
					{
						checkCommand.Parameters.AddWithValue("@ID", employeeId);
						int employeeCount = (int)checkCommand.ExecuteScalar();

						if (employeeCount == 0)
						{
							// Employee not found
							return 0;
						}
					}

					// Proceed with deletion
					var query = "DELETE FROM [Namib Mills].[dbo].[Emp_List] WHERE ID = @ID";
					using (var command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@ID", employeeId);

						// Execute and return the number of rows affected
						int rowsAffected = command.ExecuteNonQuery();

						return rowsAffected;
					}
				}
			}

			catch (SqlException ex)
			{
				
				throw; // Re-throw to allow caller to handle or log as needed
			}
		}
	}

}
