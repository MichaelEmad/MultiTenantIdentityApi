import { Routes } from '@angular/router';

export const TENANTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./tenants-list/tenants-list.component').then(m => m.TenantsListComponent)
  }
];
