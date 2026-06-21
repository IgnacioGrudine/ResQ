import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) return router.parseUrl('/login');
  if (authService.role() === 'Admin') return true;

  // Authenticated but not an admin → send to their own area
  return router.parseUrl(authService.role() === 'Merchant' ? '/panel' : '/home');
};
