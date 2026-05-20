export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterConsumerRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
}

export interface RegisterMerchantRequest {
  email: string;
  password: string;
  businessName: string;
  cuit: string;
  address: string;
  latitude: number;
  longitude: number;
  contactPhone: string;
}

export interface ClientAuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  role: 'Consumer' | 'Merchant' | 'Admin';
  profileId: number | null;
}
