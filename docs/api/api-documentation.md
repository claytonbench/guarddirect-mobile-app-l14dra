# Security Patrol API Documentation

This document provides comprehensive documentation for the Security Patrol API, which serves as the backend for the Security Patrol mobile application. The API enables security personnel to authenticate, track time, record locations, verify patrol checkpoints, capture photos, and submit activity reports.

## Overview

The Security Patrol API follows RESTful principles and uses JSON for data exchange. All API endpoints are secured using token-based authentication, with the exception of the initial authentication endpoints.

### Base URL

All API endpoints are relative to the base URL:

```
https://api.securitypatrol.example.com/
```

### API Versioning

The API uses URL path versioning to ensure backward compatibility as the API evolves. The current version is v1, which is included in the URL path:

```
https://api.securitypatrol.example.com/api/v1/resource
```

## Authentication

The Security Patrol API uses a two-step phone number verification process for authentication, followed by token-based authentication for all secured endpoints.

### Authentication Flow

1. Client requests a verification code by submitting a phone number
2. Server sends a verification code via SMS to the provided phone number
3. Client submits the verification code along with the phone number
4. Server validates the code and issues a JWT authentication token
5. Client includes the token in the Authorization header for subsequent requests
6. When the token approaches expiration, client can request a token refresh

### Authorization Header

For all secured endpoints, include the authentication token in the request headers:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

If the token is missing, invalid, or expired, the server will respond with a 401 Unauthorized status code.

For detailed information about the authentication process, refer to the [Authentication Documentation](authentication.md).

## API Endpoints

The Security Patrol API provides the following categories of endpoints:

- **Authentication**: Phone verification and token management
- **Time Tracking**: Clock in/out operations and history
- **Location Tracking**: Location data submission and retrieval
- **Patrol Management**: Patrol locations, checkpoints, and verification
- **Photo Management**: Photo upload and retrieval
- **Activity Reporting**: Report submission and retrieval

For detailed specifications of all endpoints, refer to the [API Endpoints Documentation](endpoints.md).

### Authentication Endpoints

| Endpoint | Method | Description | Authentication Required |
|----------|--------|-------------|------------------------|
| `/api/auth/verify` | POST | Request verification code | No |
| `/api/auth/validate` | POST | Validate verification code | No |
| `/api/auth/refresh` | POST | Refresh authentication token | Yes |

### Time Tracking Endpoints

| Endpoint | Method | Description | Authentication Required |
|----------|--------|-------------|------------------------|
| `/api/time/clock` | POST | Record clock in/out event | Yes |
| `/api/time/history` | GET | Get clock history | Yes |
| `/api/time/range` | GET | Get clock events by date range | Yes |
| `/api/time/status` | GET | Get current clock status | Yes |

### Location Tracking Endpoints

| Endpoint | Method | Description | Authentication Required |
|----------|--------|-------------|------------------------|
| `/api/location/batch` | POST | Submit location batch | Yes |
| `/api/location/history` | GET | Get location history | Yes |
| `/api/location/current` | GET | Get current location | Yes |

### Patrol Management Endpoints

| Endpoint | Method | Description | Authentication Required |
|----------|--------|-------------|------------------------|
| `/api/patrol/locations` | GET | Get patrol locations | Yes |
| `/api/patrol/locations/{id}` | GET | Get patrol location by ID | Yes |
| `/api/patrol/checkpoints` | GET | Get checkpoints for location | Yes |
| `/api/patrol/checkpoints/{id}` | GET | Get checkpoint by ID | Yes |
| `/api/patrol/verify` | POST | Verify checkpoint | Yes |
| `/api/patrol/status/{locationId}` | GET | Get patrol status | Yes |
| `/api/patrol/nearby` | GET | Get nearby checkpoints | Yes |
| `/api/patrol/verifications` | GET | Get user verifications | Yes |

### Photo Management Endpoints

| Endpoint | Method | Description | Authentication Required |
|----------|--------|-------------|------------------------|
| `/api/photos/upload` | POST | Upload photo | Yes |
| `/api/photos/{id}` | GET | Get photo metadata | Yes |
| `/api/photos/{id}/file` | GET | Get photo file | Yes |
| `/api/photos/user` | GET | Get user photos | Yes |
| `/api/photos/location` | GET | Get photos by location | Yes |
| `/api/photos/daterange` | GET | Get photos by date range | Yes |
| `/api/photos/{id}` | DELETE | Delete photo | Yes |

### Activity Reporting Endpoints

| Endpoint | Method | Description | Authentication Required |
|----------|--------|-------------|------------------------|
| `/api/reports` | POST | Create report | Yes |
| `/api/reports/{id}` | GET | Get report by ID | Yes |
| `/api/reports` | GET | Get all reports | Yes |
| `/api/reports/paged` | GET | Get paginated reports | Yes |
| `/api/reports/{id}` | PUT | Update report | Yes |
| `/api/reports/{id}` | DELETE | Delete report | Yes |

## Data Models

This section describes the key data models used in the Security Patrol API.

### Authentication Models

#### AuthenticationRequest
```json
{
  "phoneNumber": "+15551234567"
}
```

#### VerificationRequest
```json
{
  "phoneNumber": "+15551234567",
  "code": "123456"
}
```

#### AuthenticationResponse
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2023-07-15T14:30:00Z"
}
```

### Time Tracking Models

#### TimeRecordRequest
```json
{
  "type": "in",  // or "out"
  "timestamp": "2023-07-15T08:00:00Z",
  "location": {
    "latitude": 37.7749,
    "longitude": -122.4194
  }
}
```

#### TimeRecordResponse
```json
{
  "id": "12345",
  "status": "success"
}
```

#### TimeRecord
```json
{
  "id": 12345,
  "type": "in",
  "timestamp": "2023-07-15T08:00:00Z",
  "latitude": 37.7749,
  "longitude": -122.4194,
  "userId": "user123"
}
```

#### ClockStatus
```json
{
  "isClocked": true,
  "lastClockInTime": "2023-07-15T08:00:00Z",
  "lastClockOutTime": "2023-07-14T16:00:00Z"
}
```

### Location Models

#### LocationBatchRequest
```json
{
  "locations": [
    {
      "timestamp": "2023-07-15T08:15:00Z",
      "latitude": 37.7749,
      "longitude": -122.4194,
      "accuracy": 10.5
    },
    {
      "timestamp": "2023-07-15T08:16:00Z",
      "latitude": 37.7750,
      "longitude": -122.4195,
      "accuracy": 8.2
    }
  ]
}
```

#### LocationModel
```json
{
  "id": 12345,
  "timestamp": "2023-07-15T08:15:00Z",
  "latitude": 37.7749,
  "longitude": -122.4194,
  "accuracy": 10.5,
  "userId": "user123"
}
```

#### LocationSyncResponse
```json
{
  "processed": 2,
  "failed": 0
}
```

### Patrol Models

#### PatrolLocation
```json
{
  "id": 1,
  "name": "Downtown Office",
  "latitude": 37.7749,
  "longitude": -122.4194
}
```

#### CheckpointModel
```json
{
  "id": 101,
  "locationId": 1,
  "name": "Front Entrance",
  "latitude": 37.7748,
  "longitude": -122.4193
}
```

#### CheckpointVerificationRequest
```json
{
  "checkpointId": 101,
  "timestamp": "2023-07-15T10:30:00Z",
  "location": {
    "latitude": 37.7748,
    "longitude": -122.4193
  }
}
```

#### CheckpointVerificationResponse
```json
{
  "status": "verified"
}
```

#### PatrolStatus
```json
{
  "locationId": 1,
  "totalCheckpoints": 3,
  "verifiedCheckpoints": 2,
  "checkpoints": [
    {
      "checkpointId": 101,
      "isVerified": true,
      "verificationTime": "2023-07-15T10:30:00Z"
    },
    {
      "checkpointId": 102,
      "isVerified": true,
      "verificationTime": "2023-07-15T10:45:00Z"
    },
    {
      "checkpointId": 103,
      "isVerified": false,
      "verificationTime": null
    }
  ]
}
```

### Photo Models

#### PhotoUploadRequest
```json
{
  "timestamp": "2023-07-15T10:30:00Z",
  "latitude": 37.7749,
  "longitude": -122.4194
}
```

#### PhotoUploadResponse
```json
{
  "id": "12345",
  "status": "uploaded"
}
```

#### Photo
```json
{
  "id": 12345,
  "userId": "user123",
  "timestamp": "2023-07-15T10:30:00Z",
  "latitude": 37.7749,
  "longitude": -122.4194,
  "contentType": "image/jpeg"
}
```

### Report Models

#### ReportRequest
```json
{
  "text": "Suspicious activity observed near the loading dock. Individual left when approached.",
  "timestamp": "2023-07-15T14:30:00Z",
  "location": {
    "latitude": 37.7750,
    "longitude": -122.4195
  }
}
```

#### ReportResponse
```json
{
  "id": "12345",
  "status": "created"
}
```

#### Report
```json
{
  "id": 12345,
  "text": "Suspicious activity observed near the loading dock. Individual left when approached.",
  "timestamp": "2023-07-15T14:30:00Z",
  "latitude": 37.7750,
  "longitude": -122.4195,
  "userId": "user123"
}
```

### Common Models

#### Result<T>
```json
{
  "succeeded": true,
  "message": "Operation completed successfully",
  "data": { /* Type T data */ }
}
```

#### PaginatedList<T>
```json
{
  "items": [ /* Array of type T */ ],
  "pageNumber": 1,
  "totalPages": 5,
  "totalCount": 42
}
```

#### Error
```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": [
      "Additional error details if available"
    ]
  }
}
```

## Error Handling

The Security Patrol API uses standard HTTP status codes to indicate the success or failure of requests. In addition, error responses include a JSON body with details about the error.

### Error Response Format

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": [
      "Additional error details if available"
    ]
  }
}
```

### Common Error Codes

- `VALIDATION_ERROR`: Request validation failed
- `AUTHENTICATION_ERROR`: Authentication failed
- `AUTHORIZATION_ERROR`: Not authorized to perform the action
- `RESOURCE_NOT_FOUND`: Requested resource not found
- `CONFLICT`: Request conflicts with current state
- `INTERNAL_ERROR`: Server encountered an error

### HTTP Status Codes

- 200 OK: Request succeeded
- 201 Created: Resource created successfully
- 204 No Content: Request succeeded with no response body
- 400 Bad Request: Invalid request format or parameters
- 401 Unauthorized: Authentication required or failed
- 403 Forbidden: Not authorized to perform the action
- 404 Not Found: Resource not found
- 409 Conflict: Request conflicts with current state
- 429 Too Many Requests: Rate limit exceeded
- 500 Internal Server Error: Server error

## Rate Limiting

The API implements rate limiting to prevent abuse and ensure fair usage. Rate limits are applied per user and per endpoint.

### Rate Limit Headers

Rate limit information is included in the response headers:

```
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 59
X-RateLimit-Reset: 1626369600
```

When a rate limit is exceeded, the API responds with a 429 Too Many Requests status code and includes a Retry-After header indicating when the client can retry the request.

### Rate Limit Tiers

| Endpoint Category | Rate Limit |
|-------------------|------------|
| Authentication | 5 requests per 10 minutes |
| Standard Endpoints | 60 requests per minute |
| Photo Upload | 20 uploads per hour |

## Integration Guidelines

This section provides guidelines for integrating the Security Patrol API into your mobile application.

### Getting Started

To integrate with the Security Patrol API, follow these steps:

1. **Register for API Access**: Contact the API administrator to register your application and receive API credentials.

2. **Implement Authentication**: Implement the phone verification flow to authenticate users and obtain access tokens.

3. **Make API Requests**: Use the access token to make authenticated requests to the API endpoints.

4. **Handle Responses**: Process API responses and handle errors appropriately.

5. **Implement Offline Support**: Design your application to work offline and synchronize data when connectivity is restored.

### Best Practices

#### Security

- Always use HTTPS for all API communications
- Implement certificate pinning to prevent man-in-the-middle attacks
- Store authentication tokens securely using platform-specific secure storage mechanisms
- Validate tokens on the client side before making API requests
- Implement proper token refresh and expiration handling

#### Performance

- Implement efficient caching strategies for frequently accessed data
- Use batch operations where available (e.g., location batch uploads)
- Implement background synchronization to avoid blocking the UI
- Monitor and optimize network usage, especially for photo uploads
- Implement retry mechanisms with exponential backoff for failed requests

#### Offline Support

- Design your application to work offline by default
- Store data locally and synchronize when connectivity is restored
- Implement conflict resolution strategies for data modified offline
- Provide clear indicators of synchronization status to users
- Prioritize critical data for synchronization when connectivity is limited

### Mobile Client Implementation

The Security Patrol mobile application is implemented using .NET MAUI. Here are some key implementation patterns:

#### API Service

```csharp
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenManager _tokenManager;
    private readonly ILogger<ApiService> _logger;

    public ApiService(HttpClient httpClient, ITokenManager tokenManager, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _tokenManager = tokenManager;
        _logger = logger;
    }

    public async Task<T> GetAsync<T>(string endpoint, bool requiresAuth = true)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            
            if (requiresAuth)
            {
                var token = await _tokenManager.RetrieveToken();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in GET request to {endpoint}");
            throw;
        }
    }

    // Similar implementations for PostAsync, PutAsync, DeleteAsync, etc.
}
```

#### Authentication Service

```csharp
public class AuthenticationService : IAuthenticationService
{
    private readonly IApiService _apiService;
    private readonly ITokenManager _tokenManager;
    private readonly IAuthenticationStateProvider _authStateProvider;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(IApiService apiService, ITokenManager tokenManager, 
        IAuthenticationStateProvider authStateProvider, ILogger<AuthenticationService> logger)
    {
        _apiService = apiService;
        _tokenManager = tokenManager;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    public async Task<bool> RequestVerificationCodeAsync(string phoneNumber)
    {
        try
        {
            var request = new AuthenticationRequest { PhoneNumber = phoneNumber };
            var response = await _apiService.PostAsync<object>(ApiEndpoints.AuthVerify, request, false);
            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting verification code");
            return false;
        }
    }

    public async Task<bool> VerifyCodeAsync(string phoneNumber, string code)
    {
        try
        {
            var request = new VerificationRequest { PhoneNumber = phoneNumber, Code = code };
            var response = await _apiService.PostAsync<AuthenticationResponse>(ApiEndpoints.AuthValidate, request, false);
            
            if (response != null)
            {
                await _tokenManager.StoreToken(response.Token);
                var authState = new AuthState
                {
                    IsAuthenticated = true,
                    PhoneNumber = phoneNumber,
                    LastAuthenticated = DateTime.UtcNow
                };
                _authStateProvider.UpdateState(authState);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying code");
            return false;
        }
    }

    // Additional methods for token refresh, logout, etc.
}
```

#### Offline Synchronization

```csharp
public class SyncService : ISyncService
{
    private readonly IApiService _apiService;
    private readonly INetworkService _networkService;
    private readonly ISyncRepository _syncRepository;
    private readonly ILogger<SyncService> _logger;

    public SyncService(IApiService apiService, INetworkService networkService, 
        ISyncRepository syncRepository, ILogger<SyncService> logger)
    {
        _apiService = apiService;
        _networkService = networkService;
        _syncRepository = syncRepository;
        _logger = logger;
    }

    public async Task<SyncResult> SyncAllAsync()
    {
        if (!_networkService.IsConnected())
        {
            return new SyncResult { SuccessCount = 0, FailureCount = 0, PendingCount = 0 };
        }

        var pendingItems = await _syncRepository.GetPendingSyncAsync();
        var result = new SyncResult
        {
            SuccessCount = 0,
            FailureCount = 0,
            PendingCount = pendingItems.Count()
        };

        foreach (var item in pendingItems.OrderBy(i => i.Priority))
        {
            try
            {
                var success = await SyncEntityAsync(item.EntityType, item.EntityId);
                if (success)
                {
                    result.SuccessCount++;
                }
                else
                {
                    result.FailureCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error syncing {item.EntityType} with ID {item.EntityId}");
                result.FailureCount++;
            }
        }

        return result;
    }

    // Additional methods for entity-specific synchronization
}
```

### Troubleshooting

#### Common Issues

1. **Authentication Failures**
   - Ensure the phone number is in the correct format (E.164)
   - Verify that the verification code is entered correctly
   - Check that the token has not expired

2. **API Request Failures**
   - Verify that the authentication token is included in the request
   - Check for network connectivity issues
   - Ensure request parameters are formatted correctly

3. **Synchronization Issues**
   - Verify that local data is being stored correctly
   - Check for conflicts between local and server data
   - Ensure the synchronization process is handling errors properly

#### Debugging Tips

1. Enable detailed logging in your application
2. Use network inspection tools to monitor API requests and responses
3. Implement proper error handling and user feedback
4. Test with various network conditions (poor connectivity, offline, etc.)
5. Monitor application performance metrics

## API Versioning

The Security Patrol API uses URL path versioning to ensure backward compatibility as the API evolves. The current version is v1, which is included in the URL path:

```
https://api.securitypatrol.example.com/api/v1/resource
```

### Versioning Strategy

When significant changes are made to the API, a new version will be released. The versioning strategy follows these principles:

1. **Non-Breaking Changes**: Minor enhancements and bug fixes that don't break existing clients are made within the current version.

2. **Breaking Changes**: Changes that would break existing clients are released in a new API version.

3. **Deprecation Process**: When a new version is released, the previous version will be supported for a defined period (typically 6-12 months) before being deprecated.

4. **Deprecation Notices**: Clients using deprecated endpoints will receive deprecation notices in response headers.

### Version Support

| Version | Status | Support End Date |
|---------|--------|------------------|
| v1 | Current | Active |

Clients should be designed to handle API versioning gracefully, including the ability to upgrade to newer API versions when available.

## Support and Resources

### Documentation

- [API Endpoints Reference](endpoints.md)
- [Authentication Guide](authentication.md)
- [Mobile Integration Guide](../mobile/architecture.md)

### Support Channels

- **Email**: api-support@securitypatrol.example.com
- **Developer Forum**: https://developers.securitypatrol.example.com/forum
- **Status Page**: https://status.securitypatrol.example.com

### SDKs and Libraries

- **.NET MAUI Client Library**: https://github.com/securitypatrol/api-client-dotnet
- **Sample Applications**: https://github.com/securitypatrol/api-samples

### Change Log

For a history of API changes and updates, refer to the [API Change Log](changelog.md).