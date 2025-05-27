# ASP.NET Core MVC Project Template

This project template provides a base setup for an ASP.NET Core MVC application with the following features:

## Features

- **ASP.NET Core MVC** architecture
- **Entity Framework Core** with SQLite password-protected database
- **ASP.NET Core Identity** with predefined roles (Admin, Accountant, Auditor, Manager)
- **AdminLTE 3.2.0** integration with local references and RTL support
- **Multilingual Support** with Arabic/English language switching
- **Repository Pattern** implementation with generic CRUD operations and paging

## Getting Started

### Prerequisites

- .NET SDK 7.0 or later
- Visual Studio 2022, Visual Studio Code, or any preferred IDE

### Running the Application

1. Clone or download the repository
2. Open the solution in your preferred IDE
3. Restore NuGet packages
4. Run the application

## Project Structure

- **Controllers/** - Contains MVC controllers
- **Data/** - Contains EF Core DbContext and repository implementations
- **Models/** - Contains domain models and view models
- **Resources/** - Contains localization resources
- **Views/** - Contains Razor views
- **wwwroot/** - Contains static files including AdminLTE assets

## Key Features

### Password-Protected SQLite Database

The application uses SQLite with password protection. The connection string and password are configured in `appsettings.json`.

### ASP.NET Core Identity with Roles

The application includes ASP.NET Core Identity with predefined roles:
- Admin
- Accountant
- Auditor
- Manager

### AdminLTE Integration with RTL Support

The application integrates AdminLTE 3.2.0 with local references. The layout supports both LTR and RTL directions for multilingual support.

### Localization with Arabic/English Support

The application includes localization support for English and Arabic languages. The language can be switched using the toggle in the navbar.

### Repository Pattern

The application implements the repository pattern with generic CRUD operations and paging support. The implementation includes:
- Generic Repository interface and implementation

## Configuration

### Database Configuration

Database connection string and password are configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DataSource=app.db;Cache=Shared;Mode=ReadWriteCreate;Foreign Keys=True"
  },
  "DatabaseSettings": {
    "Password": "StrongP@ssw0rd123!"
  }
}
```

### Localization Configuration

Localization is configured in `Program.cs` and resources are stored in the `Resources` folder.

## License

This project is licensed under the MIT License.
