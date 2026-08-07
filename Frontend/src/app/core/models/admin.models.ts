// ── Shared ──────────────────────────────────────────────────────────────────

export type ReportGranularity = 'Day' | 'Week' | 'Month';
export type ReportFormat       = 'Pdf' | 'Excel';

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

// ── Dashboard ─────────────────────────────────────────────────────────────────

export interface ActivityPoint {
  label: string;
  orders: number;
  gmv: number;
  platformRevenue: number;
}

export interface MerchantRanking {
  merchantId: number;
  businessName: string;
  orders: number;
  gmv: number;
  platformRevenue: number;
  averageRating: number;
}

export interface CategorySlice {
  category: string;
  orders: number;
  gmv: number;
}

export interface AdminAlert {
  severity: 'info' | 'warning' | 'critical';
  title: string;
  detail: string;
  merchantId: number | null;
}

export interface AdminDashboard {
  rangeLabel: string;

  gmv: number;
  platformRevenue: number;
  merchantEarnings: number;
  averageOrderValue: number;

  totalOrders: number;
  completedOrders: number;
  cancelledOrders: number;
  cancellationRate: number;
  pickupRate: number;

  totalMerchants: number;
  activeMerchants: number;
  merchantsWithMp: number;
  newMerchants: number;

  totalConsumers: number;
  activeConsumers: number;
  newConsumers: number;

  averageRating: number;
  reviewCount: number;
  activePacks: number;

  activitySeries: ActivityPoint[];
  topMerchantsByRevenue: MerchantRanking[];
  topMerchantsByOrders: MerchantRanking[];
  categoryDistribution: CategorySlice[];
  alerts: AdminAlert[];
}

// ── Merchant management ─────────────────────────────────────────────────────────

export interface AdminMerchantListItem {
  id: number;
  businessName: string;
  cuit: string;
  email: string;
  isActive: boolean;
  mpConnectionStatus: string;
  categories: string[];
  totalOrders: number;
  totalRevenue: number;
  averageRating: number;
  createdAt: string;
}

export interface AdminMerchantOrder {
  id: number;
  consumerName: string;
  totalAmount: number;
  orderStatus: string;
  pickupCode: string;
  createdAt: string;
  items: { productName: string; quantity: number; unitPrice: number }[];
}

export interface AdminMerchantDetail {
  id: number;
  businessName: string;
  cuit: string;
  email: string;
  address: string;
  contactPhone: string;
  isActive: boolean;
  mpConnectionStatus: string;
  categories: string[];
  totalOrders: number;
  totalGmv: number;
  totalRevenue: number;
  averageRating: number;
  reviewCount: number;
  activePacks: number;
  createdAt: string;
  recentOrders: AdminMerchantOrder[];
}

// ── User management ─────────────────────────────────────────────────────────────

export interface AdminUserListItem {
  id: number;
  email: string;
  role: string;
  displayName: string;
  isActive: boolean;
  profileId: number | null;
  createdAt: string;
}

// ── Filters ──────────────────────────────────────────────────────────────────

export interface DashboardFilter {
  from?: string;
  to?: string;
  granularity?: ReportGranularity;
}

export interface MerchantListFilter {
  active?: boolean | null;
  categoryId?: number | null;
  mpStatus?: string | null;
  page?: number;
  pageSize?: number;
}

export interface UserListFilter {
  role?: string | null;
  active?: boolean | null;
  page?: number;
  pageSize?: number;
}
