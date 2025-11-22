import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TenantDto, CreateTenantRequest, UpdateTenantRequest } from '../models/tenant.model';
import { environment } from '@environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TenantService {
  private readonly API_URL = `${environment.apiUrl}/tenants`;
  private readonly TENANT_KEY = 'current_tenant';

  currentTenant = signal<string | null>(this.getTenantFromStorage());

  constructor(private http: HttpClient) {}

  getAllTenants(): Observable<TenantDto[]> {
    return this.http.get<TenantDto[]>(this.API_URL);
  }

  getTenantById(id: string): Observable<TenantDto> {
    return this.http.get<TenantDto>(`${this.API_URL}/${id}`);
  }

  getTenantByIdentifier(identifier: string): Observable<TenantDto> {
    return this.http.get<TenantDto>(`${this.API_URL}/by-identifier/${identifier}`);
  }

  createTenant(request: CreateTenantRequest): Observable<TenantDto> {
    return this.http.post<TenantDto>(this.API_URL, request);
  }

  updateTenant(id: string, request: UpdateTenantRequest): Observable<TenantDto> {
    return this.http.put<TenantDto>(`${this.API_URL}/${id}`, request);
  }

  deleteTenant(id: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${id}`);
  }

  activateTenant(id: string): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/${id}/activate`, {});
  }

  deactivateTenant(id: string): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/${id}/deactivate`, {});
  }

  setCurrentTenant(tenantId: string): void {
    localStorage.setItem(this.TENANT_KEY, tenantId);
    this.currentTenant.set(tenantId);
  }

  getCurrentTenantId(): string | null {
    return this.currentTenant();
  }

  private getTenantFromStorage(): string | null {
    return localStorage.getItem(this.TENANT_KEY);
  }
}
