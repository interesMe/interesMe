import { Injectable } from '@angular/core';

import { User } from '../models/user';

const ACCESS_TOKEN_KEY = 'interesme.accessToken';
const REFRESH_TOKEN_KEY = 'interesme.refreshToken';
const USER_KEY = 'interesme.user';

@Injectable({
  providedIn: 'root',
})
export class TokenService {
  getAccessToken(): string | null {
    return this.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return this.getItem(REFRESH_TOKEN_KEY);
  }

  getUser(): User | null {
    const rawUser = this.getItem(USER_KEY);

    if (!rawUser) {
      return null;
    }

    try {
      return JSON.parse(rawUser) as User;
    } catch {
      this.clear();
      return null;
    }
  }

  setSession(accessToken: string, refreshToken: string, user: User): void {
    this.setItem(ACCESS_TOKEN_KEY, accessToken);
    this.setItem(REFRESH_TOKEN_KEY, refreshToken);
    this.setItem(USER_KEY, JSON.stringify(user));
  }

  setAccessToken(accessToken: string): void {
    this.setItem(ACCESS_TOKEN_KEY, accessToken);
  }

  setRefreshToken(refreshToken: string): void {
    this.setItem(REFRESH_TOKEN_KEY, refreshToken);
  }

  clear(): void {
    this.removeItem(ACCESS_TOKEN_KEY);
    this.removeItem(REFRESH_TOKEN_KEY);
    this.removeItem(USER_KEY);
  }

  isAccessTokenExpired(offsetSeconds = 30): boolean {
    const token = this.getAccessToken();

    if (!token) {
      return true;
    }

    const expiresAt = this.getJwtExpiry(token);

    if (!expiresAt) {
      return true;
    }

    return Date.now() >= expiresAt - offsetSeconds * 1000;
  }

  private getJwtExpiry(token: string): number | null {
    const [, payload] = token.split('.');

    if (!payload) {
      return null;
    }

    try {
      const normalizedPayload = payload.replace(/-/g, '+').replace(/_/g, '/');
      const decodedPayload = JSON.parse(atob(normalizedPayload)) as { exp?: number };
      return decodedPayload.exp ? decodedPayload.exp * 1000 : null;
    } catch {
      return null;
    }
  }

  private getItem(key: string): string | null {
    if (!this.hasStorage()) {
      return null;
    }

    return localStorage.getItem(key);
  }

  private setItem(key: string, value: string): void {
    if (this.hasStorage()) {
      localStorage.setItem(key, value);
    }
  }

  private removeItem(key: string): void {
    if (this.hasStorage()) {
      localStorage.removeItem(key);
    }
  }

  private hasStorage(): boolean {
    return typeof localStorage !== 'undefined';
  }
}
