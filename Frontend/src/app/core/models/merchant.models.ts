import { Category } from './catalog.models';

// Responses map the enum to its name; requests expect the numeric value.
export type ProductType         = 'SurprisePack' | 'ExplicitItem';
export type MpConnectionStatus  = 'Disconnected' | 'Connected' | 'TokenExpired';
export const ProductTypeValue = { SurprisePack: 1, ExplicitItem: 2 } as const;

export type MerchantOrderStatus = 'Pending' | 'Paid' | 'PickedUp' | 'Cancelled';

// ── Dashboard ──
export interface DailySales {
  day: string;
  orders: number;
  income: number;
}

export interface MerchantDashboard {
  businessName: string;
  activeOrders: number;
  todayIncome: number;
  packsSoldToday: number;
  totalSales: number;
  totalIncome: number;
  kgFoodRescued: number;
  averageRating: number;
  reviewCount: number;
  activePackCount: number;
  mpConnectionStatus: string;
  weeklySales: DailySales[];
}

// ── Analytics (filterable) ──
export interface RatingBucket { stars: number; count: number; }
export interface TopProduct   { productId: number; name: string; unitsSold: number; revenue: number; }

export interface MerchantAnalytics {
  rangeLabel: string;
  gmv: number;
  earnings: number;
  averageOrderValue: number;
  previousEarnings: number;
  earningsGrowthPct: number;
  completedOrders: number;
  cancelledOrders: number;
  cancellationRate: number;
  packsSold: number;
  averageRating: number;
  reviewCount: number;
  ratingDistribution: RatingBucket[];
  topProducts: TopProduct[];
  activitySeries: DailySales[];
}

export type ReportFormat = 'Pdf' | 'Excel';

// ── Packs (own products, include isActive) ──
export interface MerchantProduct {
  id: number;
  name: string;
  description?: string;
  imageUrl?: string;
  productType: ProductType;
  originalPrice: number;
  salePrice: number;
  stockQuantity: number;
  pickupTimeStart: string;
  pickupTimeEnd: string;
  isActive: boolean;
}

export interface ProductPayload {
  name: string;
  description?: string;
  imageUrl?: string;
  productType: number; // 1 = SurprisePack, 2 = ExplicitItem (enum is numeric on the wire)
  originalPrice: number;
  salePrice: number;
  stockQuantity: number;
  pickupTimeStart: string;
  pickupTimeEnd: string;
}

// ── Orders ──
export interface MerchantOrderItem {
  productName: string;
  quantity: number;
  unitPrice: number;
}

export interface MerchantOrder {
  id: number;
  consumerName: string;
  totalAmount: number;
  orderStatus: MerchantOrderStatus;
  pickupCode: string;
  createdAt: string;
  items: MerchantOrderItem[];
}

// ── Reviews ──
export interface MerchantReview {
  id: number;
  rating: number;
  comment?: string;
  createdAt: string;
}

// ── Profile ──
export interface MerchantProfile {
  id:                  number;
  businessName:        string;
  cuit:                string;
  address:             string;
  latitude:            number;
  longitude:           number;
  contactPhone:        string;
  photoUrl?:           string;
  mpConnectionStatus:  MpConnectionStatus;
  categories:          Category[];
}

export interface UpdateMerchantProfilePayload {
  businessName: string;
  address: string;
  latitude: number;
  longitude: number;
  contactPhone: string;
  categoryIds: number[];
}
