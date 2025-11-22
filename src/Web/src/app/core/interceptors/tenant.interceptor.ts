import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TenantService } from '../services/tenant.service';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const tenantService = inject(TenantService);
  const tenantId = tenantService.getCurrentTenantId();

  if (tenantId && !req.headers.has('X-Tenant-Id')) {
    req = req.clone({
      setHeaders: {
        'X-Tenant-Id': tenantId
      }
    });
  }

  return next(req);
};
