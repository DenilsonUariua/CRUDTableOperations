# C# WPF CRUD Operations Application

### Create the cars table
```
CREATE TABLE Cars (
    CarID INT IDENTITY(1,1) PRIMARY KEY,  
    Make NVARCHAR(50) NOT NULL,           
    Model NVARCHAR(50) NOT NULL,         
    Year INT NOT NULL,                   
    Price DECIMAL(10, 2) NOT NULL
);
```

### Insert values into the cars table
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


