# Security Patrol Backend

Backend services for the Security Patrol mobile application, providing API endpoints for authentication, location tracking, time tracking, patrol management, photo storage, and activity reporting.

## Architecture

The backend follows a clean architecture pattern with the following layers:

### Core Layer

Contains domain entities, models, interfaces, and business rules. This layer has no dependencies on other layers or external frameworks.

### Application Layer

Contains business logic, service implementations, and validators. This layer depends only on the Core layer.

### Infrastructure Layer

Contains implementations of interfaces defined in the Core layer, including database access, external service integrations, and infrastructure concerns. This layer depends on the Core and Application layers.

### API Layer

Contains API controllers, middleware, and configuration. This layer depends on all other layers and serves as the entry point for the application.

## Technologies

The backend is built using the following technologies:

### Framework
.NET 8.0 with ASP.NET Core Web API

### Database
Microsoft SQL Server with Entity Framework Core 8.0

### Authentication
JWT Bearer token authentication with phone number verification

### Documentation
Swagger/OpenAPI with versioning support

### Logging
Serilog with console and file sinks

### Storage
Azure Blob Storage for file storage (photos)

### Containerization
Docker with multi-stage builds for development and production

## Getting Started

Follow these instructions to set up and run the backend services locally.

### Prerequisites

- .NET 8.0 SDK
- Docker and Docker Compose
- Visual Studio 2022 or Visual Studio Code
- SQL Server (local or containerized)

### Development Setup

1. Clone the repository
2. Navigate to the `src/backend` directory
3. Run `dotnet restore` to restore dependencies
4. Set up user secrets for local development:
   ```
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=SecurityPatrol;Trusted_Connection=True;TrustServerCertificate=True;"
   dotnet user-secrets set "JWT:SecretKey" "your-secret-key-here"
   dotnet user-secrets set "SmsService:ApiKey" "your-sms-api-key-here"
   ```
5. Run database migrations:
   ```
   dotnet ef database update --project SecurityPatrol.Infrastructure --startup-project SecurityPatrol.API
   ```
6. Run the application:
   ```
   dotnet run --project SecurityPatrol.API
   ```

### Docker Setup

1. Navigate to the `src/backend` directory
2. Create a `.env` file with the following variables:
   ```
   DB_PASSWORD=your-strong-password
   JWT_SECRET_KEY=your-secret-key-here
   SMS_API_KEY=your-sms-api-key-here
   ```
3. Run the application using Docker Compose:
   ```
   docker-compose up -d
   ```
4. For development environment with hot reload:
   ```
   docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d
   ```

## API Endpoints

The backend provides the following API endpoints:

### Authentication

- `POST /api/v1/auth/verify` - Request verification code
- `POST /api/v1/auth/validate` - Validate verification code
- `POST /api/v1/auth/refresh` - Refresh authentication token

### Time Tracking

- `POST /api/v1/time/clock` - Record clock in/out events
- `GET /api/v1/time/history` - Retrieve clock history

### Location

- `POST /api/v1/location/batch` - Upload location data batch

### Photos

- `POST /api/v1/photos/upload` - Upload captured photos
- `GET /api/v1/photos` - Retrieve photos

### Reports

- `POST /api/v1/reports` - Submit activity reports
- `GET /api/v1/reports` - Retrieve activity reports

### Patrol

- `GET /api/v1/patrol/locations` - Get available patrol locations
- `GET /api/v1/patrol/checkpoints` - Get checkpoints for location
- `POST /api/v1/patrol/verify` - Verify checkpoint completion

## Project Structure

The solution is organized into the following projects:

### SecurityPatrol.Core

Contains domain entities, models, interfaces, and business rules.

### SecurityPatrol.Application

Contains business logic, service implementations, and validators.

### SecurityPatrol.Infrastructure

Contains implementations of interfaces defined in the Core layer, including database access, external service integrations, and infrastructure concerns.

### SecurityPatrol.API

Contains API controllers, middleware, and configuration.

### SecurityPatrol.UnitTests

Contains unit tests for all layers of the application.

### SecurityPatrol.IntegrationTests

Contains integration tests for API endpoints and database operations.

## Database Migrations

The application uses Entity Framework Core migrations to manage database schema changes.

### Creating a Migration

```
dotnet ef migrations add MigrationName --project SecurityPatrol.Infrastructure --startup-project SecurityPatrol.API
```

### Applying Migrations

```
dotnet ef database update --project SecurityPatrol.Infrastructure --startup-project SecurityPatrol.API
```

### Reverting Migrations

```
dotnet ef database update MigrationName --project SecurityPatrol.Infrastructure --startup-project SecurityPatrol.API
```

## Testing

The solution includes both unit tests and integration tests.

### Running Unit Tests

```
dotnet test SecurityPatrol.UnitTests
```

### Running Integration Tests

```
dotnet test SecurityPatrol.IntegrationTests
```

### Running All Tests

```
dotnet test
```

## Deployment

The application can be deployed using Docker to various environments.

### Production Deployment

1. Build the production Docker image:
   ```
   docker build -t securitypatrol-api:latest --target production .
   ```
2. Deploy using Docker Compose:
   ```
   docker-compose -f docker-compose.yml up -d
   ```

### Azure Deployment

The application can be deployed to Azure using Azure App Service with Docker support and Azure SQL Database. Refer to the infrastructure documentation for details.

## Configuration

The application uses the following configuration settings:

### Connection Strings

- `ConnectionStrings:DefaultConnection` - SQL Server connection string

### JWT Authentication

- `JWT:SecretKey` - Secret key for JWT token generation
- `JWT:Issuer` - Token issuer (default: SecurityPatrol.API)
- `JWT:Audience` - Token audience (default: SecurityPatrol.Client)
- `JWT:TokenExpirationHours` - Token expiration in hours (default: 8)

### SMS Service

- `SmsService:ApiKey` - API key for SMS service

### Storage

- `Storage:BasePath` - Base path for file storage

### Feature Flags

- `FeatureManagement:DetailedErrorMessages` - Enable detailed error messages
- `FeatureManagement:EnableSwagger` - Enable Swagger documentation

### Security

- `Security:RequireHttps` - Require HTTPS for all requests

## Contributing

Please follow these guidelines when contributing to the project:

### Coding Standards

- Follow the .NET coding conventions
- Use meaningful names for classes, methods, and variables
- Write XML documentation comments for public APIs
- Keep methods small and focused on a single responsibility
- Use dependency injection for all services

### Pull Request Process

1. Ensure all tests pass before submitting a pull request
2. Update documentation as needed
3. Include unit tests for new functionality
4. Follow the conventional commit message format

## License

This project is licensed under the MIT License - see the LICENSE file for details.