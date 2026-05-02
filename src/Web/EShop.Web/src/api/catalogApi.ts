import apiClient from './client';
import { Product } from '../types';

export const getProducts = () =>
  apiClient.get<Product[]>('/api/products');

export const getProduct = (id: string) =>
  apiClient.get<Product>(`/api/products/${id}`);
