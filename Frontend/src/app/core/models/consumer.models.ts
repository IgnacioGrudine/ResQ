export type OrderStatus = 'Pending' | 'Paid' | 'PickedUp' | 'Cancelled';

export interface OrderItem {
  productName: string;
  quantity: number;
  unitPrice: number;
}

export interface Order {
  id: number;
  externalReference: string;
  merchantName: string;
  totalAmount: number;
  orderStatus: OrderStatus;
  pickupCode?: string;
  createdAt: string;
  items: OrderItem[];
  hasReview: boolean;
}

export interface ConsumerProfile {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  totalOrders: number;
  totalSaved: number;
  co2SavedKg: number;
}
