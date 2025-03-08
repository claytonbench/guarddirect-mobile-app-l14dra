# Security Patrol API Endpoints

## Introduction

This document provides comprehensive documentation for all API endpoints available in the Security Patrol application. These endpoints enable the mobile application to authenticate users, track time and location, manage patrols, upload photos, and submit activity reports.

## Base URL

All API endpoints are relative to the base URL:

```
https://api.securitypatrol.com/api/v1
```

## Authentication

All endpoints except authentication endpoints require a valid JWT token in the Authorization header using the Bearer scheme:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## Standard Response Format

All endpoints return responses in a standardized format with success indicator, message, and data:

```json
{
  "succeeded": true,
  "message": "Operation completed successfully",
  "data": {}
}
```

## Error Handling

Errors are returned with appropriate HTTP status codes and problem details in the response body:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "The request is invalid.",
  "errors": {
    "PhoneNumber": ["The PhoneNumber field is required."]
  }
}
```

## Authentication Endpoints

### Request Verification Code

Initiates the first step of the authentication process by requesting a verification code for the provided phone number.

**HTTP Method**: POST

**Endpoint**: `/auth/verify`

**Request Body**:
```json
{
  "phoneNumber": "+15551234567"
}
```

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Verification code sent",
  "data": {
    "verificationId": "abc123def456"
  }
}
```

**Error Responses**:
- `400 Bad Request`: Invalid phone number format
- `429 Too Many Requests`: Too many verification attempts

### Validate Verification Code

Completes the second step of the authentication process by validating the verification code and issuing a JWT token.

**HTTP Method**: POST

**Endpoint**: `/auth/validate`

**Request Body**:
```json
{
  "phoneNumber": "+15551234567",
  "code": "123456"
}
```

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Authentication successful",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2023-07-01T12:00:00Z"
  }
}
```

**Error Responses**:
- `400 Bad Request`: Invalid request format
- `401 Unauthorized`: Invalid verification code

### Refresh Token

Refreshes an existing authentication token to extend the session without requiring re-verification.

**HTTP Method**: POST

**Endpoint**: `/auth/refresh`

**Authorization**: Bearer token required

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Token refreshed",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2023-07-01T12:00:00Z"
  }
}
```

**Error Responses**:
- `401 Unauthorized`: Invalid or expired token

## Time Tracking Endpoints

### Clock In/Out

Records a clock in or clock out event with timestamp and location.

**HTTP Method**: POST

**Endpoint**: `/time/clock`

**Authorization**: Bearer token required

**Request Body**:
```json
{
  "type": "IN",
  "timestamp": "2023-07-01T09:00:00Z",
  "location": {
    "latitude": 37.7749,
    "longitude": -122.4194
  }
}
```

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Clock event recorded",
  "data": {
    "id": "123",
    "status": "Success"
  }
}
```

**Error Responses**:
- `400 Bad Request`: Invalid request format or business rule violation
- `401 Unauthorized`: Unauthorized

### Get Time Record History

Retrieves time record history with pagination.

**HTTP Method**: GET

**Endpoint**: `/time/history`

**Authorization**: Bearer token required

**Query Parameters**:
- `pageNumber` (optional): Page number for pagination (default: 1, minimum: 1)
- `pageSize` (optional): Number of records per page (default: 10, minimum: 1, maximum: 100)

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Time records retrieved",
  "data": {
    "items": [
      {
        "id": 123,
        "type": "IN",
        "timestamp": "2023-07-01T09:00:00Z",
        "latitude": 37.7749,
        "longitude": -122.4194
      },
      {
        "id": 124,
        "type": "OUT",
        "timestamp": "2023-07-01T17:00:00Z",
        "latitude": 37.7749,
        "longitude": -122.4194
      }
    ],
    "pageNumber": 1,
    "totalPages": 5,
    "totalCount": 42
  }
}
```

**Error Responses**:
- `401 Unauthorized`: Unauthorized

### Get Current Clock Status

Retrieves the current clock status (in/out) of the user.

**HTTP Method**: GET

**Endpoint**: `/time/status`

**Authorization**: Bearer token required

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Status retrieved",
  "data": {
    "isClocked": true,
    "lastClockInTime": "2023-07-01T09:00:00Z",
    "lastClockOutTime": "2023-06-30T17:00:00Z"
  }
}
```

**Error Responses**:
- `401 Unauthorized`: Unauthorized

### Get Time Records by Date Range

Retrieves time records within a specified date range.

**HTTP Method**: GET

**Endpoint**: `/time/range`

**Authorization**: Bearer token required

**Query Parameters**:
- `startDate` (required): Start date for the range (inclusive)
- `endDate` (required): End date for the range (inclusive)

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Time records retrieved",
  "data": [
    {
      "id": 123,
      "type": "IN",
      "timestamp": "2023-07-01T09:00:00Z",
      "latitude": 37.7749,
      "longitude": -122.4194
    },
    {
      "id": 124,
      "type": "OUT",
      "timestamp": "2023-07-01T17:00:00Z",
      "latitude": 37.7749,
      "longitude": -122.4194
    }
  ]
}
```

**Error Responses**:
- `400 Bad Request`: Invalid date range
- `401 Unauthorized`: Unauthorized

## Location Tracking Endpoints

### Submit Location Batch

Submits a batch of location data points from mobile clients.

**HTTP Method**: POST

**Endpoint**: `/location/batch`

**Authorization**: Bearer token required

**Request Body**:
```json
{
  "userId": "user123",
  "locations": [
    {
      "latitude": 37.7749,
      "longitude": -122.4194,
      "accuracy": 10.5,
      "timestamp": "2023-07-01T09:15:00Z"
    },
    {
      "latitude": 37.775,
      "longitude": -122.4195,
      "accuracy": 8.2,
      "timestamp": "2023-07-01T09:20:00Z"
    }
  ]
}
```

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Location batch processed",
  "data": {
    "syncedIds": [1, 2, 3, 4, 5],
    "failedIds": []
  }
}
```

**Error Responses**:
- `400 Bad Request`: Invalid request format
- `401 Unauthorized`: Unauthorized

### Get Location History

Retrieves location history for a specific time range.

**HTTP Method**: GET

**Endpoint**: `/location/history`

**Authorization**: Bearer token required

**Query Parameters**:
- `startTime` (required): Start time for the range (inclusive)
- `endTime` (required): End time for the range (inclusive)

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Location history retrieved",
  "data": [
    {
      "latitude": 37.7749,
      "longitude": -122.4194,
      "accuracy": 10.5,
      "timestamp": "2023-07-01T09:15:00Z"
    },
    {
      "latitude": 37.775,
      "longitude": -122.4195,
      "accuracy": 8.2,
      "timestamp": "2023-07-01T09:20:00Z"
    }
  ]
}
```

**Error Responses**:
- `400 Bad Request`: Invalid time range
- `401 Unauthorized`: Unauthorized

### Get Current Location

Retrieves the latest location for the current user.

**HTTP Method**: GET

**Endpoint**: `/location/current`

**Authorization**: Bearer token required

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Current location retrieved",
  "data": {
    "latitude": 37.7749,
    "longitude": -122.4194,
    "accuracy": 10.5,
    "timestamp": "2023-07-01T09:15:00Z"
  }
}
```

**Response (204 No Content)**: No location data available

**Error Responses**:
- `401 Unauthorized`: Unauthorized

## Patrol Management Endpoints

### Get Patrol Locations

Retrieves all patrol locations available in the system.

**HTTP Method**: GET

**Endpoint**: `/patrol/locations`

**Authorization**: Bearer token required

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Locations retrieved",
  "data": [
    {
      "id": 1,
      "name": "Main Building",
      "latitude": 37.7749,
      "longitude": -122.4194
    },
    {
      "id": 2,
      "name": "Warehouse",
      "latitude": 37.775,
      "longitude": -122.4195
    }
  ]
}
```

**Error Responses**:
- `401 Unauthorized`: Unauthorized

### Get Checkpoints by Location

Retrieves all checkpoints for a specific patrol location.

**HTTP Method**: GET

**Endpoint**: `/patrol/locations/{locationId}/checkpoints`

**Authorization**: Bearer token required

**Path Parameters**:
- `locationId` (required): ID of the patrol location

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Checkpoints retrieved",
  "data": [
    {
      "id": 1,
      "locationId": 1,
      "name": "Front Entrance",
      "latitude": 37.7749,
      "longitude": -122.4194,
      "isVerified": false
    },
    {
      "id": 2,
      "locationId": 1,
      "name": "Back Entrance",
      "latitude": 37.775,
      "longitude": -122.4195,
      "isVerified": true
    }
  ]
}
```

**Error Responses**:
- `401 Unauthorized`: Unauthorized
- `404 Not Found`: Location not found

### Verify Checkpoint

Processes a checkpoint verification request from a security officer.

**HTTP Method**: POST

**Endpoint**: `/patrol/verify`

**Authorization**: Bearer token required

**Request Body**:
```json
{
  "checkpointId": 1,
  "timestamp": "2023-07-01T09:15:00Z",
  "location": {
    "latitude": 37.7749,
    "longitude": -122.4194
  }
}
```

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Checkpoint verified",
  "data": {
    "status": "Verified"
  }
}
```

**Error Responses**:
- `400 Bad Request`: Invalid request format
- `401 Unauthorized`: Unauthorized
- `404 Not Found`: Checkpoint not found

### Get Patrol Status

Retrieves the current status of a patrol for a specific location and the current user.

**HTTP Method**: GET

**Endpoint**: `/patrol/locations/{locationId}/status`

**Authorization**: Bearer token required

**Path Parameters**:
- `locationId` (required): ID of the patrol location

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Patrol status retrieved",
  "data": {
    "locationId": 1,
    "totalCheckpoints": 10,
    "verifiedCheckpoints": 4
  }
}
```

**Error Responses**:
- `401 Unauthorized`: Unauthorized
- `404 Not Found`: Location not found

### Get Nearby Checkpoints

Retrieves checkpoints that are within a specified distance of the user's current location.

**HTTP Method**: GET

**Endpoint**: `/patrol/checkpoints/nearby`

**Authorization**: Bearer token required

**Query Parameters**:
- `latitude` (required): Latitude coordinate
- `longitude` (required): Longitude coordinate
- `radiusInMeters` (required): Search radius in meters

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Nearby checkpoints retrieved",
  "data": [
    {
      "id": 1,
      "locationId": 1,
      "name": "Front Entrance",
      "latitude": 37.7749,
      "longitude": -122.4194,
      "isVerified": false,
      "distance": 15.2
    },
    {
      "id": 2,
      "locationId": 1,
      "name": "Back Entrance",
      "latitude": 37.775,
      "longitude": -122.4195,
      "isVerified": true,
      "distance": 42.8
    }
  ]
}
```

**Error Responses**:
- `400 Bad Request`: Invalid parameters
- `401 Unauthorized`: Unauthorized

## Photo Management Endpoints

### Upload Photo

Uploads a photo with metadata to the system.

**HTTP Method**: POST

**Endpoint**: `/photos/upload`

**Authorization**: Bearer token required

**Request Body**: `multipart/form-data`

| Field | Type | Description | Required |
|-------|------|-------------|----------|
| file | File | The photo file to upload | Yes |
| timestamp | String | The timestamp when the photo was taken (ISO 8601) | Yes |
| latitude | Number | The latitude coordinate where the photo was taken | Yes |
| longitude | Number | The longitude coordinate where the photo was taken | Yes |

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Photo uploaded",
  "data": {
    "id": "photo123",
    "status": "Success"
  }
}
```

**Error Responses**:
- `400 Bad Request`: Invalid request format or missing file
- `401 Unauthorized`: Unauthorized

### Get Photo by ID

Retrieves a photo by its unique identifier.

**HTTP Method**: GET

**Endpoint**: `/photos/{id}`

**Authorization**: Bearer token required

**Path Parameters**:
- `id` (required): ID of the photo

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Photo retrieved",
  "data": {
    "id": 123,
    "userId": "user123",
    "timestamp": "2023-07-01T09:15:00Z",
    "latitude": 37.7749,
    "longitude": -122.4194,
    "filePath": "/photos/123.jpg"
  }
}
```

**Error Responses**:
- `401 Unauthorized`: Unauthorized
- `404 Not Found`: Photo not found

### Get Photo File

Retrieves the binary data of a photo by its unique identifier.

**HTTP Method**: GET

**Endpoint**: `/photos/{id}/file`

**Authorization**: Bearer token required

**Path Parameters**:
- `id` (required): ID of the photo

**Response (200 OK)**:
Binary photo data with appropriate content type (typically image/jpeg)

**Error Responses**:
- `401 Unauthorized`: Unauthorized
- `404 Not Found`: Photo not found

### Get My Photos

Retrieves all photos for the current authenticated user.

**HTTP Method**: GET

**Endpoint**: `/photos/my`

**Authorization**: Bearer token required

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Photos retrieved",
  "data": [
    {
      "id": 123,
      "userId": "user123",
      "timestamp": "2023-07-01T09:15:00Z",
      "latitude": 37.7749,
      "longitude": -122.4194,
      "filePath": "/photos/123.jpg"
    },
    {
      "id": 124,
      "userId": "user123",
      "timestamp": "2023-07-01T10:30:00Z",
      "latitude": 37.775,
      "longitude": -122.4195,
      "filePath": "/photos/124.jpg"
    }
  ]
}
```

**Error Responses**:
- `401 Unauthorized`: Unauthorized

### Get Photos by Location

Retrieves photos within a specified radius of a geographic location.

**HTTP Method**: GET

**Endpoint**: `/photos/location`

**Authorization**: Bearer token required

**Query Parameters**:
- `latitude` (required): Latitude coordinate
- `longitude` (required): Longitude coordinate
- `radiusInMeters` (required): Search radius in meters

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Photos retrieved",
  "data": [
    {
      "id": 123,
      "userId": "user123",
      "timestamp": "2023-07-01T09:15:00Z",
      "latitude": 37.7749,
      "longitude": -122.4194,
      "filePath": "/photos/123.jpg",
      "distance": 15.2
    },
    {
      "id": 124,
      "userId": "user123",
      "timestamp": "2023-07-01T10:30:00Z",
      "latitude": 37.775,
      "longitude": -122.4195,
      "filePath": "/photos/124.jpg",
      "distance": 42.8
    }
  ]
}
```

**Error Responses**:
- `400 Bad Request`: Invalid parameters
- `401 Unauthorized`: Unauthorized

### Delete Photo

Deletes a photo by its unique identifier.

**HTTP Method**: DELETE

**Endpoint**: `/photos/{id}`

**Authorization**: Bearer token required

**Path Parameters**:
- `id` (required): ID of the photo

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Photo deleted"
}
```

**Error Responses**:
- `401 Unauthorized`: Unauthorized
- `404 Not Found`: Photo not found

## Activity Reporting Endpoints

### Create Report

Creates a new activity report.

**HTTP Method**: POST

**Endpoint**: `/reports`

**Authorization**: Bearer token required

**Request Body**:
```json
{
  "text": "Suspicious activity observed near the loading dock.",
  "timestamp": "2023-07-01T09:15:00Z",
  "location": {
    "latitude": 37.7749,
    "longitude": -122.4194
  }
}
```

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Report created",
  "data": {
    "id": "report123",
    "status": "Success"
  }
}
```

**Error Responses**:
- `400 Bad Request`: Invalid request format or validation error
- `401 Unauthorized`: Unauthorized

### Get Reports

Retrieves all reports for the current user with pagination.

**HTTP Method**: GET

**Endpoint**: `/reports`

**Authorization**: Bearer token required

**Query Parameters**:
- `pageNumber` (optional): Page number for pagination (default: 1, minimum: 1)
- `pageSize` (optional): Number of reports per page (default: 10, minimum: 1, maximum: 100)

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Reports retrieved",
  "data": {
    "items": [
      {
        "id": 123,
        "text": "Suspicious activity observed near the loading dock.",
        "timestamp": "2023-07-01T09:15:00Z",
        "latitude": 37.7749,
        "longitude": -122.4194,
        "userId": "user123"
      },
      {
        "id": 124,
        "text": "All clear at the main entrance.",
        "timestamp": "2023-07-01T10:30:00Z",
        "latitude": 37.775,
        "longitude": -122.4195,
        "userId": "user123"
      }
    ],
    "pageNumber": 1,
    "totalPages": 3,
    "totalCount": 25
  }
}
```

**Error Responses**:
- `401 Unauthorized`: Unauthorized

### Get Report by ID

Retrieves a specific report by ID.

**HTTP Method**: GET

**Endpoint**: `/reports/{id}`

**Authorization**: Bearer token required

**Path Parameters**:
- `id` (required): ID of the report

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Report retrieved",
  "data": {
    "id": 123,
    "text": "Suspicious activity observed near the loading dock.",
    "timestamp": "2023-07-01T09:15:00Z",
    "latitude": 37.7749,
    "longitude": -122.4194,
    "userId": "user123"
  }
}
```

**Error Responses**:
- `401 Unauthorized`: Unauthorized
- `404 Not Found`: Report not found

### Get Reports by Date Range

Retrieves reports within a specific date range.

**HTTP Method**: GET

**Endpoint**: `/reports/range`

**Authorization**: Bearer token required

**Query Parameters**:
- `startDate` (required): Start date for the range (inclusive)
- `endDate` (required): End date for the range (inclusive)

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Reports retrieved",
  "data": [
    {
      "id": 123,
      "text": "Suspicious activity observed near the loading dock.",
      "timestamp": "2023-07-01T09:15:00Z",
      "latitude": 37.7749,
      "longitude": -122.4194,
      "userId": "user123"
    },
    {
      "id": 124,
      "text": "All clear at the main entrance.",
      "timestamp": "2023-07-01T10:30:00Z",
      "latitude": 37.775,
      "longitude": -122.4195,
      "userId": "user123"
    }
  ]
}
```

**Error Responses**:
- `400 Bad Request`: Invalid date range
- `401 Unauthorized`: Unauthorized

### Update Report

Updates an existing report.

**HTTP Method**: PUT

**Endpoint**: `/reports/{id}`

**Authorization**: Bearer token required

**Path Parameters**:
- `id` (required): ID of the report to update

**Request Body**:
```json
{
  "text": "Suspicious activity observed near the loading dock. Security responded.",
  "timestamp": "2023-07-01T09:30:00Z",
  "location": {
    "latitude": 37.7749,
    "longitude": -122.4194
  }
}
```

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Report updated"
}
```

**Error Responses**:
- `400 Bad Request`: Invalid request format or validation error
- `401 Unauthorized`: Unauthorized
- `404 Not Found`: Report not found

### Delete Report

Deletes a specific report.

**HTTP Method**: DELETE

**Endpoint**: `/reports/{id}`

**Authorization**: Bearer token required

**Path Parameters**:
- `id` (required): ID of the report to delete

**Response (200 OK)**:
```json
{
  "succeeded": true,
  "message": "Report deleted"
}
```

**Error Responses**:
- `401 Unauthorized`: Unauthorized
- `404 Not Found`: Report not found