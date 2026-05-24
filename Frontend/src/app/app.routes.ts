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

  // ── Consumer area ──────────────────────────────────────────────
  {
    path: 'home',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layouts/consumer-layout/consumer-layout.component')
        .then(m => m.ConsumerLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/consumer/feed/feed.component').then(m => m.FeedComponent)
      },
      {
        path: 'orders',
        loadComponent: () =>
          import('./features/consumer/orders/orders.component').then(m => m.OrdersComponent)
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('./features/consumer/profile/profile.component').then(m => m.ProfileComponent)
      }
    ]
  },

  // ── Detail screens (no consumer layout) ────────────────────────
  {
    path: 'packs/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/consumer/pack-detail/pack-detail.component')
        .then(m => m.PackDetailComponent)
  },
  {
    path: 'merchant/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/consumer/merchant-detail/merchant-detail.component')
        .then(m => m.MerchantDetailComponent)
  },

  {
    path: '**',
    redirectTo: ''
  }
];
