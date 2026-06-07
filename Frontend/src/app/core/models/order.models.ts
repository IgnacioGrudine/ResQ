export interface CreateOrderRequest {
  productId: number;
  quantity:  number;
}

export interface OrderCreatedResponse {
  orderId:        number;
  mpPreferenceId: string;
  mpCheckoutUrl:  string;
}

export interface OrderItem {
  productName: string;
  quantity:    number;
  unitPrice:   number;
}

export type OrderStatus = 'Pending' | 'Paid' | 'PickedUp' | 'Cancelled';

export interface OrderSummary {
  id:                number;
  externalReference: string;
  merchantName:      string;
  totalAmount:       number;
  orderStatus:       OrderStatus;
  pickupCode:        string;
  createdAt:         string;
  items:             OrderItem[];
}
