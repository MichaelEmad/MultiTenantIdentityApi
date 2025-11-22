import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { TenantService } from '@core/services/tenant.service';
import { TranslatePipe } from '@shared/pipes/translate.pipe';
import { LanguageSwitcherComponent } from '@shared/components/language-switcher/language-switcher.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TranslatePipe, LanguageSwitcherComponent],
  template: `
    <div class="register-container">
      <div class="register-card">
        <div class="language-switcher-container">
          <app-language-switcher></app-language-switcher>
        </div>

        <h2>{{ 'auth.register-title' | translate }}</h2>
        <form [formGroup]="registerForm" (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label for="tenantId">{{ 'auth.tenant-id' | translate }}</label>
            <input id="tenantId" type="text" formControlName="tenantId" [placeholder]="'auth.tenant-id' | translate" required>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="firstName">{{ 'auth.first-name' | translate }}</label>
              <input id="firstName" type="text" formControlName="firstName" [placeholder]="'auth.first-name' | translate">
            </div>

            <div class="form-group">
              <label for="lastName">{{ 'auth.last-name' | translate }}</label>
              <input id="lastName" type="text" formControlName="lastName" [placeholder]="'auth.last-name' | translate">
            </div>
          </div>

          <div class="form-group">
            <label for="email">{{ 'auth.email' | translate }}</label>
            <input id="email" type="email" formControlName="email" [placeholder]="'auth.email' | translate" required>
          </div>

          <div class="form-group">
            <label for="password">{{ 'auth.password' | translate }}</label>
            <input id="password" type="password" formControlName="password" [placeholder]="'auth.password' | translate" required>
          </div>

          <div class="form-group">
            <label for="confirmPassword">{{ 'auth.confirm-password' | translate }}</label>
            <input id="confirmPassword" type="password" formControlName="confirmPassword" [placeholder]="'auth.confirm-password' | translate" required>
          </div>

          @if (errorMessage) {
            <div class="error-message">{{ errorMessage }}</div>
          }

          <button type="submit" [disabled]="!registerForm.valid || loading">
            {{ loading ? ('common.loading' | translate) : ('auth.sign-up' | translate) }}
          </button>
        </form>

        <div class="login-link">
          {{ 'auth.have-account' | translate }} <a routerLink="/auth/login">{{ 'auth.login' | translate }}</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .register-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: calc(100vh - 200px);
      padding: 20px;
    }

    .register-card {
      background: white;
      padding: 2rem;
      border-radius: 8px;
      box-shadow: 0 2px 10px rgba(0,0,0,0.1);
      width: 100%;
      max-width: 500px;

      .language-switcher-container {
        display: flex;
        justify-content: flex-end;
        margin-bottom: 1rem;
      }

      h2 {
        margin-bottom: 1.5rem;
        color: #333;
        text-align: center;
      }
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
    }

    .form-group {
      margin-bottom: 1rem;

      label {
        display: block;
        margin-bottom: 0.5rem;
        color: #555;
        font-weight: 500;
      }

      input {
        width: 100%;
        padding: 0.75rem;
        border: 1px solid #ddd;
        border-radius: 4px;
        font-size: 1rem;

        &:focus {
          outline: none;
          border-color: #007bff;
        }
      }
    }

    .error-message {
      background: #fee;
      color: #c33;
      padding: 0.75rem;
      border-radius: 4px;
      margin-bottom: 1rem;
      font-size: 0.875rem;
    }

    button {
      width: 100%;
      padding: 0.75rem;
      background: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      font-size: 1rem;
      font-weight: 500;
      cursor: pointer;
      transition: background 0.2s;

      &:hover:not(:disabled) {
        background: #0056b3;
      }

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    }

    .login-link {
      margin-top: 1.5rem;
      text-align: center;
      color: #666;
    }
  `]
})
export class RegisterComponent {
  registerForm: FormGroup;
  loading = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private tenantService: TenantService,
    private router: Router
  ) {
    this.registerForm = this.fb.group({
      tenantId: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required],
      firstName: [''],
      lastName: ['']
    }, {
      validators: this.passwordMatchValidator
    });
  }

  passwordMatchValidator(form: FormGroup) {
    const password = form.get('password');
    const confirmPassword = form.get('confirmPassword');
    if (password && confirmPassword && password.value !== confirmPassword.value) {
      confirmPassword.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }
    return null;
  }

  onSubmit(): void {
    if (this.registerForm.valid) {
      this.loading = true;
      this.errorMessage = '';

      const { tenantId, email, password, confirmPassword, firstName, lastName } = this.registerForm.value;

      // Set tenant before registration
      this.tenantService.setCurrentTenant(tenantId);

      this.authService.register({
        email,
        password,
        confirmPassword,
        firstName,
        lastName
      }).subscribe({
        next: (response) => {
          if (response.succeeded) {
            this.router.navigate(['/dashboard']);
          } else {
            this.errorMessage = response.errors?.join(', ') || 'Registration failed';
          }
          this.loading = false;
        },
        error: (error) => {
          this.errorMessage = error.error?.message || 'An error occurred during registration';
          this.loading = false;
        }
      });
    }
  }
}
