import { NgClass } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
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

  ngOnInit(): void {
    this.loadInitiatives();
  }

  loadInitiatives(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.initiativesApi.getAll().subscribe({
      next: (initiatives) => {
        this.initiatives.set(initiatives);
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
