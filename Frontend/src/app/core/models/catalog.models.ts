export interface PackListItem {
  id: number;
  name: string;
  description?: string;
  imageUrl?: string;
  productType: string;
  originalPrice: number;
  salePrice: number;
  stockQuantity: number;
  pickupTimeStart: string;
  pickupTimeEnd: string;
  merchantId: number;
  merchantName: string;
  merchantAverageRating: number;
  merchantReviewCount: number;
  distanceKm?: number;
}

export interface Review {
  id: number;
  rating: number;
  comment?: string;
  createdAt: string;
}

export interface Category {
  id: number;
  name: string;
}

export interface MerchantProduct {
  id: number;
  name: string;
  description?: string;
  imageUrl?: string;
  productType: string;
  originalPrice: number;
  salePrice: number;
  stockQuantity: number;
  pickupTimeStart: string;
  pickupTimeEnd: string;
  isActive: boolean;
}

export interface MerchantDetail {
  id: number;
  businessName: string;
  address: string;
  latitude: number;
  longitude: number;
  contactPhone: string;
  photoUrl?: string;
  categories: Category[];
  averageRating: number;
  reviewCount: number;
  activeProducts: MerchantProduct[];
  recentReviews: Review[];
}
