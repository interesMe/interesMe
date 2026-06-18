import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';

import { INTERESTS_API_ENDPOINTS } from '../../../core/constants/api.constants';
import { InterestCatalogResponse, InterestCategory } from '../models/interest.model';
import { MOCK_INTEREST_CATEGORIES } from './interests.mock';

@Injectable({ providedIn: 'root' })
export class InterestsService {
  private readonly http = inject(HttpClient);
  private readonly categories = signal<InterestCategory[]>(MOCK_INTEREST_CATEGORIES);
  private readonly loading = signal(false);
  private readonly fallbackError = signal(false);

  readonly allCategories = this.categories.asReadonly();
  readonly isLoading = this.loading.asReadonly();
  readonly isUsingFallback = this.fallbackError.asReadonly();
  readonly hasData = computed(() => this.categories().length > 0);

  loadCatalog(): void {
    this.loading.set(true);
    this.fallbackError.set(false);

    this.getCatalog().subscribe((categories) => {
      this.categories.set(categories);
      this.loading.set(false);
    });
  }

  private getCatalog(): Observable<InterestCategory[]> {
    return this.http.get<InterestCatalogResponse | null>(INTERESTS_API_ENDPOINTS.catalog).pipe(
      map((response) => this.normalizeCategories(response?.categories ?? [])),
      map((categories) => (categories.length > 0 ? categories : MOCK_INTEREST_CATEGORIES)),
      catchError(() => {
        this.fallbackError.set(true);
        return of(MOCK_INTEREST_CATEGORIES);
      }),
    );
  }

  private normalizeCategories(categories: InterestCategory[]): InterestCategory[] {
    return categories.map((category) => ({
      ...category,
      peopleCount: category.peopleCount ?? 0,
      initiativesCount: category.initiativesCount ?? 0,
      portfolioCount: category.portfolioCount ?? 0,
      interests: (category.interests ?? []).map((interest) => ({
        ...interest,
        peopleCount: interest.peopleCount ?? 0,
        initiativesCount: interest.initiativesCount ?? 0,
        portfolioCount: interest.portfolioCount ?? 0,
        subinterests: (interest.subinterests ?? []).map((subinterest) => ({
          ...subinterest,
          peopleCount: subinterest.peopleCount ?? 0,
          initiativesCount: subinterest.initiativesCount ?? 0,
          portfolioCount: subinterest.portfolioCount ?? 0,
        })),
      })),
    }));
  }
}
