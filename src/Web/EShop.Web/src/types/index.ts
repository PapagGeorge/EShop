export interface Product {
  id: string;
  name: string;
  price: number;
  category: string;
}

export interface User {
  id: string;
  email: string;
  fullName: string;
  role: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export interface Address {
  street: string;
  city: string;
  zipCode: string;
  country: string;
}

export interface OrderItemDto {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
}

export interface Order {
  id: string;
  userId: string;
  status: string;
  shippingAddress: Address;
  items: OrderItemDto[];
  totalAmount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface OrderSummary {
  id: string;
  status: string;
  totalAmount: number;
  itemCount: number;
  createdAt: string;
}

export interface PaginatedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CartItem {
  product: Product;
  quantity: number;
}
