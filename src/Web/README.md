# Multi-Tenant Identity - Angular Frontend

Modern Angular 17 application with standalone components, featuring authentication, multi-tenancy, and clean architecture principles.

## 🎯 Features

- ✅ **Standalone Components** - Latest Angular architecture
- ✅ **Feature-Based Structure** - Organized by domain
- ✅ **Lazy Loading** - Optimal performance
- ✅ **Signal-Based State** - Modern reactive state management
- ✅ **HTTP Interceptors** - Automatic auth and tenant headers
- ✅ **Route Guards** - Protected routes
- ✅ **Reactive Forms** - Type-safe forms with validation
- ✅ **Path Aliases** - Clean imports with @core, @shared, @features

## 📁 Project Structure

```
src/
├── app/
│   ├── core/                      # Singleton services and core functionality
│   │   ├── guards/               # Route guards (auth.guard.ts)
│   │   ├── interceptors/         # HTTP interceptors (auth, tenant)
│   │   ├── models/               # TypeScript interfaces and types
│   │   └── services/             # Core services (auth, tenant)
│   │
│   ├── shared/                    # Reusable components and utilities
│   │   ├── components/           # Shared UI components
│   │   └── services/             # Shared services
│   │
│   ├── features/                  # Feature modules
│   │   ├── auth/                 # Authentication (login, register)
│   │   ├── dashboard/            # User dashboard
│   │   └── tenants/              # Tenant management
│   │
│   ├── app.component.ts          # Root component
│   ├── app.config.ts             # Application configuration
│   └── app.routes.ts             # Route definitions
│
├── assets/                        # Static assets
├── environments/                  # Environment configurations
├── index.html                     # Main HTML file
├── main.ts                        # Application bootstrap
└── styles.scss                    # Global styles
```

## 🚀 Getting Started

### Prerequisites

- Node.js 18+ and npm
- Angular CLI (optional): `npm install -g @angular/cli`

### Installation

```bash
# Install dependencies
npm install

# Start development server
npm start
# or
ng serve

# Navigate to http://localhost:4200
```

### Configuration

Update the API URL in `src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api'  // Your backend API URL
};
```

## 🎨 Features Breakdown

### Authentication Flow

1. **Login Component** (`features/auth/login`)
   - Tenant ID input
   - Email/password login
   - Remember me option
   - Error handling

2. **Register Component** (`features/auth/register`)
   - Tenant ID input
   - User registration with validation
   - Password confirmation
   - Automatic login after registration

### Core Services

#### AuthService (`core/services/auth.service.ts`)
```typescript
// Signal-based current user
currentUser = signal<UserDto | null>(null);

// Authentication methods
login(request: LoginRequest): Observable<AuthResponse>
register(request: RegisterRequest): Observable<AuthResponse>
logout(): void
isAuthenticated(): boolean
```

#### TenantService (`core/services/tenant.service.ts`)
```typescript
// Signal-based current tenant
currentTenant = signal<string | null>(null);

// Tenant methods
getAllTenants(): Observable<TenantDto[]>
getTenantById(id: string): Observable<TenantDto>
createTenant(request: CreateTenantRequest): Observable<TenantDto>
setCurrentTenant(tenantId: string): void
```

### HTTP Interceptors

#### Auth Interceptor
Automatically adds JWT token to requests:
```typescript
Authorization: Bearer {token}
```

#### Tenant Interceptor
Automatically adds tenant ID to requests:
```typescript
X-Tenant-Id: {tenantId}
```

### Route Guards

#### Auth Guard
Protects routes that require authentication:
```typescript
{
  path: 'dashboard',
  loadChildren: () => import('./features/dashboard/dashboard.routes'),
  canActivate: [authGuard]  // Redirects to login if not authenticated
}
```

## 🛠️ Development

### Creating a New Feature

1. **Create feature directory**
   ```bash
   mkdir -p src/app/features/my-feature
   ```

2. **Create routes file**
   ```typescript
   // src/app/features/my-feature/my-feature.routes.ts
   import { Routes } from '@angular/router';

   export const MY_FEATURE_ROUTES: Routes = [
     {
       path: '',
       loadComponent: () => import('./my-feature.component')
     }
   ];
   ```

3. **Create component**
   ```bash
   ng generate component features/my-feature --standalone
   ```

4. **Add to main routes**
   ```typescript
   // src/app/app.routes.ts
   {
     path: 'my-feature',
     loadChildren: () => import('./features/my-feature/my-feature.routes')
   }
   ```

### Path Aliases

Use clean imports with configured path aliases:

```typescript
// Instead of
import { AuthService } from '../../core/services/auth.service';

// Use
import { AuthService } from '@core/services/auth.service';
```

Available aliases:
- `@app/*` - src/app/*
- `@core/*` - src/app/core/*
- `@shared/*` - src/app/shared/*
- `@features/*` - src/app/features/*
- `@environments/*` - src/environments/*

## 📦 Building

### Development Build
```bash
npm run build
# Output: dist/multi-tenant-identity-web
```

### Production Build
```bash
ng build --configuration production
# Optimized output: dist/multi-tenant-identity-web
```

### Watch Mode
```bash
npm run watch
# Rebuilds on file changes
```

## 🧪 Testing

### Run Tests
```bash
npm test
# Runs Karma test runner
```

### Run Tests with Coverage
```bash
ng test --code-coverage
```

## 🎯 Best Practices

1. **Use Signals for State**
   ```typescript
   currentUser = signal<UserDto | null>(null);
   ```

2. **Standalone Components**
   ```typescript
   @Component({
     selector: 'app-my-component',
     standalone: true,
     imports: [CommonModule, ReactiveFormsModule],
     // ...
   })
   ```

3. **Lazy Loading**
   ```typescript
   {
     path: 'feature',
     loadChildren: () => import('./features/feature/feature.routes')
   }
   ```

4. **Reactive Forms**
   ```typescript
   form = this.fb.group({
     email: ['', [Validators.required, Validators.email]],
     password: ['', Validators.required]
   });
   ```

5. **Type Safety**
   ```typescript
   // Define interfaces in core/models
   interface UserDto {
     id: string;
     email: string;
     // ...
   }
   ```

## 🔧 Common Tasks

### Update Dependencies
```bash
npm update
```

### Lint Code
```bash
ng lint
```

### Format Code
```bash
npm run format
```

## 📚 Resources

- [Angular Documentation](https://angular.io/docs)
- [Angular Standalone Components](https://angular.io/guide/standalone-components)
- [Angular Signals](https://angular.io/guide/signals)
- [RxJS Documentation](https://rxjs.dev/)

## 🤝 Contributing

1. Create a feature branch
2. Make your changes
3. Write/update tests
4. Submit a pull request

## 📄 License

MIT License
