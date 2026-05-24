export type OrderStatus = 'Pending' | 'Paid' | 'PickedUp' | 'Cancelled';

export interface OrderItem {
  packName: string;
  quantity: number;
  unitPrice: number;
}

export interface Order {
  id: number;
  merchantName: string;
  status: OrderStatus;
  pickupCode?: string;
  totalAmount: number;
  createdAt: string;
  items: OrderItem[];
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
