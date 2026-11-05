import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { APP_ENVIRONMENT } from '../../../../core/constants/app-environment.constants';
import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { InterestResponse } from '../../../profile/models';
import { ProfileApiService } from '../../../profile/services/profile-api.service';
import { PeopleSearchResponse, PersonMatch } from '../../models/people-search';
import { PeopleSearchApiService } from '../../services/people-search-api.service';

@Component({
  selector: 'app-people-search',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './people-search.html',
  styleUrl: './people-search.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PeopleSearchPage implements OnInit {
  private readonly peopleApi = inject(PeopleSearchApiService);
  private readonly profileApi = inject(ProfileApiService);
  private readonly i18n = inject(I18nService);

  readonly profileEditRoute = ['/', APP_ROUTES.profile, 'edit'];
  readonly interests = signal<InterestResponse[]>([]);
  readonly selectedInterestId = signal<string>('');
  readonly result = signal<PeopleSearchResponse | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly people = computed(() => this.result()?.items ?? []);
  readonly hasNoOwnInterests = computed(() => {
    const result = this.result();
    return !!result && result.usedMyInterests && result.interestIds.length === 0;
  });
  readonly isEmpty = computed(() => !!this.result() && !this.hasNoOwnInterests() && this.people().length === 0);

  readonly text = computed(() => {
    this.i18n.currentLocale();

    return {
      eyebrow: this.i18n.t('people.eyebrow'),
      title: this.i18n.t('people.title'),
      subtitle: this.i18n.t('people.subtitle'),
      filterLabel: this.i18n.t('people.filterLabel'),
      filterMine: this.i18n.t('people.filterMine'),
      loading: this.i18n.t('people.loading'),
      retry: this.i18n.t('people.retry'),
      skills: this.i18n.t('people.skills'),
      openProfile: this.i18n.t('people.openProfile'),
      emptyTitle: this.i18n.t('people.emptyTitle'),
      emptyDescription: this.i18n.t('people.emptyDescription'),
      noInterestsTitle: this.i18n.t('people.noInterestsTitle'),
      noInterestsDescription: this.i18n.t('people.noInterestsDescription'),
      noInterestsCta: this.i18n.t('people.noInterestsCta'),
      resultsCount: this.i18n.t('people.resultsCount', { count: this.people().length }),
    };
  });

  ngOnInit(): void {
    this.profileApi.getInterests().subscribe({
      next: (interests) => this.interests.set(interests),
      error: () => this.interests.set([]),
    });

    this.search();
  }

  selectInterest(event: Event): void {
    this.selectedInterestId.set((event.target as HTMLSelectElement).value);
    this.search();
  }

  search(): void {
    const interestId = this.selectedInterestId();

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.peopleApi.searchPeople(interestId ? [interestId] : []).subscribe({
      next: (result) => {
        this.result.set(result);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.result.set(null);
        this.errorMessage.set(getApiErrorMessage(error, this.i18n.t('people.loadError')));
        this.isLoading.set(false);
      },
    });
  }

  profileLink(person: PersonMatch): string[] {
    return ['/', 'u', person.userId];
  }

  sharedCountLabel(person: PersonMatch): string {
    return this.i18n.t('people.sharedCount', { count: person.sharedInterestCount });
  }

  initial(person: PersonMatch): string {
    return person.displayName.trim().charAt(0).toUpperCase() || 'I';
  }

  avatarUrl(person: PersonMatch): string | null {
    const url = person.avatarUrl?.trim();

    if (!url) {
      return null;
    }

    if (!url.startsWith('/uploads')) {
      return url;
    }

    const apiBase = APP_ENVIRONMENT.apiBaseUrl.replace(/\/$/, '').replace(/\/api$/, '');
    const origin = /^https?:\/\//i.test(apiBase) ? apiBase : (globalThis.location?.origin ?? '');

    return `${origin}${url}`;
  }
}
