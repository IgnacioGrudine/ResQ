import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { merchantGuard } from './core/guards/merchant.guard';
import { adminGuard } from './core/guards/admin.guard';

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
        path: 'mapa',
        loadComponent: () =>
          import('./features/consumer/map/map.component').then(m => m.MapComponent)
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

  // ── Merchant area ──────────────────────────────────────────────
  {
    path: 'panel',
    canActivate: [merchantGuard],
    loadComponent: () =>
      import('./layouts/merchant-layout/merchant-layout.component')
        .then(m => m.MerchantLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/merchant/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'packs',
        loadComponent: () =>
          import('./features/merchant/packs/packs.component').then(m => m.PacksComponent)
      },
      {
        path: 'orders',
        loadComponent: () =>
          import('./features/merchant/orders/merchant-orders.component').then(m => m.MerchantOrdersComponent)
      },
      {
        path: 'reviews',
        loadComponent: () =>
          import('./features/merchant/reviews/reviews.component').then(m => m.ReviewsComponent)
      },
      {
        path: 'analytics',
        loadComponent: () =>
          import('./features/merchant/analytics/merchant-analytics.component').then(m => m.MerchantAnalyticsComponent)
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('./features/merchant/profile/merchant-profile.component').then(m => m.MerchantProfileComponent)
      }
    ]
  },

  // ── Admin area ─────────────────────────────────────────────────
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./layouts/admin-layout/admin-layout.component')
        .then(m => m.AdminLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/admin/dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent)
      },
      {
        path: 'merchants',
        loadComponent: () =>
          import('./features/admin/merchants/admin-merchants.component').then(m => m.AdminMerchantsComponent)
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./features/admin/users/admin-users.component').then(m => m.AdminUsersComponent)
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./features/admin/reports/admin-reports.component').then(m => m.AdminReportsComponent)
      },
      {
        path: 'categories',
        loadComponent: () =>
          import('./features/admin/categories/admin-categories.component').then(m => m.AdminCategoriesComponent)
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

  // ── Mercado Pago OAuth callback ─────────────────────────────────
  {
    path: 'mp/callback',
    loadComponent: () =>
      import('./features/auth/mp-callback/mp-callback.component')
        .then(m => m.MpCallbackComponent)
  },

  // ── Payment result pages ────────────────────────────────────────
  {
    path: 'pago/exitoso',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/consumer/payment-result/payment-result.component')
        .then(m => m.PaymentResultComponent)
  },
  {
    path: 'pago/fallido',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/consumer/payment-result/payment-result.component')
        .then(m => m.PaymentResultComponent)
  },
  {
    path: 'pago/pendiente',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/consumer/payment-result/payment-result.component')
        .then(m => m.PaymentResultComponent)
  },

  {
    path: '**',
    redirectTo: ''
  }
];
