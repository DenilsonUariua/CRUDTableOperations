# C# WPF CRUD Operations Application

A simple WPF (Windows Presentation Foundation) application for performing CRUD (Create, Read, Update, Delete) operations on a **Cars** table in an SQL Server database. The application demonstrates how to build a functional desktop application with a DataGrid for data visualization and includes dynamic database connection capabilities.

---

## 🚀 Features

- **CRUD Operations**:
  - Add new cars to the database.
  - View and filter existing car records.
  - Update details of existing cars.
  - Delete cars from the database.
- **Dynamic Database Connection**:
  - Connect to different SQL Server databases at runtime via a user-friendly interface.
  - Supports both **Windows Authentication** and **SQL Server Authentication**.
- **Filter and Search**:
  - Search for cars based on `Make`, `Model`, `Year`, and `Price`.
  - Real-time filtering in the DataGrid.
- **Modern UI**:
  - Designed using WPF with responsive layouts and styles for an intuitive user experience.

---

## 🗄️ Database Setup

### Prerequisites
1. Microsoft SQL Server installed and running.
2. A database (e.g., `CarsDatabase`) created in SQL Server.

### Create the Cars Table
Run the following SQL script to create the `Cars` table in your database:

```sql
CREATE TABLE Cars (
    CarID INT IDENTITY(1,1) PRIMARY KEY,  
    Make NVARCHAR(50) NOT NULL,           
    Model NVARCHAR(50) NOT NULL,         
    Year INT NOT NULL,                   
    Price DECIMAL(10, 2) NOT NULL
);
```

### Insert values into the Cars Table
Run the following SQL script to insert 10 cars into your table:

```sql
INSERT INTO Cars (Make, Model, Year, Price) VALUES
('Toyota', 'Corolla', 2020, 20000.00),
('Honda', 'Civic', 2019, 18000.00),
('Ford', 'Mustang', 2021, 35000.00),
('Chevrolet', 'Malibu', 2018, 22000.00),
('BMW', '3 Series', 2022, 45000.00),
('Audi', 'A4', 2021, 42000.00),
('Mercedes', 'C-Class', 2020, 48000.00),
('Hyundai', 'Elantra', 2019, 17000.00),
('Nissan', 'Altima', 2020, 21000.00),
('Volkswagen', 'Jetta', 2021, 24000.00);
```
