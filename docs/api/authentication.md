# Authentication API

## Authentication Overview

The Security Patrol application uses a secure, two-step phone number authentication system. This approach provides a balance between security and usability for mobile security personnel in the field. The authentication process involves:

1. **Phone Number Verification**: Users enter their phone number to request a verification code
2. **Code Validation**: Users enter the verification code received via SMS
3. **Token Issuance**: Upon successful validation, a JWT token is issued for subsequent API requests

This authentication system eliminates the need for passwords while ensuring that only authorized personnel with access to registered phones can use the application.

## Authentication Flow

```mermaid
sequenceDiagram
    participant User
    participant App
    participant API
    participant SMS

    User->>App: Enter Phone Number
    App->>API: POST /auth/verify (phone)
    API->>SMS: Request Verification Code
    SMS-->>User: SMS with Code
    User->>App: Enter Verification Code
    App->>API: POST /auth/validate (code)
    API-->>App: JWT Access Token
    App->>App: Store Token in Secure Storage
    App-->>User: Authentication Complete

    Note over App,API: Token Refresh Flow
    App->>App: Detect Token Expiring
    App->>API: POST /auth/refresh
    API-->>App: New Access Token
    App->>App: Update Stored Token
```

The authentication flow is designed to be secure yet straightforward:

1. The user enters their phone number in the application
2. The application sends the phone number to the API's `/auth/verify` endpoint
3. The API generates a random 6-digit verification code and sends it via SMS to the user's phone
4. The user receives the SMS and enters the verification code in the application
5. The application sends the code to the API's `/auth/validate` endpoint
6. If the code is valid, the API issues a JWT token with an 8-hour expiration
7. The application securely stores the token and uses it for all subsequent API requests
8. When the token approaches expiration, the application can request a new token using the `/auth/refresh` endpoint

## API Endpoints

The authentication API provides three main endpoints for the authentication process:

## Request Verification Code

**Endpoint:** `POST /api/v1/auth/verify`

**Description:** Initiates the first step of the authentication process by requesting a verification code for the provided phone number.

**Request Body:**
```json
{
    "phoneNumber": "+15551234567"
}
```

**Response (200 OK):**
```json
{
    "success": true,
    "data": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "message": "Verification code sent successfully"
}
```

**Response (400 Bad Request):**
```json
{
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "Bad Request",
    "status": 400,
    "detail": "Phone number format is invalid"
}
```

**Notes:**
- Phone numbers must be in E.164 format (e.g., +15551234567)
- The verification code is valid for 10 minutes
- The returned verification ID is used internally by the system and does not need to be stored by the client

## Validate Verification Code

**Endpoint:** `POST /api/v1/auth/validate`

**Description:** Completes the second step of the authentication process by validating the verification code and issuing a JWT token.

**Request Body:**
```json
{
    "phoneNumber": "+15551234567",
    "code": "123456"
}
```

**Response (200 OK):**
```json
{
    "success": true,
    "data": {
        "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        "expiresAt": "2023-07-15T14:30:00Z"
    },
    "message": "Authentication successful"
}
```

**Response (400 Bad Request):**
```json
{
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "Bad Request",
    "status": 400,
    "detail": "Verification code must be 6 digits"
}
```

**Response (401 Unauthorized):**
```json
{
    "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
    "title": "Unauthorized",
    "status": 401,
    "detail": "Invalid verification code"
}
```

**Notes:**
- The verification code must be exactly 6 digits
- The phone number must match the one used in the verification request
- The JWT token is valid for 8 hours (480 minutes) by default
- The `expiresAt` field provides the exact expiration time in ISO 8601 format

## Refresh Token

**Endpoint:** `POST /api/v1/auth/refresh`

**Description:** Refreshes an existing authentication token to extend the session without requiring re-verification.

**Request Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK):**
```json
{
    "success": true,
    "data": {
        "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        "expiresAt": "2023-07-15T22:30:00Z"
    },
    "message": "Token refreshed successfully"
}
```

**Response (401 Unauthorized):**
```json
{
    "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
    "title": "Unauthorized",
    "status": 401,
    "detail": "Invalid or expired token"
}
```

**Notes:**
- The token refresh endpoint can be used to extend a session without requiring the user to re-authenticate
- The original token must be valid (not expired) to be refreshed
- The new token will have a fresh 8-hour expiration period
- It's recommended to refresh tokens when they reach 75% of their lifetime

## Token Usage

The JWT token returned from the authentication process should be included in all subsequent API requests as an Authorization header:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Token Structure

The JWT token contains the following claims:

- `sub`: User ID
- `phone_number`: User's phone number
- `role`: User role (always "SecurityPersonnel" for mobile app users)
- `iat`: Issued at timestamp
- `exp`: Expiration timestamp
- `iss`: Token issuer (API domain)
- `aud`: Token audience (API domain)

### Token Storage

The mobile application should store the JWT token securely using platform-specific secure storage mechanisms:

- On Android, use the Android Keystore System
- Never store tokens in plain text or standard preferences
- Clear tokens when the user logs out or when the application is uninstalled

### Token Lifecycle Management

The application should implement proper token lifecycle management:

1. Store the token and its expiration time securely
2. Monitor the token's expiration time
3. Refresh the token before it expires (recommended at 75% of lifetime)
4. Handle token invalidation scenarios gracefully
5. Provide a way for users to explicitly log out (clear token)

## Error Handling

### Common Authentication Errors

| HTTP Status | Error Code | Description | Recommended Action |
|-------------|------------|-------------|--------------------|  
| 400 | INVALID_PHONE_FORMAT | Phone number format is invalid | Validate phone number format before submission |
| 400 | INVALID_CODE_FORMAT | Verification code format is invalid | Ensure code is exactly 6 digits |
| 401 | INVALID_CODE | Verification code is incorrect | Prompt user to re-enter code or request a new one |
| 401 | CODE_EXPIRED | Verification code has expired | Request a new verification code |
| 401 | INVALID_TOKEN | Authentication token is invalid | Redirect to login flow |
| 401 | EXPIRED_TOKEN | Authentication token has expired | Attempt token refresh or redirect to login |

### Error Response Format

All authentication errors follow the RFC 7807 Problem Details standard with this structure:

```json
{
    "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
    "title": "Unauthorized",
    "status": 401,
    "detail": "Specific error message"
}
```

### Client-Side Error Handling

The mobile application should implement the following error handling strategies:

1. **Validation Errors (400)**: Display specific guidance to help users correct their input
2. **Authentication Errors (401)**: Provide clear error messages and appropriate recovery options
3. **Network Errors**: Implement retry logic with exponential backoff
4. **Unexpected Errors**: Log errors for troubleshooting and display user-friendly messages

For verification code errors, consider implementing a resend option with appropriate rate limiting to prevent abuse.