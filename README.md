# Security Patrol Application

A comprehensive solution for security personnel to track activities, locations, and complete checkpoint-based patrols.

## Project Overview

The Security Patrol Application is designed for security personnel and patrol officers to efficiently manage their patrol activities. It consists of a .NET MAUI mobile application (Android-focused) and a .NET backend API service. The solution provides features for authentication, time tracking, location monitoring, photo documentation, activity reporting, and checkpoint verification during patrols.

## Key Features

- Phone number-based authentication with verification code
- Clock-in/out functionality with historical record keeping
- Continuous GPS location tracking during active shifts
- In-app photo capture and storage
- Activity reporting with note-taking capabilities
- Interactive patrol management with map-based checkpoint verification
- Offline-first capability with background synchronization

## Repository Structure

- `src/android/` - Mobile application source code (.NET MAUI)
- `src/backend/` - Backend API services (.NET 8)
- `src/test/` - Test projects for both mobile and backend components
- `infrastructure/` - Infrastructure as Code and deployment scripts
- `docs/` - Documentation files including architecture diagrams

## Technology Stack

### Mobile Application:
- .NET MAUI 8.0+ for cross-platform development (Android-focused)
- C# as the primary programming language
- XAML for UI definition
- SQLite for local data storage
- MVVM architecture pattern

### Backend Services:
- .NET 8.0 with ASP.NET Core Web API
- Clean Architecture pattern with layered design
- Microsoft SQL Server with Entity Framework Core
- JWT Bearer token authentication
- Azure Blob Storage for file storage
- Docker containerization

## Getting Started

Follow these instructions to set up the development environment for both the mobile application and backend services.

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 with .NET MAUI workload
- Android SDK (minimum API level 26 / Android 8.0)
- Android Emulator or physical device for testing
- Docker and Docker Compose (for backend services)
- SQL Server (local or containerized)
- Git for version control

### Mobile Application Setup

1. Clone the repository
2. Navigate to the `src/android` directory
3. Open the `SecurityPatrol.sln` solution in Visual Studio 2022
4. Restore NuGet packages
5. Configure the API endpoints in `src/android/SecurityPatrol/Constants/ApiEndpoints.cs`
6. Build and run the application on an Android device or emulator

### Backend Services Setup

1. Navigate to the `src/backend` directory
2. Run `dotnet restore` to restore dependencies
3. Set up user secrets for local development:
   ```
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=SecurityPatrol;Trusted_Connection=True;TrustServerCertificate=True;"
   dotnet user-secrets set "JWT:SecretKey" "your-secret-key-here"
   dotnet user-secrets set "SmsService:ApiKey" "your-sms-api-key-here"
   ```
4. Run database migrations:
   ```
   dotnet ef database update --project SecurityPatrol.Infrastructure --startup-project SecurityPatrol.API
   ```
5. Run the application:
   ```
   dotnet run --project SecurityPatrol.API
   ```

Alternatively, you can use Docker Compose:

1. Navigate to the `src/backend` directory
2. Create a `.env` file with the required environment variables
3. Run `docker-compose up -d`

## Architecture Overview

The Security Patrol Application follows a client-server architecture with a mobile application client and backend API services.

### Mobile Application Architecture

The mobile application follows the MVVM (Model-View-ViewModel) architecture pattern with a service-oriented approach:

- **Views**: XAML-based UI components
- **ViewModels**: Business logic and state management
- **Models**: Data structures and DTOs
- **Services**: Core functionality implementations
- **Repositories**: Data access and persistence

The application implements a local-first data strategy with SQLite for offline capability and background synchronization with the backend when connectivity is available.

### Backend Architecture

The backend follows a clean architecture pattern with the following layers:

- **Core**: Domain entities, models, interfaces, and business rules
- **Application**: Business logic, service implementations, and validators
- **Infrastructure**: Database access, external service integrations, and infrastructure concerns
- **API**: Controllers, middleware, and configuration

This layered approach ensures separation of concerns and maintainability of the codebase.

## API Endpoints

The backend provides the following API endpoints:

- **Authentication**:
  - `POST /api/v1/auth/verify` - Request verification code
  - `POST /api/v1/auth/validate` - Validate verification code
  - `POST /api/v1/auth/refresh` - Refresh authentication token

- **Time Tracking**:
  - `POST /api/v1/time/clock` - Record clock in/out events
  - `GET /api/v1/time/history` - Retrieve clock history

- **Location**:
  - `POST /api/v1/location/batch` - Upload location data batch

- **Photos**:
  - `POST /api/v1/photos/upload` - Upload captured photos
  - `GET /api/v1/photos` - Retrieve photos

- **Reports**:
  - `POST /api/v1/reports` - Submit activity reports
  - `GET /api/v1/reports` - Retrieve activity reports

- **Patrol**:
  - `GET /api/v1/patrol/locations` - Get available patrol locations
  - `GET /api/v1/patrol/checkpoints` - Get checkpoints for location
  - `POST /api/v1/patrol/verify` - Verify checkpoint completion

## Testing

The solution includes comprehensive test suites for both the mobile application and backend services:

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test component interactions and API endpoints
- **UI Tests**: Test the mobile application user interface
- **Performance Tests**: Test application performance under load

Run tests using the Visual Studio Test Explorer or via command line with `dotnet test`.

## Deployment

- **Mobile Application**: Build and deploy using Visual Studio or CI/CD pipeline to App Center or Google Play Store
- **Backend Services**: Deploy using Docker containers to Azure App Service or Kubernetes cluster

Refer to the deployment documentation in the `infrastructure/` directory for detailed instructions.

## Contributing

Contributions to the project are welcome. Please follow the coding standards and submit pull requests for review. Ensure all tests pass before submitting changes.

## License

This project is licensed under the terms specified in the LICENSE file.