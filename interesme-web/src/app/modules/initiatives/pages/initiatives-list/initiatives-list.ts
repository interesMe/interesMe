import { NgClass } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translations';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { Initiative, InitiativeGoalType } from '../../models';
import { InitiativesApiService } from '../../services/initiatives-api.service';

@Component({
  selector: 'app-initiatives-list-page',
  standalone: true,
  imports: [NgClass, RouterLink],
  templateUrl: './initiatives-list.html',
  styleUrl: './initiatives-list.scss',
})
export class InitiativesListPage implements OnInit {
  private readonly initiativesApi = inject(InitiativesApiService);
  private readonly i18n = inject(I18nService);

  readonly initiatives = signal<Initiative[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly createPath = `/${APP_ROUTES.initiatives}/new`;
  readonly searchText = signal('');
  readonly goalFilter = signal<InitiativeGoalType | null>(null);
  /** Filters used for the currently displayed results. */
  private readonly appliedFilters = signal<{ search: string; goalType: InitiativeGoalType | null }>({
    search: '',
    goalType: null,
  });
  readonly hasAppliedFilters = computed(() => {
    const filters = this.appliedFilters();
    return filters.search.length > 0 || filters.goalType !== null;
  });
  readonly goalOptions = [
    InitiativeGoalType.Connect,
    InitiativeGoalType.Learn,
    InitiativeGoalType.Build,
    InitiativeGoalType.Play,
    InitiativeGoalType.Explore,
  ];

  ngOnInit(): void {
    this.loadInitiatives();
  }

  updateSearch(event: Event): void {
    this.searchText.set((event.target as HTMLInputElement).value);
  }

  selectGoal(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.goalFilter.set(value ? (value as InitiativeGoalType) : null);
    this.applyFilters();
  }

  applyFilters(event?: Event): void {
    event?.preventDefault();
    this.loadInitiatives();
  }

  clearFilters(): void {
    this.searchText.set('');
    this.goalFilter.set(null);
    this.loadInitiatives();
  }

  loadInitiatives(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    const filters = { search: this.searchText().trim(), goalType: this.goalFilter() };

    this.initiativesApi.getAll({ search: filters.search, goalType: filters.goalType }).subscribe({
      next: (initiatives) => {
        this.initiatives.set(initiatives);
        this.appliedFilters.set(filters);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        const fallbackMessage = this.t('initiativesList.error.fallback');
        this.errorMessage.set(getApiErrorMessage(error, fallbackMessage));
        this.isLoading.set(false);
      },
    });
  }

  initiativePath(initiative: Initiative): string {
    return `/${APP_ROUTES.initiatives}/${initiative.id}`;
  }

  t(key: TranslationKey, params?: Record<string, string | number | boolean | null | undefined>): string {
    return this.i18n.t(key, params);
  }

  goalLabel(goalType: InitiativeGoalType): string {
    switch (goalType) {
      case InitiativeGoalType.Connect:
        return this.t('publicInitiative.goal.connect');
      case InitiativeGoalType.Learn:
        return this.t('publicInitiative.goal.learn');
      case InitiativeGoalType.Build:
        return this.t('publicInitiative.goal.build');
      case InitiativeGoalType.Play:
        return this.t('publicInitiative.goal.play');
      case InitiativeGoalType.Explore:
        return this.t('publicInitiative.goal.explore');
      default:
        return this.t('publicInitiative.goal.explore');
    }
  }

  goalClass(goalType: InitiativeGoalType): string {
    switch (goalType) {
      case InitiativeGoalType.Connect:
        return 'goal-connect';
      case InitiativeGoalType.Learn:
        return 'goal-learn';
      case InitiativeGoalType.Build:
        return 'goal-build';
      case InitiativeGoalType.Play:
        return 'goal-play';
      case InitiativeGoalType.Explore:
        return 'goal-explore';
      default:
        return 'goal-explore';
    }
  }
}
