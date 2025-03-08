# Set default argument for environment
ARG ASPNETCORE_ENVIRONMENT=Production

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copy csproj files first to optimize layer caching
COPY ["SecurityPatrol.sln", "./"]
COPY ["SecurityPatrol.API/SecurityPatrol.API.csproj", "SecurityPatrol.API/"]
COPY ["SecurityPatrol.Application/SecurityPatrol.Application.csproj", "SecurityPatrol.Application/"]
COPY ["SecurityPatrol.Core/SecurityPatrol.Core.csproj", "SecurityPatrol.Core/"]
COPY ["SecurityPatrol.Infrastructure/SecurityPatrol.Infrastructure.csproj", "SecurityPatrol.Infrastructure/"]
# Restore dependencies
RUN dotnet restore
# Copy everything else
COPY . .
# Build and publish
WORKDIR /src/SecurityPatrol.API
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Development stage for debugging and hot reload
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS development
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV ASPNETCORE_URLS=http://+:80
# Create directory for file storage
RUN mkdir -p /app/storage
# Use dotnet watch for hot reload during development
ENTRYPOINT ["dotnet", "watch", "run", "--project", "SecurityPatrol.API/SecurityPatrol.API.csproj", "--urls", "http://+:80"]

# Production stage with minimal footprint
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS production
WORKDIR /app
# Create non-root user for security
RUN adduser -u 5678 --disabled-password --gecos "" appuser
# Copy published files from build stage
COPY --from=build /app/publish .
# Create directory for file storage with proper permissions
RUN mkdir -p /app/storage && chown -R appuser:appuser /app/storage
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
# Use non-root user for security
USER appuser
# Run the application
ENTRYPOINT ["dotnet", "SecurityPatrol.API.dll"]