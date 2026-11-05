import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { INITIATIVES_API_ENDPOINTS } from '../../../core/constants/api.constants';
import {
  CreateInitiativeJoinRequestRequest,
  CreateInitiativeRequest,
  Initiative,
  InitiativeJoinRequest,
  InitiativeListQuery,
  UpdateInitiativeRequest,
} from '../models';

@Injectable({
  providedIn: 'root',
})
export class InitiativesApiService {
  private readonly http = inject(HttpClient);

  getAll(query: InitiativeListQuery = {}): Observable<Initiative[]> {
    let params = new HttpParams();

    if (query.search?.trim()) {
      params = params.set('search', query.search.trim());
    }

    if (query.goalType) {
      params = params.set('goalType', query.goalType);
    }

    if (query.limit) {
      params = params.set('limit', query.limit);
    }

    return this.http.get<Initiative[]>(INITIATIVES_API_ENDPOINTS.list, { params });
  }

  /** Initiatives owned by the current user, any status or visibility. */
  getMine(): Observable<Initiative[]> {
    return this.http.get<Initiative[]>(INITIATIVES_API_ENDPOINTS.mine);
  }

  getById(id: string): Observable<Initiative> {
    return this.http.get<Initiative>(INITIATIVES_API_ENDPOINTS.byId(id));
  }

  create(request: CreateInitiativeRequest): Observable<Initiative> {
    return this.http.post<Initiative>(INITIATIVES_API_ENDPOINTS.create, request);
  }

  update(id: string, request: UpdateInitiativeRequest): Observable<Initiative> {
    return this.http.put<Initiative>(INITIATIVES_API_ENDPOINTS.byId(id), request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(INITIATIVES_API_ENDPOINTS.byId(id));
  }

  createJoinRequest(
    initiativeId: string,
    request: CreateInitiativeJoinRequestRequest,
  ): Observable<InitiativeJoinRequest> {
    return this.http.post<InitiativeJoinRequest>(INITIATIVES_API_ENDPOINTS.joinRequests(initiativeId), request);
  }

  getJoinRequests(initiativeId: string): Observable<InitiativeJoinRequest[]> {
    return this.http.get<InitiativeJoinRequest[]>(INITIATIVES_API_ENDPOINTS.joinRequests(initiativeId));
  }

  acceptJoinRequest(initiativeId: string, requestId: string): Observable<InitiativeJoinRequest> {
    return this.http.post<InitiativeJoinRequest>(
      INITIATIVES_API_ENDPOINTS.acceptJoinRequest(initiativeId, requestId),
      {},
    );
  }

  rejectJoinRequest(initiativeId: string, requestId: string): Observable<InitiativeJoinRequest> {
    return this.http.post<InitiativeJoinRequest>(
      INITIATIVES_API_ENDPOINTS.rejectJoinRequest(initiativeId, requestId),
      {},
    );
  }
}
