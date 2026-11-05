import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { DISCOVERY_API_ENDPOINTS } from '../../../core/constants/api.constants';
import { PeopleSearchResponse } from '../models/people-search';

@Injectable({
  providedIn: 'root',
})
export class PeopleSearchApiService {
  private readonly http = inject(HttpClient);

  /** Without interestIds the backend searches by the current user's own interests. */
  searchPeople(interestIds: readonly string[] = [], limit = 20): Observable<PeopleSearchResponse> {
    let params = new HttpParams().set('limit', limit);

    for (const interestId of interestIds) {
      params = params.append('interestIds', interestId);
    }

    return this.http.get<PeopleSearchResponse>(DISCOVERY_API_ENDPOINTS.people, { params });
  }
}
