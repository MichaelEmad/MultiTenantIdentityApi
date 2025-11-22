# Features Documentation

This document provides detailed information about the key features implemented in the Multi-Tenant Identity API with Angular frontend.

## Table of Contents

1. [File Upload and Management](#file-upload-and-management)
2. [Excel Export Service](#excel-export-service)
3. [Internationalization (i18n)](#internationalization-i18n)
4. [Multi-Tenancy](#multi-tenancy)
5. [JWT Authentication](#jwt-authentication)

---

## File Upload and Management

The application provides a comprehensive file storage system with support for uploading, downloading, and managing files across the entire system.

### Backend Implementation

#### IFileStorageService Interface

Located at: `src/Application/Common/Interfaces/IFileStorageService.cs`

Provides the following operations:

- **UploadFileAsync**: Upload a single file with optional folder organization
- **UploadFileWithMetadataAsync**: Upload with custom metadata
- **DownloadFileAsync**: Download files by path
- **DeleteFileAsync**: Delete files
- **FileExistsAsync**: Check file existence
- **GetFileUrlAsync**: Generate public URLs for files
- **GetFilesInFolderAsync**: List files in a specific folder

#### LocalFileStorageService

Located at: `src/Infrastructure/Services/LocalFileStorageService.cs`

Implementation details:
- Stores files in a configurable local directory
- Generates unique filenames using GUIDs to prevent conflicts
- Validates file sizes and types
- Provides streaming support for efficient file handling
- Configurable via `appsettings.json`:

```json
{
  "FileStorage": {
    "LocalPath": "./uploads",
    "BaseUrl": "/files"
  }
}
```

#### Files API Controller

Located at: `src/API/Controllers/FilesController.cs`

Endpoints:

- **POST /api/files/upload**: Upload a single file
  - Query params: `folder` (optional)
  - Max file size: 10MB
  - Allowed extensions: `.jpg`, `.jpeg`, `.png`, `.gif`, `.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.txt`, `.zip`

- **POST /api/files/upload-multiple**: Upload multiple files
  - Returns status for each file upload

- **GET /api/files/download/{*filePath}**: Download a file
  - Returns file stream with proper content type

- **DELETE /api/files/{*filePath}**: Delete a file

- **HEAD /api/files/{*filePath}**: Check if file exists

- **GET /api/files/folder/{*folder}**: List files in a folder

### Frontend Implementation

#### FileUploadComponent

Located at: `src/Web/src/app/shared/components/file-upload/file-upload.component.ts`

Features:
- **Drag and Drop**: Intuitive file selection
- **Multiple File Support**: Configurable via `[multiple]` input
- **File Validation**:
  - File size validation (configurable max size)
  - File type validation (configurable allowed types)
- **Upload Progress**: Real-time progress tracking
- **Internationalized**: Supports English and Arabic

Usage Example:

```typescript
<app-file-upload
  [multiple]="true"
  [maxSize]="10"
  [allowedTypes]="['.pdf', '.jpg', '.png']"
  [folder]="'documents'"
  (filesUploaded)="onFilesUploaded($event)"
  (uploadError)="onUploadError($event)">
</app-file-upload>
```

Component Inputs:
- `multiple`: boolean (default: false) - Allow multiple file selection
- `maxSize`: number (default: 10) - Maximum file size in MB
- `allowedTypes`: string[] (default: []) - Allowed file extensions
- `folder`: string | null (default: null) - Target folder for uploads

Component Outputs:
- `filesUploaded`: Emits array of uploaded file information
- `uploadError`: Emits error messages

---

## Excel Export Service

A generic service for exporting data to Excel files with support for custom columns and multiple sheets.

### Backend Implementation

#### IExcelExportService Interface

Located at: `src/Application/Common/Interfaces/IExcelExportService.cs`

Methods:

- **ExportToExcelAsync&lt;T&gt;**: Export data with auto-detected columns
- **ExportToExcelAsync&lt;T&gt; (with column mappings)**: Export with custom column definitions
- **ExportMultipleSheetsAsync**: Export multiple sheets to a single workbook

#### ExcelExportService

Located at: `src/Infrastructure/Services/ExcelExportService.cs`

Built using **ClosedXML** library.

Features:
- **Reflection-based column detection**: Automatically detects properties
- **Custom column mappings**: Define custom columns with lambda expressions
- **Auto-formatting**: Headers with bold text and auto-fitted columns
- **Multi-sheet support**: Export multiple data sets to different sheets
- **Type-safe**: Generic implementation with strong typing

Usage Example:

```csharp
// Simple export
var data = await _tenantService.GetAllTenantsAsync();
var excelData = await _excelExportService.ExportToExcelAsync(
    data.Data,
    sheetName: "Tenants");

// Custom columns
var columnMappings = new Dictionary<string, Func<TenantDto, object>>
{
    ["Tenant Name"] = t => t.Name,
    ["Identifier"] = t => t.Identifier,
    ["Active"] = t => t.IsActive ? "Yes" : "No"
};

var excelData = await _excelExportService.ExportToExcelAsync(
    data.Data,
    columnMappings,
    sheetName: "Tenants");
```

#### Export API Controller

Located at: `src/API/Controllers/ExportController.cs`

Endpoints:

- **GET /api/export/tenants**: Export all tenants to Excel
  - Returns: Excel file with `.xlsx` extension
  - Filename format: `Tenants_yyyyMMddHHmmss.xlsx`

- **POST /api/export/generic**: Generic export endpoint
  - Request body: JSON array of objects
  - Query params: `sheetName` (optional)

---

## Internationalization (i18n)

Full support for English and Arabic languages with RTL (Right-to-Left) layout for Arabic.

### Implementation

#### Translation Service

Located at: `src/Web/src/app/core/services/translation.service.ts`

Features:
- Signal-based reactive language state
- JSON-based translation files
- Parameter interpolation support
- Automatic RTL/LTR switching
- LocalStorage persistence of language preference
- Dynamic loading of translation files

Supported Languages:
- English (en)
- Arabic (ar)

#### Translation Files

Located at: `src/Web/src/assets/i18n/`

- `en.json`: English translations
- `ar.json`: Arabic translations

Translation structure:
```json
{
  "common": {
    "welcome": "Welcome",
    "submit": "Submit",
    ...
  },
  "auth": {
    "login": "Login",
    "email": "Email",
    ...
  },
  ...
}
```

#### Translation Pipe

Located at: `src/Web/src/app/shared/pipes/translate.pipe.ts`

Usage in templates:

```html
<!-- Simple translation -->
<h2>{{ 'auth.login-title' | translate }}</h2>

<!-- Translation with parameters -->
<p>{{ 'validation.min-length' | translate: {length: 6} }}</p>
```

#### Language Switcher Component

Located at: `src/Web/src/app/shared/components/language-switcher/language-switcher.component.ts`

Features:
- Toggle between English and Arabic
- Visual indicator for active language
- Triggers automatic RTL/LTR switching
- Persists selection in LocalStorage

Usage:
```html
<app-language-switcher></app-language-switcher>
```

### RTL Support

RTL layout is automatically applied when Arabic is selected.

Global RTL styles in `src/Web/src/styles.scss`:
- Direction switching (rtl/ltr)
- Text alignment
- Font family optimization for Arabic
- Automatic margin/padding adjustments

---

## Multi-Tenancy

Implemented using **Finbuckle.MultiTenant** library.

### Features

- **Multiple Resolution Strategies**:
  - JWT Claims (tenant_id claim)
  - HTTP Headers (X-Tenant-Id)
  - Route values
  - Query strings

- **Tenant Isolation**:
  - Per-tenant database connections
  - Per-tenant user isolation
  - Automatic tenant filtering

### Frontend Integration

#### TenantInterceptor

Located at: `src/Web/src/app/core/interceptors/tenant.interceptor.ts`

Automatically adds the `X-Tenant-Id` header to all HTTP requests based on the currently selected tenant.

#### TenantService

Located at: `src/Web/src/app/core/services/tenant.service.ts`

Manages tenant context in the frontend with signal-based state management.

---

## JWT Authentication

Supports both symmetric (HMAC-SHA256) and asymmetric (RSA-SHA256) signing.

### Features

- **Dual-mode JWT signing**:
  - Development: Symmetric key (HMAC-SHA256)
  - Production: RSA certificate (RSA-SHA256)

- **Token Contents**:
  - User ID
  - Email
  - Tenant ID
  - Roles
  - Custom claims

### Configuration

See `docs/RSA_CERTIFICATE_SETUP.md` for RSA certificate configuration.

### Frontend Integration

#### AuthInterceptor

Located at: `src/Web/src/app/core/interceptors/auth.interceptor.ts`

Automatically adds the JWT token to all HTTP requests via the `Authorization` header.

#### AuthService

Located at: `src/Web/src/app/core/services/auth.service.ts`

Manages authentication state with signal-based reactive state management.

Features:
- Login/Register/Logout
- Token storage and retrieval
- Current user state
- Automatic token refresh

---

## Usage Examples

### Complete File Upload Flow

```typescript
// Component
export class DocumentsComponent {
  onFilesUploaded(results: any[]): void {
    console.log('Uploaded files:', results);
    results.forEach(file => {
      console.log(`File URL: ${file.fileUrl}`);
      console.log(`File size: ${file.fileSize} bytes`);
    });
  }

  onUploadError(error: string): void {
    console.error('Upload failed:', error);
  }
}
```

### Complete Export Flow

```typescript
// Component
export class TenantsListComponent {
  async exportToExcel(): Promise<void> {
    const tenants = await this.tenantService.getAll();
    const blob = await this.exportService.exportTenants();
    this.downloadFile(blob, `Tenants_${new Date().toISOString()}.xlsx`);
  }

  private downloadFile(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    window.URL.revokeObjectURL(url);
  }
}
```

### Language Switching

```typescript
// Using TranslationService directly
export class HeaderComponent {
  constructor(private translationService: TranslationService) {}

  switchToArabic(): void {
    this.translationService.setLanguage('ar');
  }

  switchToEnglish(): void {
    this.translationService.setLanguage('en');
  }

  get currentLanguage(): string {
    return this.translationService.getLanguage();
  }

  get isRTL(): boolean {
    return this.translationService.isRTL();
  }
}
```

---

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MultiTenantIdentityDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-here-min-32-chars",
    "Issuer": "MultiTenantIdentityApi",
    "Audience": "MultiTenantIdentityClient",
    "ExpirationMinutes": 60,
    "UseRsaCertificate": false
  },
  "FileStorage": {
    "LocalPath": "./uploads",
    "BaseUrl": "/files"
  }
}
```

### environment.ts (Angular)

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7001/api'
};
```

---

## Best Practices

### File Upload

1. Always validate file types and sizes on both frontend and backend
2. Use unique filenames to prevent conflicts
3. Implement virus scanning for production environments
4. Consider cloud storage (Azure Blob, AWS S3) for production
5. Implement proper error handling and user feedback

### Excel Export

1. Limit export size to prevent memory issues
2. Use streaming for large datasets
3. Implement pagination for exports
4. Cache frequently exported data
5. Provide export progress feedback for large operations

### Internationalization

1. Always use translation keys instead of hardcoded text
2. Keep translation files organized by feature
3. Test RTL layout thoroughly
4. Provide fallback text for missing translations
5. Keep translations in sync across all language files

---

## Future Enhancements

### File Upload
- Cloud storage integration (Azure Blob Storage, AWS S3)
- Image thumbnail generation
- Virus scanning integration
- Direct upload to CDN
- Chunked upload for large files

### Excel Export
- PDF export support
- CSV export option
- Custom styling and branding
- Charts and graphs support
- Template-based exports

### Internationalization
- Additional language support (French, Spanish, etc.)
- Date and number formatting localization
- Currency localization
- Pluralization support
- Translation management UI

---

For more information, please refer to:
- [RSA Certificate Setup](./RSA_CERTIFICATE_SETUP.md)
- [Clean Architecture Guide](./CLEAN_ARCHITECTURE.md)
