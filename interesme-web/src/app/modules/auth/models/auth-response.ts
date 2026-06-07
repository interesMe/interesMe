import { User } from '../../../core/models/user';

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
}

export interface BackendAuthResponse {
  userId: string;
  email: string;
  displayName: string;
  token: string;
  refreshToken: string;
}

export interface RefreshResponse {
  accessToken: string;
  refreshToken: string;
}

export interface GoogleAuthRequest {
  idToken: string;
}

export interface GithubSessionRequest {
  sessionCode: string;
}
