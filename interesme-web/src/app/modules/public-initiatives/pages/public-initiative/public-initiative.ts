import { NgClass } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translations';
import { getApiErrorMessage, normalizeApiError } from '../../../../core/utils/api-error.util';
import { AuthService } from '../../../auth/services/auth.service';
import { InitiativeGoalType, PublicInitiative } from '../../../initiatives/models';
import { PublicInitiativesApiService } from '../../services/public-initiatives-api.service';

@Component({
  selector: 'app-public-initiative-page',
  standalone: true,
  imports: [NgClass, RouterLink],
  templateUrl: './public-initiative.html',
  styleUrl: './public-initiative.scss',
})
export class PublicInitiativePage implements OnInit {
  private readonly initiativeApi = inject(PublicInitiativesApiService);
  private readonly authService = inject(AuthService);
  private readonly i18n = inject(I18nService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly initiative = signal<PublicInitiative | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly isNotFound = signal(false);
  readonly copyState = signal<'idle' | 'copied' | 'failed'>('idle');

  readonly goalLabel = computed(() => {
    const initiative = this.initiative();
    return initiative ? this.getGoalLabel(initiative.goalType) : '';
  });

  readonly goalClass = computed(() => {
    const initiative = this.initiative();
    return initiative ? `goal-${this.getGoalKey(initiative.goalType)}` : '';
  });

  readonly applyPath = computed(() => {
    const initiative = this.initiative();
    return initiative ? `/${APP_ROUTES.initiatives}/${initiative.id}/apply` : `/${APP_ROUTES.login}`;
  });

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug');

    if (!slug) {
      this.isLoading.set(false);
      this.isNotFound.set(true);
      return;
    }

    this.loadInitiative(slug);
  }

  copyLink(): void {
    const link = globalThis.location?.href;

    if (!link) {
      this.copyState.set('failed');
      return;
    }

    if (globalThis.navigator?.clipboard) {
      void globalThis.navigator.clipboard
        .writeText(link)
        .then(() => this.showCopied())
        .catch(() => this.copyWithFallback(link));
      return;
    }

    this.copyWithFallback(link);
  }

  goToApply(): void {
    const returnUrl = this.applyPath();

    if (this.authService.isAuthenticated()) {
      void this.router.navigateByUrl(returnUrl);
      return;
    }

    void this.router.navigate([`/${APP_ROUTES.login}`], {
      queryParams: { returnUrl },
    });
  }

  t(key: TranslationKey, params?: Record<string, string | number | boolean | null | undefined>): string {
    return this.i18n.t(key, params);
  }

  private loadInitiative(slug: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.isNotFound.set(false);

    this.initiativeApi.getBySlug(slug).subscribe({
      next: (initiative) => {
        this.initiative.set(initiative);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        const fallbackMessage = this.t('publicInitiative.error.fallback');
        const apiError = normalizeApiError(error, fallbackMessage);
        this.isNotFound.set(apiError.status === 404);
        this.errorMessage.set(apiError.status === 404 ? null : getApiErrorMessage(error, fallbackMessage));
        this.isLoading.set(false);
      },
    });
  }

  private getGoalLabel(goalType: InitiativeGoalType): string {
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

  private getGoalKey(goalType: InitiativeGoalType): string {
    switch (goalType) {
      case InitiativeGoalType.Connect:
        return 'connect';
      case InitiativeGoalType.Learn:
        return 'learn';
      case InitiativeGoalType.Build:
        return 'build';
      case InitiativeGoalType.Play:
        return 'play';
      case InitiativeGoalType.Explore:
        return 'explore';
      default:
        return 'explore';
    }
  }

  private showCopied(): void {
    this.copyState.set('copied');
    window.setTimeout(() => this.copyState.set('idle'), 1800);
  }

  private copyWithFallback(link: string): void {
    try {
      const textarea = document.createElement('textarea');
      textarea.value = link;
      textarea.setAttribute('readonly', '');
      textarea.style.position = 'fixed';
      textarea.style.opacity = '0';
      document.body.appendChild(textarea);
      textarea.select();
      document.execCommand('copy');
      document.body.removeChild(textarea);
      this.showCopied();
    } catch {
      this.copyState.set('failed');
    }
  }
}
