import apiClient from './client';
import { Order, OrderSummary, PaginatedResult, Address } from '../types';

interface OrderItemRequest {
  productId: string;
  quantity: number;
}

export const createOrder = (shippingAddress: Address, items: OrderItemRequest[]) =>
  apiClient.post<Order>('/api/orders', { shippingAddress, items });

export const getOrders = (page = 1, pageSize = 10, status?: string) =>
  apiClient.get<PaginatedResult<OrderSummary>>('/api/orders', {
    params: { page, pageSize, ...(status ? { status } : {}) },
  });

export const getOrder = (id: string) =>
  apiClient.get<Order>(`/api/orders/${id}`);

export const cancelOrder = (id: string) =>
  apiClient.patch<Order>(`/api/orders/${id}/cancel`);
