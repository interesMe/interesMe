import { computed, inject, Injectable, signal } from '@angular/core';

import { TokenService } from '../../../core/services/token.service';
import { User } from '../../../core/models/user';
import { AuthResponse } from '../models';

interface AuthState {
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  isLoading: boolean;
  error: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class AuthStore {
  private readonly tokenService = inject(TokenService);
  private readonly state = signal<AuthState>(this.createInitialState());

  readonly user = computed(() => this.state().user);
  readonly accessToken = computed(() => this.state().accessToken);
  readonly refreshToken = computed(() => this.state().refreshToken);
  readonly isLoading = computed(() => this.state().isLoading);
  readonly error = computed(() => this.state().error);
  readonly isAuthenticated = computed(() => {
    const state = this.state();
    const isAuthenticated = Boolean(state.accessToken && state.user && !this.tokenService.isAccessTokenExpired());

    console.debug('[AuthStore.isAuthenticated]', {
      hasUser: Boolean(state.user),
      hasAccessToken: Boolean(state.accessToken),
      isAccessTokenExpired: this.tokenService.isAccessTokenExpired(),
      isAuthenticated,
    });

    return isAuthenticated;
  });

  setLoading(isLoading: boolean): void {
    this.patch({ isLoading });
  }

  setError(error: string | null): void {
    this.patch({ error });
  }

  setSession(response: AuthResponse): void {
    this.tokenService.setSession(response.accessToken, response.refreshToken, response.user);
    this.patch({
      user: response.user,
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      error: null,
    });
  }

  updateAccessToken(accessToken: string): void {
    this.tokenService.setAccessToken(accessToken);
    this.patch({ accessToken });
  }

  updateTokens(accessToken: string, refreshToken: string): void {
    this.tokenService.setAccessToken(accessToken);
    this.tokenService.setRefreshToken(refreshToken);
    this.patch({ accessToken, refreshToken });
  }

  hasCompleteSession(): boolean {
    const { user, accessToken } = this.state();
    const hasCompleteSession = Boolean(user && accessToken && !this.tokenService.isAccessTokenExpired());

    console.debug('[AuthStore.hasCompleteSession]', {
      hasUser: Boolean(user),
      hasAccessToken: Boolean(accessToken),
      hasRefreshToken: Boolean(this.state().refreshToken),
      isAccessTokenExpired: this.tokenService.isAccessTokenExpired(),
      hasCompleteSession,
    });

    if ((user && !accessToken) || (accessToken && !user)) {
      this.clear();
      return false;
    }

    if (!user || !accessToken) {
      return false;
    }

    if (this.tokenService.isAccessTokenExpired() && !this.state().refreshToken) {
      this.clear();
      return false;
    }

    return !this.tokenService.isAccessTokenExpired();
  }

  clear(): void {
    this.tokenService.clearSession();
    this.state.set({
      user: null,
      accessToken: null,
      refreshToken: null,
      isLoading: false,
      error: null,
    });
  }

  private createInitialState(): AuthState {
    const user = this.tokenService.getUser();
    const accessToken = this.tokenService.getAccessToken();

    if ((user && !accessToken) || (accessToken && !user)) {
      this.tokenService.clearSession();
      return this.emptyState();
    }

    return {
      user,
      accessToken,
      refreshToken: this.tokenService.getRefreshToken(),
      isLoading: false,
      error: null,
    };
  }

  private emptyState(): AuthState {
    return {
      user: null,
      accessToken: null,
      refreshToken: null,
      isLoading: false,
      error: null,
    };
  }

  private patch(state: Partial<AuthState>): void {
    this.state.update((currentState) => ({ ...currentState, ...state }));
  }
}
