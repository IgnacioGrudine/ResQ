export type NotificationType = 'OrderPaid' | 'OrderCancelled';

export interface MerchantNotification {
  id: number;
  type: NotificationType;
  title: string;
  message: string;
  isRead: boolean;
  orderId: number | null;
  createdAt: string;
}
