# Employee Management System

An employee management web application built with **ASP.NET Core MVC**, **Entity Framework Core**, and **Microsoft SQL Server**.

The project provides a centralized system for managing employees, departments, positions, and user access with role-based authorization.

## Technologies

- **Backend:** C#, ASP.NET Core MVC (.NET 10)
- **ORM:** Entity Framework Core 10
- **Database:** Microsoft SQL Server
- **Authentication & Authorization:** ASP.NET Core Identity
- **Frontend:** Razor Views, HTML, CSS, Bootstrap
- **Tools:** Visual Studio / Visual Studio Code, Git, GitHub

## Features

### Employee Management
- Create, view, update, and manage employee records.
- Search and filter employee information.
- Pagination for employee lists.
- Soft delete and restore employee records.

### Department Management
- Create, view, update, and manage departments.

### Position Management
- Manage employee positions.
- Associate employees with departments and positions.

### Dashboard
- Display an overview of employee management data.

### Authentication & Authorization
- User registration and login management through ASP.NET Core Identity.
- Role-based access control with Admin and HR roles.
- User management and account restrictions.
- Protected pages based on user permissions.

## Project Structure

```text
EmployeeManagement.Web/
├── Controllers/       # MVC controllers
├── Data/
│   └── Seed/           # Database and identity seeding
├── Migrations/         # Entity Framework Core migrations
├── Models/             # Application data models
├── Properties/
├── Views/              # Razor views
├── wwwroot/            # Static files (CSS, JavaScript, images)
├── Program.cs          # Application configuration
├── appsettings.json    # General application settings
└── EmployeeManagement.Web.csproj
```

## Getting Started

### Prerequisites

Make sure you have installed:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Microsoft SQL Server (SQL Server Express is supported)
- SQL Server Management Studio (optional)
- Git

### 1. Clone the repository

```bash
git clone https://github.com/NamTruongJr/EmployeeManagement.git
cd EmployeeManagement
```

### 2. Configure the database connection

The application requires a SQL Server connection string.

For local development, configure it using .NET User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\\SQLEXPRESS;Database=EmployeeManagementDb;Trusted_Connection=True;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=True"
```

Adjust the server name and authentication settings to match your local SQL Server installation.

### 3. Configure the initial Admin account

The application can seed an initial Admin account when the following configuration values are provided:

```bash
dotnet user-secrets set "AdminUser:Email" "admin@example.com"
dotnet user-secrets set "AdminUser:Password" "YourStrongPassword123"
```

Use your own email and a password that meets the configured Identity password policy.

**Important:** Do not commit passwords, connection strings, or other secrets to source control.

### 4. Apply database migrations

Install the Entity Framework Core CLI tool if it is not already installed:

```bash
dotnet tool install --global dotnet-ef
```

Then apply the existing migrations:

```bash
dotnet ef database update
```

### 5. Run the application

```bash
dotnet run
```

Open the local URL displayed in the terminal to access the application.

## Security Notes

- Application authentication is managed by ASP.NET Core Identity.
- Access to protected features is controlled through user roles.
- Local development settings and credentials should be stored outside the public repository.

## Project Status

This project was developed as a learning and portfolio project to practice ASP.NET Core MVC, database integration, authentication, authorization, and CRUD application development.

## Screenshots

### Dashboard
![Dashboard](docs/screenshots/01.%20Dashboard.png)

### Employee Management
![Employee Management](docs/screenshots/02.%20Employee%20Management.png)

## Author

**Nam Truong**

GitHub: [NamTruongJr](https://github.com/NamTruongJr)

Repository: [EmployeeManagement](https://github.com/NamTruongJr/EmployeeManagement)