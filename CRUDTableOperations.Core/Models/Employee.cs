using System;
using System.Collections.Generic;
using System.Text;

namespace CRUDTableOperations.Core.Models
{
	public class Employee
	{
		/// <summary>
		/// Primary identifier for the employee record
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// Secondary identifier for the employee record
		/// </summary>
		public int? Id2 { get; set; }

		/// <summary>
		/// Unique employee number
		/// </summary>
		public string EmployeeNumber { get; set; }

		/// <summary>
		/// Employee's surname
		/// </summary>
		public string Surname { get; set; }

		/// <summary>
		/// Employee's first name
		/// </summary>
		public string FirstName { get; set; }

		/// <summary>
		/// Employee's second name
		/// </summary>
		public string SecondName { get; set; }

		/// <summary>
		/// Employee's identification number
		/// </summary>
		public string IdNumber { get; set; }

		/// <summary>
		/// Date the employee joined the group
		/// </summary>
		public DateTime? GroupJoinDate { get; set; }

		/// <summary>
		/// Date of the employee's last discharge
		/// </summary>
		public DateTime? LastDischargeDate { get; set; }

		/// <summary>
		/// Employee's initials
		/// </summary>
		public string Initials { get; set; }
	}
}
