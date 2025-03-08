# Security Patrol Mobile Application

A .NET MAUI mobile application for security personnel to track activities, locations, and complete checkpoint-based patrols.

## Overview

The Security Patrol application is designed for security personnel and patrol officers to efficiently manage their patrol activities. It provides features for authentication, time tracking, location monitoring, photo documentation, activity reporting, and checkpoint verification during patrols.

## Key Features

- Phone number-based authentication with verification code
- Clock-in/out functionality with historical record keeping
- Continuous GPS location tracking during active shifts
- In-app photo capture and storage
- Activity reporting with note-taking capabilities
- Interactive patrol management with map-based checkpoint verification

## Technology Stack

- .NET MAUI 8.0+ for cross-platform development (Android-focused)
- C# as the primary programming language
- XAML for UI definition
- SQLite for local data storage
- REST APIs for backend communication
- MVVM architecture pattern

## Prerequisites

- Visual Studio 2022 with .NET MAUI workload
- Android SDK (minimum API level 26 / Android 8.0)
- Android Emulator or physical device for testing
- Active internet connection for API communication

## Getting Started

1. Clone the repository
2. Open the solution in Visual Studio 2022
3. Restore NuGet packages
4. Configure the API endpoints in ApiEndpoints.cs
5. Build and run the application on an Android device or emulator

## Project Structure

- **Constants/** - Application constants and configuration
- **Converters/** - XAML value converters
- **Database/** - SQLite database implementation
- **Helpers/** - Utility and helper classes
- **Models/** - Data models and DTOs
- **Platforms/** - Platform-specific implementations
- **Resources/** - Application resources (images, fonts, styles)
- **Services/** - Business logic and service implementations
- **ViewModels/** - MVVM view models
- **Views/** - XAML UI pages and controls

## Architecture

The application follows the MVVM (Model-View-ViewModel) architecture pattern with a service-oriented approach. It implements a local-first data strategy with background synchronization for offline capability. The application uses dependency injection for service resolution and maintains a clear separation between UI, business logic, and data access layers.

## Offline Capability

The application is designed to work offline with a local-first approach. All data is stored locally in SQLite and synchronized with the backend when connectivity is available. This ensures the application remains functional in areas with poor or no connectivity.

## Authentication

Authentication is handled via phone number verification. The user enters their phone number, receives a verification code via SMS, and enters the code to authenticate. Authentication tokens are securely stored on the device for subsequent sessions.

## Building and Deployment

- Debug Build: `dotnet build -c Debug`
- Release Build: `dotnet build -c Release`
- Android APK: `dotnet publish -f net8.0-android -c Release`
- The application can be deployed to devices via Visual Studio or using the generated APK

## Testing

The project includes unit tests, integration tests, and UI tests to ensure functionality and reliability. Tests can be run using the Visual Studio Test Explorer or via command line with `dotnet test`.

## Contributing

Contributions to the project are welcome. Please follow the coding standards and submit pull requests for review. Ensure all tests pass before submitting changes.

## License

This project is licensed under the terms specified in the LICENSE file.