import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, finalize, map, Observable, of, shareReplay, tap, throwError } from 'rxjs';

import { APP_ROUTES } from '../../../core/constants/routes.constants';
import { TokenService } from '../../../core/services/token.service';
import { AuthResponse, GithubSessionRequest, GoogleAuthRequest, LoginRequest, RegisterRequest } from '../models';
import { AuthStore } from '../state/auth.store';
import { AuthApiService } from './auth-api.service';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly authApi = inject(AuthApiService);
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly tokenService = inject(TokenService);
  private validationRequest$: Observable<boolean> | null = null;

  readonly user = this.authStore.user;
  readonly isAuthenticated = this.authStore.isAuthenticated;
  readonly isLoading = this.authStore.isLoading;
  readonly error = this.authStore.error;

  login(request: LoginRequest): Observable<AuthResponse> {
    this.authStore.setLoading(true);
    this.authStore.setError(null);

    return this.authApi.login(request).pipe(
      tap((response) => this.authStore.setSession(response)),
      tap(() => this.authStore.setLoading(false)),
      catchError((error: unknown) => this.handleAuthError(error)),
    );
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    this.authStore.setLoading(true);
    this.authStore.setError(null);

    return this.authApi.register(request).pipe(
      tap((response) => this.authStore.setSession(response)),
      tap(() => this.authStore.setLoading(false)),
      catchError((error: unknown) => this.handleAuthError(error)),
    );
  }

  refreshSession(): Observable<string> {
    const refreshToken = this.tokenService.getRefreshToken();

    if (!refreshToken) {
      this.clearSession();
      return throwError(() => new Error('Refresh token is missing.'));
    }

    return this.authApi.refreshToken(refreshToken).pipe(
      tap((response) => this.authStore.updateTokens(response.accessToken, response.refreshToken)),
      map((response) => response.accessToken),
      catchError((error: unknown) => {
        this.clearSession();
        return throwError(() => error);
      }),
    );
  }

  validateSession(): Observable<boolean> {
    const user = this.tokenService.getUser();
    const accessToken = this.tokenService.getAccessToken();
    const refreshToken = this.tokenService.getRefreshToken();

    if ((user && !accessToken) || (accessToken && !user) || !user || !accessToken || !refreshToken) {
      this.clearSession();
      return of(false);
    }

    this.validationRequest$ ??= this.refreshSession().pipe(
      map(() => true),
      catchError(() => of(false)),
      finalize(() => (this.validationRequest$ = null)),
      shareReplay(1),
    );

    return this.validationRequest$;
  }

  googleLogin(request: GoogleAuthRequest): Observable<AuthResponse> {
    this.authStore.setLoading(true);
    this.authStore.setError(null);

    return this.authApi.googleLogin(request).pipe(
      tap((response) => this.authStore.setSession(response)),
      tap(() => this.authStore.setLoading(false)),
      catchError((error: unknown) => this.handleAuthError(error)),
    );
  }

  completeGithubSession(request: GithubSessionRequest): Observable<AuthResponse> {
    this.authStore.setLoading(true);
    this.authStore.setError(null);

    return this.authApi.completeGithubSession(request).pipe(
      tap((response) => this.authStore.setSession(response)),
      tap(() => this.authStore.setLoading(false)),
      catchError((error: unknown) => this.handleAuthError(error)),
    );
  }

  logout(): void {
    // TODO: Add backend logout/revoke endpoint if server-side token revocation on sign-out is required.
    this.clearAndRedirect();
  }

  clearSession(): void {
    this.authStore.clear();
  }

  private clearAndRedirect(): void {
    this.clearSession();
    void this.router.navigate([APP_ROUTES.login]);
  }

  private handleAuthError(error: unknown): Observable<never> {
    this.authStore.setLoading(false);
    this.authStore.setError('Authentication failed. Please check your details and try again.');
    return throwError(() => error);
  }
}
