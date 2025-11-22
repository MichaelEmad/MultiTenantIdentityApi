# Multi-Tenant Identity API

A full-featured ASP.NET Core 8 API with multi-tenant support using Finbuckle.MultiTenant, complete Identity endpoints, JWT authentication, and Entity Framework Core with SQL Server.

## Features

- **Multi-Tenant Architecture**: Using Finbuckle.MultiTenant with EF Core Store
- **Flexible Tenant Resolution**: Combined strategy supporting Claims, Headers, Route, and Query parameters
- **Full Identity Endpoints**: Registration, Login, Password Management, Email Confirmation, 2FA
- **JWT Authentication**: Secure token-based authentication with refresh tokens
- **Role-Based Authorization**: Tenant-scoped role management
- **User Management**: Full CRUD operations with tenant isolation
- **Swagger/OpenAPI**: Complete API documentation with JWT support

## Project Structure

```
MultiTenantIdentityApi/
├── Configurations/
│   ├── ClaimStrategy.cs           # Custom claim-based tenant resolution
│   ├── CombinedTenantStrategy.cs  # Combined resolution strategy
│   └── JwtSettings.cs             # JWT configuration model
├── Controllers/
│   ├── AuthController.cs          # Authentication endpoints
│   ├── RolesController.cs         # Role management endpoints
│   ├── TenantsController.cs       # Tenant management endpoints
│   └── UsersController.cs         # User management endpoints
├── Data/
│   ├── ApplicationDbContext.cs    # Multi-tenant Identity DbContext
│   └── TenantDbContext.cs         # Tenant store DbContext
├── Extensions/
│   └── ServiceCollectionExtensions.cs  # DI configuration helpers
├── Middleware/
│   └── TenantValidationMiddleware.cs   # Tenant validation
├── Models/
│   ├── ApplicationUser.cs         # Custom Identity user with multi-tenant support
│   ├── AppTenantInfo.cs           # Tenant information model
│   └── DTOs/
│       ├── AuthDtos.cs            # Authentication DTOs
│       └── TenantDtos.cs          # Tenant DTOs
├── Services/
│   ├── AuthService.cs             # Authentication business logic
│   ├── RoleService.cs             # Role management logic
│   ├── TenantService.cs           # Tenant management logic
│   └── TokenService.cs            # JWT token generation
├── appsettings.json               # Application configuration
├── appsettings.Development.json   # Development configuration
├── Program.cs                     # Application entry point
└── MultiTenantIdentityApi.csproj  # Project file
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- SQL Server (or SQL Server Express/LocalDB)
- Visual Studio 2022 / VS Code / Rider

### Installation

1. Clone or copy the project files
2. Update the connection string in `appsettings.json`
3. Run the following commands:

```bash
# Restore packages
dotnet restore

# Create initial migration for TenantDbContext
dotnet ef migrations add InitialTenants -c TenantDbContext -o Migrations/Tenants

# Create initial migration for ApplicationDbContext
dotnet ef migrations add InitialIdentity -c ApplicationDbContext -o Migrations/Identity

# Apply migrations
dotnet ef database update -c TenantDbContext
dotnet ef database update -c ApplicationDbContext

# Run the application
dotnet run
```

### Configuration

Update `appsettings.json` with your settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your-Connection-String"
  },
  "JwtSettings": {
    "SecretKey": "Your-32-Character-Or-Longer-Secret-Key",
    "Issuer": "YourApp",
    "Audience": "YourApp",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

## API Usage

### Tenant Resolution

The API supports multiple tenant resolution strategies (in priority order):

1. **JWT Claim** (`tenant_id`): For authenticated requests
2. **Header** (`X-Tenant-Id`): For login/register and external calls
3. **Route** (`/api/{tenant}/...`): For tenant-specific routes
4. **Query** (`?tenant=xxx`): For query-based resolution

### Authentication Flow

#### 1. Create a Tenant (Admin)

```http
POST /api/tenants
Content-Type: application/json

{
  "identifier": "acme",
  "name": "Acme Corporation"
}
```

#### 2. Register a User

```http
POST /api/auth/register
Content-Type: application/json
X-Tenant-Id: acme

{
  "email": "user@example.com",
  "password": "SecureP@ss123",
  "confirmPassword": "SecureP@ss123",
  "firstName": "John",
  "lastName": "Doe"
}
```

#### 3. Login

```http
POST /api/auth/login
Content-Type: application/json
X-Tenant-Id: acme

{
  "email": "user@example.com",
  "password": "SecureP@ss123"
}
```

Response:
```json
{
  "succeeded": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "abc123...",
  "accessTokenExpiration": "2024-01-15T12:00:00Z",
  "user": {
    "id": "user-id",
    "email": "user@example.com",
    "tenantId": "tenant-1"
  }
}
```

#### 4. Use Protected Endpoints

```http
GET /api/auth/me
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### API Endpoints

#### Authentication (`/api/auth`)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/register` | Register new user | No (Header required) |
| POST | `/login` | Login with credentials | No (Header required) |
| POST | `/refresh-token` | Refresh access token | No |
| POST | `/logout` | Logout user | Yes |
| POST | `/change-password` | Change password | Yes |
| POST | `/forgot-password` | Request password reset | No |
| POST | `/reset-password` | Reset password with token | No |
| POST | `/confirm-email` | Confirm email | No |
| POST | `/resend-confirmation` | Resend confirmation email | No |
| GET | `/me` | Get current user | Yes |
| PUT | `/me` | Update profile | Yes |
| DELETE | `/me` | Delete account | Yes |

#### Two-Factor Authentication (`/api/auth/2fa`)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/enable` | Enable 2FA | Yes |
| POST | `/verify` | Verify 2FA setup | Yes |
| POST | `/disable` | Disable 2FA | Yes |
| POST | `/login` | Login with 2FA code | No |
| POST | `/recovery-codes` | Generate recovery codes | Yes |
| POST | `/recovery-login` | Login with recovery code | No |

#### Tenants (`/api/tenants`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | Get all tenants |
| GET | `/{id}` | Get tenant by ID |
| GET | `/by-identifier/{identifier}` | Get tenant by identifier |
| POST | `/` | Create new tenant |
| PUT | `/{id}` | Update tenant |
| DELETE | `/{id}` | Delete tenant |
| POST | `/{id}/activate` | Activate tenant |
| POST | `/{id}/deactivate` | Deactivate tenant |

#### Users (`/api/users`)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/` | Get all users (paginated) | Yes |
| GET | `/{id}` | Get user by ID | Yes |
| POST | `/{id}/activate` | Activate user | Yes |
| POST | `/{id}/deactivate` | Deactivate user | Yes |
| POST | `/{id}/lockout` | Lock out user | Yes |
| POST | `/{id}/unlock` | Unlock user | Yes |
| DELETE | `/{id}` | Delete user | Yes |

#### Roles (`/api/roles`)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/` | Get all roles | Yes |
| GET | `/{id}` | Get role by ID | Yes |
| POST | `/` | Create new role | Yes |
| DELETE | `/{id}` | Delete role | Yes |
| POST | `/assign` | Assign role to user | Yes |
| POST | `/remove` | Remove role from user | Yes |
| GET | `/{roleName}/users` | Get users in role | Yes |

## Multi-Tenant Data Isolation

The API ensures complete data isolation between tenants:

1. **Users**: Each user belongs to a specific tenant
2. **Roles**: Roles are tenant-scoped
3. **Query Filters**: EF Core automatically filters data by tenant
4. **Unique Constraints**: Email/Username uniqueness is per-tenant

## Security Considerations

- JWT tokens contain the `tenant_id` claim for tenant resolution
- Tenant validation middleware prevents cross-tenant access
- Password hashing uses ASP.NET Core Identity defaults (PBKDF2)
- Lockout protection against brute-force attacks
- Refresh tokens stored securely with expiration

## Production Checklist

- [ ] Update JWT secret key (use secrets manager)
- [ ] Enable HTTPS (`RequireHttpsMetadata = true`)
- [ ] Configure proper CORS policy
- [ ] Enable email confirmation (`RequireConfirmedEmail = true`)
- [ ] Add rate limiting
- [ ] Configure proper logging (Serilog, Application Insights)
- [ ] Add authorization policies for admin endpoints
- [ ] Set up database backups
- [ ] Configure health checks with database checks

## License

MIT License
