import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/landing/landing.component').then(m => m.LandingComponent)
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/auth/register/role-picker.component').then(m => m.RolePickerComponent)
      },
      {
        path: 'consumer',
        loadComponent: () =>
          import('./features/auth/register/consumer/register-consumer.component')
            .then(m => m.RegisterConsumerComponent)
      },
      {
        path: 'merchant',
        loadComponent: () =>
          import('./features/auth/register/merchant/register-merchant.component')
            .then(m => m.RegisterMerchantComponent)
      }
    ]
  },
  {
    path: 'home',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
