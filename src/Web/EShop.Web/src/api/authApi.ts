import apiClient from './client';
import { AuthResponse } from '../types';

export const login = (email: string, password: string) =>
  apiClient.post<AuthResponse>('/api/auth/login', { email, password });

export const register = (email: string, password: string, fullName: string) =>
  apiClient.post('/api/auth/register', { email, password, fullName });
