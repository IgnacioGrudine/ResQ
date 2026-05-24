import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const merchantGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) return router.parseUrl('/login');
  if (authService.role() === 'Merchant') return true;

  // Authenticated but not a merchant → send to the consumer area
  return router.parseUrl('/home');
};
