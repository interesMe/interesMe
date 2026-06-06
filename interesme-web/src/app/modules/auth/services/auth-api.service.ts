import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { AUTH_API_ENDPOINTS } from '../../../core/constants/api.constants';
import { BYPASS_AUTH, BYPASS_REFRESH } from '../../../core/interceptors/http-context.tokens';
import { AuthResponse, BackendAuthResponse, GoogleAuthRequest, LoginRequest, RefreshResponse, RegisterRequest } from '../models';

@Injectable({
  providedIn: 'root',
})
export class AuthApiService {
  private readonly http = inject(HttpClient);
  private readonly unauthenticatedContext = new HttpContext().set(BYPASS_AUTH, true).set(BYPASS_REFRESH, true);

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<BackendAuthResponse>(AUTH_API_ENDPOINTS.login, request, {
        context: this.unauthenticatedContext,
      })
      .pipe(map((response) => this.mapAuthResponse(response)));
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<BackendAuthResponse>(
        AUTH_API_ENDPOINTS.register,
        {
          email: request.email,
          displayName: request.displayName,
          password: request.password,
        },
        {
          context: this.unauthenticatedContext,
        },
      )
      .pipe(map((response) => this.mapAuthResponse(response)));
  }

  refreshToken(refreshToken: string): Observable<RefreshResponse> {
    return this.http.post<RefreshResponse>(
      AUTH_API_ENDPOINTS.refreshToken,
      { refreshToken },
      {
        context: this.unauthenticatedContext,
      },
    );
  }

  googleLogin(request: GoogleAuthRequest): Observable<AuthResponse> {
    return this.http
      .post<BackendAuthResponse>(AUTH_API_ENDPOINTS.googleLogin, request, {
        context: this.unauthenticatedContext,
      })
      .pipe(map((response) => this.mapAuthResponse(response)));
  }

  private mapAuthResponse(response: BackendAuthResponse): AuthResponse {
    return {
      accessToken: response.token,
      refreshToken: response.refreshToken,
      user: {
        id: response.userId,
        email: response.email,
        displayName: response.displayName,
        roles: [],
      },
    };
  }
}
