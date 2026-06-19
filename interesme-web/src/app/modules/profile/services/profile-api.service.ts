import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, of, throwError } from 'rxjs';

import { INTERESTS_API_ENDPOINTS, PROFILE_API_ENDPOINTS } from '../../../core/constants/api.constants';
import {
  InterestResponse,
  ProfileRequest,
  ProfileResponse,
  ProfileViewResponse,
  UpdateUserInterestsRequest,
} from '../models';

@Injectable({
  providedIn: 'root',
})
export class ProfileApiService {
  private readonly http = inject(HttpClient);

  getMyProfile(): Observable<ProfileResponse | null> {
    return this.http.get<ProfileResponse | null>(PROFILE_API_ENDPOINTS.me).pipe(
      map((profile) => profile ?? null),
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 404) {
          return of(null);
        }

        return throwError(() => error);
      }),
    );
  }

  getMyProfileView(): Observable<ProfileViewResponse> {
    return this.http.get<ProfileViewResponse>(PROFILE_API_ENDPOINTS.myView);
  }

  getUserProfileView(userId: string): Observable<ProfileViewResponse> {
    return this.http.get<ProfileViewResponse>(PROFILE_API_ENDPOINTS.userView(userId));
  }

  followUser(userId: string): Observable<void> {
    return this.http.post<void>(PROFILE_API_ENDPOINTS.follow(userId), {});
  }

  unfollowUser(userId: string): Observable<void> {
    return this.http.delete<void>(PROFILE_API_ENDPOINTS.follow(userId));
  }

  createMyProfile(request: ProfileRequest): Observable<ProfileResponse> {
    return this.http.post<ProfileResponse>(PROFILE_API_ENDPOINTS.me, request);
  }

  createMyProfileWithAvatar(request: ProfileRequest, avatarFile: File): Observable<ProfileResponse> {
    return this.http.post<ProfileResponse>(
      PROFILE_API_ENDPOINTS.me,
      this.buildProfileFormData(request, avatarFile),
    );
  }

  updateMyProfile(request: ProfileRequest): Observable<ProfileResponse> {
    return this.http.put<ProfileResponse>(PROFILE_API_ENDPOINTS.me, request);
  }

  updateMyProfileWithAvatar(request: ProfileRequest, avatarFile: File): Observable<ProfileResponse> {
    return this.http.put<ProfileResponse>(
      PROFILE_API_ENDPOINTS.me,
      this.buildProfileFormData(request, avatarFile),
    );
  }

  getInterests(): Observable<InterestResponse[]> {
    return this.http.get<InterestResponse[] | null>(INTERESTS_API_ENDPOINTS.list).pipe(map((interests) => interests ?? []));
  }

  saveMyInterests(request: UpdateUserInterestsRequest): Observable<InterestResponse[]> {
    return this.http
      .put<InterestResponse[] | null>(PROFILE_API_ENDPOINTS.myInterests, request)
      .pipe(map((interests) => interests ?? []));
  }

  private buildProfileFormData(request: ProfileRequest, avatarFile: File): FormData {
    const formData = new FormData();

    formData.append('displayName', request.displayName);

    if (request.city) {
      formData.append('city', request.city);
    }

    if (request.birthDate) {
      formData.append('birthDate', request.birthDate);
    }

    formData.append('avatarFile', avatarFile);

    return formData;
  }
}
