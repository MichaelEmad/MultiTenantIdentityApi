import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TenantService } from '@core/services/tenant.service';
import { TenantDto } from '@core/models/tenant.model';

@Component({
  selector: 'app-tenants-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="tenants-list">
      <h2>Tenants</h2>

      @if (loading) {
        <div class="loading">Loading tenants...</div>
      } @else if (tenants.length > 0) {
        <div class="tenants-grid">
          @for (tenant of tenants; track tenant.id) {
            <div class="tenant-card" [class.inactive]="!tenant.isActive">
              <h3>{{ tenant.name }}</h3>
              <p class="identifier">{{ tenant.identifier }}</p>
              <div class="tenant-info">
                <span class="status" [class.active]="tenant.isActive">
                  {{ tenant.isActive ? 'Active' : 'Inactive' }}
                </span>
                <span class="date">Created: {{ tenant.createdAt | date }}</span>
              </div>
            </div>
          }
        </div>
      } @else {
        <div class="no-tenants">No tenants found</div>
      }
    </div>
  `,
  styles: [`
    .tenants-list {
      max-width: 1200px;
      margin: 0 auto;
      padding: 2rem;

      h2 {
        margin-bottom: 2rem;
        color: #333;
      }
    }

    .loading, .no-tenants {
      text-align: center;
      padding: 3rem;
      color: #666;
    }

    .tenants-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 1.5rem;
    }

    .tenant-card {
      background: white;
      padding: 1.5rem;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      transition: transform 0.2s;

      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 4px 8px rgba(0,0,0,0.15);
      }

      &.inactive {
        opacity: 0.6;
      }

      h3 {
        margin-bottom: 0.5rem;
        color: #333;
      }

      .identifier {
        color: #666;
        font-size: 0.875rem;
        margin-bottom: 1rem;
      }

      .tenant-info {
        display: flex;
        justify-content: space-between;
        align-items: center;
        font-size: 0.875rem;

        .status {
          padding: 0.25rem 0.75rem;
          border-radius: 12px;
          background: #ddd;
          color: #666;

          &.active {
            background: #d4edda;
            color: #155724;
          }
        }

        .date {
          color: #999;
        }
      }
    }
  `]
})
export class TenantsListComponent implements OnInit {
  tenants: TenantDto[] = [];
  loading = true;

  constructor(private tenantService: TenantService) {}

  ngOnInit(): void {
    this.loadTenants();
  }

  loadTenants(): void {
    this.tenantService.getAllTenants().subscribe({
      next: (tenants) => {
        this.tenants = tenants;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading tenants:', error);
        this.loading = false;
      }
    });
  }
}
