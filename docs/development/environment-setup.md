## Introduction

This document provides detailed instructions for setting up your development environment for the Security Patrol application. Following these steps will ensure you have all the necessary tools and configurations to develop, test, and debug both the mobile application and backend services.

The Security Patrol application consists of a .NET MAUI mobile application targeting Android devices and a .NET-based backend API. This guide covers the setup for both components.

## Prerequisites

Before beginning the setup process, ensure you have the following prerequisites:

### Hardware Requirements
- **Processor**: 64-bit, multi-core processor (Intel or AMD)
- **RAM**: 16 GB minimum, 32 GB recommended
- **Disk Space**: 100 GB free space minimum (SSD recommended)
- **Display**: 1920 x 1080 resolution or higher
- **Internet Connection**: Broadband connection for downloads and package restoration

### Operating System Requirements
- **Windows**: Windows 10 version 1909 or higher, or Windows 11
- **macOS**: macOS Monterey (12.0) or higher (for Mac users)
- **Linux**: Ubuntu 20.04 or higher (limited support for .NET MAUI development)

### Access Requirements
- GitHub account with access to the Security Patrol repository
- Azure DevOps account (if applicable)
- Development environment credentials (if applicable)
- Azure subscription (for backend deployment and testing)

## Required Software Installation

Install the following software components to set up your development environment:

### Visual Studio 2022
1. Download Visual Studio 2022 (Community, Professional, or Enterprise edition) from [Visual Studio Downloads](https://visualstudio.microsoft.com/downloads/)

2. During installation, select the following workloads:
   - **Mobile development with .NET** (includes .NET MAUI)
   - **ASP.NET and web development** (for backend API)
   - **.NET desktop development**
   - **Azure development** (recommended)

3. Under Individual components, ensure the following are selected:
   - **Android SDK setup (API level 33)** or higher
   - **Android emulator**
   - **.NET MAUI runtime**
   - **.NET 8.0 SDK**

4. Complete the installation and restart your computer if prompted

### .NET 8.0 SDK
If not installed with Visual Studio:

1. Download the .NET 8.0 SDK from [.NET Downloads](https://dotnet.microsoft.com/download/dotnet/8.0)

2. Follow the installer instructions

3. Verify installation by opening a command prompt or terminal and running:
   ```
   dotnet --version
   ```
   The output should show version 8.0.x or higher

### Android SDK and Emulator
If not installed with Visual Studio:

1. Download and install Android Studio from [Android Developer site](https://developer.android.com/studio)

2. During installation, ensure the Android SDK and Android Emulator are selected

3. After installation, open Android Studio and go to SDK Manager

4. Install the following components:
   - Android SDK Platform API level 33 or higher
   - Android SDK Build-Tools
   - Android Emulator
   - Android SDK Platform-Tools
   - Google Play Services

5. Configure environment variables:
   - Set `ANDROID_HOME` to the Android SDK installation path
   - Add `%ANDROID_HOME%\platform-tools` to your PATH

6. Create an Android Virtual Device (AVD) for testing:
   - Open Android Studio
   - Go to AVD Manager
   - Click "Create Virtual Device"
   - Select a device definition (e.g., Pixel 6)
   - Select a system image with API level 33 or higher
   - Complete the AVD creation process

### Git
1. Download Git from [Git Downloads](https://git-scm.com/downloads)

2. During installation:
   - Choose the default editor you prefer
   - Select "Use Git from the Windows Command Prompt" (Windows)
   - Select "Checkout as-is, commit as-is" for line ending conversions
   - Choose your preferred terminal emulator

3. Verify installation by opening a command prompt or terminal and running:
   ```
   git --version
   ```

4. Configure Git with your identity:
   ```
   git config --global user.name "Your Name"
   git config --global user.email "your.email@example.com"
   ```

### Docker Desktop
1. Download Docker Desktop from [Docker Downloads](https://www.docker.com/products/docker-desktop)

2. Follow the installation instructions for your operating system

3. Start Docker Desktop and ensure it's running properly

4. Verify installation by opening a command prompt or terminal and running:
   ```
   docker --version
   docker-compose --version
   ```

### SQL Server
You can use one of the following options for SQL Server:

**Option 1: SQL Server Developer Edition**
1. Download SQL Server Developer Edition from [SQL Server Downloads](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
2. Follow the installation instructions
3. Install SQL Server Management Studio (SSMS) or Azure Data Studio for database management

**Option 2: SQL Server Express**
1. Download SQL Server Express from [SQL Server Downloads](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
2. Follow the installation instructions
3. Install SQL Server Management Studio (SSMS) or Azure Data Studio for database management

**Option 3: SQL Server in Docker**
1. Ensure Docker Desktop is running
2. Open a command prompt or terminal and run:
   ```
   docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2019-latest
   ```
3. This will download and run SQL Server 2019 in a Docker container
4. Connect to the SQL Server instance using Azure Data Studio or SSMS with:
   - Server: localhost,1433
   - Authentication: SQL Login
   - Username: sa
   - Password: YourStrong!Passw0rd

### Azure CLI
1. Download Azure CLI from [Azure CLI Downloads](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)

2. Follow the installation instructions for your operating system

3. Verify installation by opening a command prompt or terminal and running:
   ```
   az --version
   ```

4. Log in to your Azure account:
   ```
   az login
   ```

### Recommended Visual Studio Extensions
Install the following Visual Studio extensions to enhance your development experience:

1. **.NET MAUI Toolkit**: Provides additional tools and templates for .NET MAUI development

2. **XAML Styler**: Formats XAML code according to configurable rules

3. **CodeMaid** or **Productivity Power Tools**: Provides additional code cleanup and organization features

4. **GitHub Extension for Visual Studio**: Enhances GitHub integration

5. **SQLite/SQL Server Compact Toolbox**: Helps manage SQLite databases

6. **Markdown Editor**: Improves editing of markdown documentation files

To install extensions in Visual Studio:
1. Go to Extensions > Manage Extensions
2. Search for each extension by name
3. Click Download and follow the installation instructions
4. Restart Visual Studio when prompted

## Repository Setup

Follow these steps to set up the Security Patrol repository on your local machine:

### Cloning the Repository
1. Open a command prompt or terminal

2. Navigate to the directory where you want to clone the repository

3. Clone the repository using Git:
   ```
   git clone https://github.com/your-organization/security-patrol.git
   ```
   Replace `your-organization` with the actual GitHub organization name

4. Navigate to the cloned repository:
   ```
   cd security-patrol
   ```

5. Set up the development branch:
   ```
   git checkout -b develop origin/develop
   git pull
   ```

### Repository Structure Overview
The repository is organized with the following structure:

```
/
├── .github/            # GitHub workflows and templates
├── docs/               # Documentation
├── infrastructure/     # Infrastructure as code and deployment scripts
├── src/                # Source code
│   ├── android/        # Mobile application code
│   ├── backend/        # Backend services code
│   └── test/           # Test projects
├── .editorconfig      # Editor configuration
├── .gitattributes     # Git attributes
├── .gitignore         # Git ignore rules
├── CONTRIBUTING.md    # Contribution guidelines
├── LICENSE            # Project license
└── README.md          # Project overview
```

For a more detailed explanation of the repository structure, refer to the [Getting Started Guide](getting-started.md#repository-structure).

### EditorConfig Setup
The repository includes an `.editorconfig` file that configures code style and formatting rules. Visual Studio 2022 supports EditorConfig by default.

1. Ensure EditorConfig support is enabled in Visual Studio:
   - Go to Tools > Options
   - Navigate to Text Editor > General
   - Ensure "Follow EditorConfig conventions" is checked

2. The `.editorconfig` file will be automatically applied when you open files in the repository

3. For other editors, install the appropriate EditorConfig plugin if needed

## Mobile Application Setup

Follow these steps to set up the .NET MAUI mobile application for development:

### Opening the Solution
1. Open Visual Studio 2022

2. Select "Open a project or solution"

3. Navigate to the cloned repository and open `src/android/SecurityPatrol.sln`

4. Wait for Visual Studio to load the solution and restore NuGet packages

5. If prompted to install additional components, follow the instructions

### Restoring NuGet Packages
1. Right-click on the solution in Solution Explorer

2. Select "Restore NuGet Packages"

3. Wait for the package restoration to complete

4. Alternatively, you can restore packages from the command line:
   ```
   cd src/android
   dotnet restore
   ```

### Configuring Android Emulator
1. In Visual Studio, go to Tools > Android > Android Device Manager

2. If you already created an emulator in Android Studio, it should appear in the list

3. To create a new emulator:
   - Click "New"
   - Select a device definition (e.g., Pixel 6)
   - Select a system image with API level 33 or higher
   - Configure other options as needed
   - Click "Create"

4. Start the emulator by selecting it and clicking "Start"

5. Verify the emulator starts correctly and reaches the Android home screen

### Configuring Application Settings
1. Open `src/android/SecurityPatrol/Constants/AppConstants.cs`

2. Update the API endpoint URLs to point to your development environment:
   ```csharp
   public static class ApiEndpoints
   {
       // For local development with backend running in Docker
       public const string BaseUrl = "http://localhost:5000/api/v1";
       
       // For local development with backend running in Visual Studio
       // public const string BaseUrl = "https://localhost:7001/api/v1";
       
       // Uncomment and use this for development environment
       // public const string BaseUrl = "https://dev-securitypatrol-api.azurewebsites.net/api/v1";
   }
   ```

3. Save the file

### Building and Running the Application
1. In Visual Studio, set the startup project:
   - Right-click on `SecurityPatrol` project in Solution Explorer
   - Select "Set as Startup Project"

2. Select the deployment target:
   - In the toolbar, select the Android emulator from the dropdown

3. Build the solution:
   - Press F6 or select Build > Build Solution

4. Run the application:
   - Press F5 or select Debug > Start Debugging
   - The application should build, deploy to the emulator, and start

5. If you encounter any issues, check the Error List and Output windows for details

### Debugging the Mobile Application
1. Set breakpoints in your code by clicking in the left margin of the code editor

2. Run the application in debug mode (F5)

3. When the application reaches a breakpoint, execution will pause

4. Use the debugging tools to inspect variables, step through code, and evaluate expressions

5. Use the Android Device Log (Debug > Windows > Android > Device Log) to view Android system logs

6. Use the Output window to view build and deployment messages

7. For XAML debugging, use Live Visual Tree (Debug > Windows > Live Visual Tree) to inspect the visual tree

## Backend API Setup

Follow these steps to set up the backend API for development:

### Opening the Solution
1. Open Visual Studio 2022

2. Select "Open a project or solution"

3. Navigate to the cloned repository and open `src/backend/SecurityPatrol.sln`

4. Wait for Visual Studio to load the solution and restore NuGet packages

5. If prompted to install additional components, follow the instructions

### Restoring NuGet Packages
1. Right-click on the solution in Solution Explorer

2. Select "Restore NuGet Packages"

3. Wait for the package restoration to complete

4. Alternatively, you can restore packages from the command line:
   ```
   cd src/backend
   dotnet restore
   ```

### Configuring the Database
1. Ensure SQL Server is running (local instance or Docker container)

2. Open `src/backend/SecurityPatrol.API/appsettings.Development.json`

3. Update the connection string to point to your SQL Server instance:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost,1433;Database=SecurityPatrol;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
     },
     // Other settings...
   }
   ```

4. Save the file

5. Open Package Manager Console in Visual Studio:
   - Go to View > Other Windows > Package Manager Console

6. Set the Default Project to `SecurityPatrol.Infrastructure`

7. Run the following command to apply database migrations:
   ```
   Update-Database
   ```

8. Alternatively, you can apply migrations from the command line:
   ```
   cd src/backend
   dotnet ef database update --project SecurityPatrol.Infrastructure --startup-project SecurityPatrol.API
   ```

9. Verify the database was created successfully by connecting to it using SQL Server Management Studio or Azure Data Studio

### Configuring Application Settings
1. Open `src/backend/SecurityPatrol.API/appsettings.Development.json`

2. Review and update the following settings as needed:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost,1433;Database=SecurityPatrol;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft": "Warning",
         "Microsoft.Hosting.Lifetime": "Information"
       }
     },
     "JwtSettings": {
       "Secret": "your-development-secret-key-at-least-32-characters",
       "Issuer": "SecurityPatrol",
       "Audience": "SecurityPatrolApp",
       "ExpiryMinutes": 480
     },
     "SmsService": {
       "ApiKey": "your-sms-service-api-key",
       "FromNumber": "+15555555555"
     },
     "StorageSettings": {
       "PhotoContainer": "photos",
       "ConnectionString": "UseDevelopmentStorage=true"
     },
     "AllowedOrigins": [
       "http://localhost:5000",
       "https://localhost:7001"
     ]
   }
   ```

3. For development purposes, you can use the default values or update them as needed

4. For SMS service integration, you may need to obtain API keys from your SMS provider or use a mock service for development

### Building and Running the API
1. In Visual Studio, set the startup project:
   - Right-click on `SecurityPatrol.API` project in Solution Explorer
   - Select "Set as Startup Project"

2. Build the solution:
   - Press F6 or select Build > Build Solution

3. Run the API:
   - Press F5 or select Debug > Start Debugging
   - The API should start and open the Swagger UI in your browser

4. Verify the API is running correctly by checking the Swagger UI and testing some endpoints

5. Note the URL of the API (typically https://localhost:7001 or http://localhost:5000) for configuring the mobile application

### Using Docker for Backend Development
Alternatively, you can use Docker to run the backend services:

1. Ensure Docker Desktop is running

2. Open a command prompt or terminal

3. Navigate to the backend directory:
   ```
   cd src/backend
   ```

4. Build and start the Docker containers:
   ```
   docker-compose up -d
   ```

5. Verify the containers are running:
   ```
   docker-compose ps
   ```

6. Access the API Swagger UI at http://localhost:5000/swagger

7. To stop the containers:
   ```
   docker-compose down
   ```

### Debugging the API
1. Set breakpoints in your code by clicking in the left margin of the code editor

2. Run the API in debug mode (F5)

3. When the API receives a request that triggers a breakpoint, execution will pause

4. Use the debugging tools to inspect variables, step through code, and evaluate expressions

5. Use the Output window to view logs and messages

6. For debugging API requests, you can use:
   - Swagger UI built into the API
   - Postman or similar API testing tool
   - The mobile application configured to point to your local API

## Development Tools Configuration

Configure additional development tools to enhance your development experience:

### Git Configuration
1. Configure Git user information (if not already done):
   ```
   git config --global user.name "Your Name"
   git config --global user.email "your.email@example.com"
   ```

2. Configure line endings:
   - For Windows:
     ```
     git config --global core.autocrlf true
     ```
   - For macOS/Linux:
     ```
     git config --global core.autocrlf input
     ```

3. Configure Git to use your preferred editor:
   ```
   git config --global core.editor "code --wait"  # For Visual Studio Code
   ```

4. Set up Git credentials storage to avoid repeated password prompts:
   ```
   git config --global credential.helper store  # Stores credentials indefinitely
   ```
   Or for a more secure option:
   ```
   git config --global credential.helper cache  # Caches credentials for 15 minutes
   ```

### Visual Studio Configuration
1. Configure code style and formatting:
   - Go to Tools > Options
   - Navigate to Text Editor > C# > Code Style
   - Review and adjust settings as needed
   - The project's `.editorconfig` file will override many of these settings

2. Configure IntelliSense:
   - Go to Tools > Options
   - Navigate to Text Editor > C# > IntelliSense
   - Adjust settings as needed

3. Configure debugging settings:
   - Go to Tools > Options
   - Navigate to Debugging
   - Adjust settings as needed

4. Configure source control integration:
   - Go to Tools > Options
   - Navigate to Source Control > Git Global Settings
   - Adjust settings as needed

### Code Analysis Configuration
1. Enable code analysis in Visual Studio:
   - Go to Tools > Options
   - Navigate to Text Editor > C# > Advanced
   - Ensure "Enable full solution analysis" is checked

2. Configure code analysis severity:
   - Go to Tools > Options
   - Navigate to Text Editor > C# > Code Analysis
   - Adjust severity levels as needed

3. The project includes StyleCop Analyzers and other code analysis tools configured in the project files

4. Review the [Coding Standards](coding-standards.md) document for details on code style and analysis rules

### Database Tools
1. **SQL Server Management Studio (SSMS)**:
   - Download and install from [SSMS Downloads](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms)
   - Connect to your SQL Server instance
   - Use for database management, query execution, and administration

2. **Azure Data Studio**:
   - Download and install from [Azure Data Studio Downloads](https://docs.microsoft.com/en-us/sql/azure-data-studio/download-azure-data-studio)
   - Connect to your SQL Server instance
   - Use for database management, query execution, and visualization

3. **SQLite Browser**:
   - Download and install from [SQLite Browser](https://sqlitebrowser.org/)
   - Use for inspecting and managing SQLite databases used by the mobile application

### API Testing Tools
1. **Postman**:
   - Download and install from [Postman Downloads](https://www.postman.com/downloads/)
   - Create a new collection for the Security Patrol API
   - Set up environment variables for different environments (local, development, etc.)
   - Import API definitions from Swagger/OpenAPI

2. **Swagger UI**:
   - Built into the API and accessible at `/swagger` when the API is running
   - Use for exploring and testing API endpoints
   - View request and response schemas

3. **REST Client for VS Code**:
   - If you use VS Code, install the REST Client extension
   - Create `.http` files to define and execute API requests
   - Store request collections in the repository for team sharing

## Troubleshooting Common Issues

This section provides solutions for common issues you might encounter during development environment setup:

### Visual Studio Installation Issues
- **Issue**: Visual Studio installer fails or crashes
  **Solution**: Run the installer as administrator, ensure Windows is up to date, or try the offline installer

- **Issue**: Missing workloads or components
  **Solution**: Run the Visual Studio Installer, select "Modify", and add the required workloads and components

- **Issue**: Unable to create or start Android emulator
  **Solution**: Ensure Hyper-V is enabled, HAXM is installed, or try using a physical device

- **Issue**: .NET MAUI workload not available
  **Solution**: Ensure you have Visual Studio 2022 17.3 or later, or install .NET MAUI separately using the dotnet CLI

### Android Emulator Issues
- **Issue**: Emulator fails to start
  **Solution**: Check if Hyper-V is enabled, ensure HAXM is installed, or try a different emulator configuration

- **Issue**: Emulator starts but app doesn't deploy
  **Solution**: Restart the emulator, ensure ADB is running, or try a physical device

- **Issue**: Slow emulator performance
  **Solution**: Allocate more RAM and CPU cores to the emulator, use an x86 system image, or enable hardware acceleration

- **Issue**: App crashes on startup in emulator
  **Solution**: Check the Android Device Log for error details, ensure the correct API level is targeted, or try a different emulator configuration

### NuGet Package Restoration Issues
- **Issue**: Unable to restore NuGet packages
  **Solution**: Check internet connection, ensure NuGet sources are configured correctly, or try clearing the NuGet cache

- **Issue**: Package conflicts or version issues
  **Solution**: Check for package version conflicts, update packages to compatible versions, or add package binding redirects

- **Issue**: Authentication issues with private NuGet feeds
  **Solution**: Ensure credentials are configured correctly, or try authenticating manually

- **Issue**: Corrupted packages
  **Solution**: Clear the NuGet cache using `dotnet nuget locals all --clear` and try again

### Database Connection Issues
- **Issue**: Unable to connect to SQL Server
  **Solution**: Verify SQL Server is running, check connection string, ensure firewall allows connections, or try using SQL Server Configuration Manager to troubleshoot

- **Issue**: Migration fails to apply
  **Solution**: Check for migration errors in the Package Manager Console output, ensure the database exists, or try recreating the database

- **Issue**: Permission denied errors
  **Solution**: Ensure the user in the connection string has appropriate permissions, or try using a different authentication method

- **Issue**: Database already exists
  **Solution**: Drop the existing database or use a different database name

### Docker Issues
- **Issue**: Docker Desktop fails to start
  **Solution**: Ensure Hyper-V is enabled, check system requirements, or reinstall Docker Desktop

- **Issue**: Container fails to start
  **Solution**: Check Docker logs for error details, ensure ports are not in use, or try rebuilding the container

- **Issue**: Unable to connect to services in containers
  **Solution**: Verify port mappings, check network configuration, or try accessing the service from within the container

- **Issue**: Volume mounting issues
  **Solution**: Check path syntax, ensure paths exist, or try using absolute paths

### Git Issues
- **Issue**: Unable to clone repository
  **Solution**: Verify GitHub credentials, check repository URL, or try using SSH instead of HTTPS

- **Issue**: Permission denied when pushing
  **Solution**: Verify GitHub credentials, ensure you have write access to the repository, or check if branch protection rules are in place

- **Issue**: Merge conflicts
  **Solution**: Resolve conflicts manually, use a merge tool, or consider rebasing instead of merging

- **Issue**: Large files causing issues
  **Solution**: Use Git LFS for large files, avoid committing binary files, or add large files to .gitignore

### Build and Compilation Issues
- **Issue**: Build errors in .NET MAUI project
  **Solution**: Check for missing NuGet packages, ensure Android SDK is properly installed, or try cleaning and rebuilding the solution

- **Issue**: XAML compilation errors
  **Solution**: Check XAML syntax, ensure referenced resources exist, or look for missing namespace declarations

- **Issue**: Backend API build errors
  **Solution**: Check for missing NuGet packages, ensure .NET SDK is properly installed, or try cleaning and rebuilding the solution

- **Issue**: Code analysis errors
  **Solution**: Address the reported issues, suppress specific rules if necessary, or adjust code analysis severity levels

## Environment Verification

After completing the setup, verify your development environment is working correctly:

### Mobile Application Verification
1. Build and run the mobile application on the Android emulator

2. Verify the application starts without errors

3. Test basic functionality such as navigation and UI rendering

4. If the backend API is running, test API communication

5. Run unit tests to ensure the development environment is correctly configured:
   - In Visual Studio, open Test Explorer (Test > Test Explorer)
   - Run all tests for the mobile application
   - Verify tests pass without errors

### Backend API Verification
1. Build and run the backend API

2. Access the Swagger UI at `/swagger`

3. Test basic API endpoints to ensure they respond correctly

4. Verify database connectivity by executing operations that access the database

5. Run unit tests to ensure the development environment is correctly configured:
   - In Visual Studio, open Test Explorer (Test > Test Explorer)
   - Run all tests for the backend API
   - Verify tests pass without errors

### End-to-End Verification
1. Start both the backend API and mobile application

2. Configure the mobile application to use the local backend API

3. Test end-to-end functionality such as authentication, data synchronization, and API communication

4. Verify that data flows correctly between the mobile application and backend API

5. Test offline functionality and synchronization when connectivity is restored

## Next Steps

Now that your development environment is set up, you can start contributing to the Security Patrol application. Here are some next steps:

### Learn the Codebase
1. Review the [Getting Started Guide](getting-started.md) for an overview of the application architecture and components

2. Explore the codebase to understand its structure and organization

3. Review the [Coding Standards](coding-standards.md) to understand the project's coding conventions

4. Run and debug the application to understand its functionality

5. Review existing issues and pull requests to understand current development activities

### Start Contributing
1. Pick an issue to work on from the issue tracker

2. Create a feature branch for your work

3. Implement the required changes following the project's coding standards

4. Write tests for your changes

5. Submit a pull request for review

6. Address feedback from code reviews

Refer to the [Getting Started Guide](getting-started.md#development-workflow) for more details on the development workflow.

### Additional Resources
- [.NET MAUI Documentation](https://docs.microsoft.com/dotnet/maui/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/ef/core/)
- [Azure Documentation](https://docs.microsoft.com/azure/)
- [Android Developer Documentation](https://developer.android.com/docs)
- [Git Documentation](https://git-scm.com/doc)
- [Docker Documentation](https://docs.docker.com/)

## Conclusion
This document provided detailed instructions for setting up your development environment for the Security Patrol application. By following these steps, you should have a fully functional development environment for both the mobile application and backend API.

If you encounter any issues not covered in the troubleshooting section, please reach out to the development team for assistance.

Happy coding!