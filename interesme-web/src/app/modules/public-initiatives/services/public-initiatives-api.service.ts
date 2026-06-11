import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { PUBLIC_INITIATIVES_API_ENDPOINTS } from '../../../core/constants/api.constants';
import { BYPASS_AUTH, BYPASS_REFRESH } from '../../../core/interceptors/http-context.tokens';
import { PublicInitiative } from '../../initiatives/models';

@Injectable({
  providedIn: 'root',
})
export class PublicInitiativesApiService {
  private readonly http = inject(HttpClient);
  private readonly publicContext = new HttpContext().set(BYPASS_AUTH, true).set(BYPASS_REFRESH, true);

  getBySlug(slug: string): Observable<PublicInitiative> {
    return this.http.get<PublicInitiative>(PUBLIC_INITIATIVES_API_ENDPOINTS.bySlug(slug), {
      context: this.publicContext,
    });
  }
}
