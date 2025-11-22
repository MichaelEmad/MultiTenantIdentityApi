export interface TenantDto {
  id: string;
  identifier: string;
  name: string;
  isActive: boolean;
  createdAt: Date;
  settings?: string;
}

export interface CreateTenantRequest {
  identifier: string;
  name: string;
  connectionString?: string;
  settings?: string;
}

export interface UpdateTenantRequest {
  name?: string;
  connectionString?: string;
  isActive?: boolean;
  settings?: string;
}
