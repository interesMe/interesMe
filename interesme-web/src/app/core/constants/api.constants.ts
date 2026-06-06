import { APP_ENVIRONMENT } from './app-environment.constants';

export const API_BASE_URL = APP_ENVIRONMENT.apiBaseUrl;

export const AUTH_API_ENDPOINTS = {
  login: `${API_BASE_URL}/auth/login`,
  register: `${API_BASE_URL}/auth/register`,
  refreshToken: `${API_BASE_URL}/auth/refresh`,
  googleLogin: `${API_BASE_URL}/auth/google`,
} as const;
