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
  private readonly state = signal<AuthState>({
    user: this.tokenService.getUser(),
    accessToken: this.tokenService.getAccessToken(),
    refreshToken: this.tokenService.getRefreshToken(),
    isLoading: false,
    error: null,
  });

  readonly user = computed(() => this.state().user);
  readonly accessToken = computed(() => this.state().accessToken);
  readonly refreshToken = computed(() => this.state().refreshToken);
  readonly isLoading = computed(() => this.state().isLoading);
  readonly error = computed(() => this.state().error);
  readonly isAuthenticated = computed(() =>
    Boolean(this.state().accessToken && this.state().user && !this.tokenService.isAccessTokenExpired()),
  );

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

  clear(): void {
    this.tokenService.clear();
    this.state.set({
      user: null,
      accessToken: null,
      refreshToken: null,
      isLoading: false,
      error: null,
    });
  }

  private patch(state: Partial<AuthState>): void {
    this.state.update((currentState) => ({ ...currentState, ...state }));
  }
}
