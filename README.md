# Multi-Tenant Identity API - Clean Architecture

A full-stack multi-tenant identity management system built with **ASP.NET Core 8** (Clean Architecture) and **Angular 17**, featuring complete authentication, authorization, and tenant isolation.

## 🏗️ Architecture

This solution follows **Clean Architecture** principles with clear separation of concerns:

```
MultiTenantIdentityApi/
├── src/
│   ├── Domain/                     # Enterprise Business Rules
│   │   ├── Entities/              # Domain entities (ApplicationUser, ApplicationRole, AppTenantInfo)
│   │   ├── Interfaces/            # Repository and domain service interfaces
│   │   ├── Exceptions/            # Domain-specific exceptions
│   │   └── Common/                # Base entities and value objects
│   │
│   ├── Application/               # Application Business Rules
│   │   ├── Common/
│   │   │   ├── Interfaces/       # Application service interfaces
│   │   │   ├── Models/           # Result patterns and common models
│   │   │   └── Behaviours/       # MediatR pipeline behaviors
│   │   ├── DTOs/                 # Data Transfer Objects
│   │   └── Features/             # Use cases organized by feature
│   │
│   ├── Infrastructure/            # External Concerns
│   │   ├── Persistence/          # EF Core DbContexts
│   │   ├── Identity/             # ASP.NET Core Identity implementation
│   │   ├── Services/             # External service implementations
│   │   └── Configurations/       # Configuration classes
│   │
│   ├── API/                       # Presentation Layer (Web API)
│   │   ├── Controllers/          # API Controllers
│   │   ├── Middleware/           # Custom middleware
│   │   ├── Extensions/           # Service extensions
│   │   └── Program.cs            # Application entry point
│   │
│   └── Web/                       # Angular Frontend
│       ├── src/
│       │   ├── app/
│       │   │   ├── core/         # Singleton services, guards, interceptors
│       │   │   ├── shared/       # Shared components and services
│       │   │   └── features/     # Feature modules (auth, tenants, dashboard)
│       │   ├── assets/
│       │   └── environments/
│       └── package.json
│
└── MultiTenantIdentityApi.sln

```

## 🎯 Key Features

### Backend (ASP.NET Core 8)

- ✅ **Clean Architecture** with proper layer separation
- ✅ **Multi-Tenant Architecture** using Finbuckle.MultiTenant
- ✅ **Multiple Tenant Resolution Strategies**:
  - JWT Claims (`tenant_id`)
  - HTTP Headers (`X-Tenant-Id`)
  - Route parameters (`/api/{tenant}/...`)
  - Query strings (`?tenant=xxx`)
- ✅ **Full Identity Management**:
  - User registration and login
  - Password management (change, reset, forgot)
  - Email confirmation
  - Two-factor authentication (2FA)
- ✅ **JWT Authentication** with refresh tokens
- ✅ **Role-Based Authorization** with tenant isolation
- ✅ **Entity Framework Core** with SQL Server
- ✅ **Swagger/OpenAPI** documentation
- ✅ **CQRS Pattern** with MediatR (Application layer ready)
- ✅ **Repository Pattern** and Unit of Work

### Frontend (Angular 17)

- ✅ **Standalone Components** (latest Angular pattern)
- ✅ **Feature-Based Architecture**
- ✅ **Reactive Forms** with validation
- ✅ **HTTP Interceptors** for auth and tenant headers
- ✅ **Route Guards** for authentication
- ✅ **Signal-based State Management**
- ✅ **Lazy Loading** for optimal performance
- ✅ **TypeScript** with strict mode
- ✅ **SCSS** for styling

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK
- Node.js 18+ and npm
- SQL Server (or SQL Server Express/LocalDB)
- Visual Studio 2022 / VS Code / Rider (optional)
- Angular CLI (optional, for development)

### Backend Setup

1. **Update Connection String**

   Edit `src/API/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MultiTenantIdentityDb;Trusted_Connection=true;"
     }
   }
   ```

2. **Update JWT Settings**

   Edit `src/API/appsettings.json`:
   ```json
   {
     "JwtSettings": {
       "SecretKey": "Your-Secret-Key-At-Least-32-Characters-Long!!",
       "Issuer": "MultiTenantIdentityApi",
       "Audience": "MultiTenantIdentityApi"
     }
   }
   ```

3. **Install Dependencies & Run Migrations**

   ```bash
   # Navigate to Infrastructure project
   cd src/Infrastructure

   # Create migrations
   dotnet ef migrations add InitialCreate -s ../API -c TenantDbContext -o Persistence/Migrations/Tenant
   dotnet ef migrations add InitialIdentity -s ../API -c ApplicationDbContext -o Persistence/Migrations/Identity

   # Apply migrations
   dotnet ef database update -s ../API -c TenantDbContext
   dotnet ef database update -s ../API -c ApplicationDbContext
   ```

4. **Run the API**

   ```bash
   cd src/API
   dotnet run
   ```

   The API will be available at `http://localhost:5000` (or check console output)

### Frontend Setup

1. **Install Dependencies**

   ```bash
   cd src/Web
   npm install
   ```

2. **Update API URL**

   Edit `src/Web/src/environments/environment.ts`:
   ```typescript
   export const environment = {
     production: false,
     apiUrl: 'http://localhost:5000/api'  // Update to match your API URL
   };
   ```

3. **Run the Angular App**

   ```bash
   npm start
   # or
   ng serve
   ```

   The app will be available at `http://localhost:4200`

## 📚 API Documentation

Once the API is running, visit:
- **Swagger UI**: `http://localhost:5000/swagger`

### Authentication Flow

1. **Create a Tenant** (if using seeded data, skip this)
   ```http
   POST /api/tenants
   Content-Type: application/json

   {
     "identifier": "acme",
     "name": "Acme Corporation"
   }
   ```

2. **Register a User**
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

3. **Login**
   ```http
   POST /api/auth/login
   Content-Type: application/json
   X-Tenant-Id: acme

   {
     "email": "user@example.com",
     "password": "SecureP@ss123"
   }
   ```

4. **Use Protected Endpoints**
   ```http
   GET /api/auth/me
   Authorization: Bearer {your-jwt-token}
   ```

## 🔐 Multi-Tenancy

### Tenant Resolution Priority

1. **JWT Claim** (`tenant_id`) - For authenticated requests
2. **HTTP Header** (`X-Tenant-Id`) - For login/register
3. **Route Parameter** (`/api/{tenant}/...`)
4. **Query String** (`?tenant=xxx`)

### Data Isolation

- Each user belongs to a specific tenant
- Query filters automatically applied by EF Core
- Email/username uniqueness is per-tenant
- Roles are tenant-scoped

## 📁 Project Structure Details

### Domain Layer
- **No dependencies** on other layers
- Contains core business entities and logic
- Defines interfaces (contracts) for infrastructure

### Application Layer
- Depends **only** on Domain layer
- Contains business logic and use cases
- Defines DTOs and application service interfaces
- Ready for CQRS with MediatR

### Infrastructure Layer
- Implements interfaces from Domain and Application layers
- Contains EF Core, Identity, and external service implementations
- Database migrations and configurations

### API Layer
- Depends on Application and Infrastructure layers
- Contains controllers, middleware, and API configuration
- Entry point for HTTP requests

### Web Layer (Angular)
- **Core**: Singleton services, guards, interceptors
- **Shared**: Reusable components and utilities
- **Features**: Feature modules with lazy loading

## 🛠️ Technologies Used

### Backend
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- ASP.NET Core Identity
- Finbuckle.MultiTenant 7.0
- MediatR 12.2
- FluentValidation 11.9
- AutoMapper 12.0
- JWT Bearer Authentication
- Swagger/OpenAPI

### Frontend
- Angular 17 (Standalone Components)
- TypeScript 5.2
- RxJS 7.8
- SCSS
- Angular Router
- Angular Forms (Reactive)

## 🚨 Security Considerations

- [ ] Update JWT secret key (use Azure Key Vault or similar in production)
- [ ] Enable HTTPS (`RequireHttpsMetadata = true`)
- [ ] Configure proper CORS policy
- [ ] Enable email confirmation (`RequireConfirmedEmail = true`)
- [ ] Add rate limiting
- [ ] Configure proper logging (Serilog, Application Insights)
- [ ] Add authorization policies for admin endpoints
- [ ] Set up database backups
- [ ] Configure health checks with database checks

## 📄 License

MIT License

---

**Built with ❤️ using Clean Architecture principles**
