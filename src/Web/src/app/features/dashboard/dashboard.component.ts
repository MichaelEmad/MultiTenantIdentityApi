import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="dashboard">
      <h2>Dashboard</h2>
      @if (currentUser(); as user) {
        <div class="welcome-message">
          <h3>Welcome, {{ user.firstName || user.email }}!</h3>
          <p>Tenant: {{ user.tenantId }}</p>
          <p>Email: {{ user.email }}</p>
          @if (user.roles.length > 0) {
            <p>Roles: {{ user.roles.join(', ') }}</p>
          }
        </div>
      }

      <div class="actions">
        <button (click)="logout()">Logout</button>
      </div>
    </div>
  `,
  styles: [`
    .dashboard {
      max-width: 800px;
      margin: 0 auto;
      padding: 2rem;

      h2 {
        margin-bottom: 2rem;
        color: #333;
      }
    }

    .welcome-message {
      background: white;
      padding: 2rem;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      margin-bottom: 2rem;

      h3 {
        margin-bottom: 1rem;
        color: #007bff;
      }

      p {
        margin: 0.5rem 0;
        color: #666;
      }
    }

    .actions {
      button {
        padding: 0.75rem 1.5rem;
        background: #dc3545;
        color: white;
        border: none;
        border-radius: 4px;
        font-size: 1rem;
        cursor: pointer;
        transition: background 0.2s;

        &:hover {
          background: #c82333;
        }
      }
    }
  `]
})
export class DashboardComponent {
  currentUser = this.authService.currentUser;

  constructor(private authService: AuthService) {}

  logout(): void {
    this.authService.logout();
  }
}
