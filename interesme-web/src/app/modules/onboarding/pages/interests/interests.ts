import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { Router } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { InterestResponse } from '../../../profile/models';
import { ProfileApiService } from '../../../profile/services/profile-api.service';

/**
 * Onboarding step: the user picks interests from the real catalog (GET /api/interests)
 * and the selection is persisted to the profile (PUT /api/profile/me/interests).
 */
@Component({
  selector: 'app-onboarding-interests-page',
  standalone: true,
  imports: [NgClass],
  templateUrl: './interests.html',
  styleUrl: './interests.scss',
})
export class OnboardingInterestsPage implements OnInit {
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);
  private readonly profileApi = inject(ProfileApiService);

  readonly interests = signal<InterestResponse[]>([]);
  readonly selectedInterestIds = signal<string[]>([]);
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly loadFailed = signal(false);

  readonly text = computed(() => {
    this.i18n.currentLocale();

    return {
      eyebrow: this.i18n.t('onboarding.interests.eyebrow'),
      title: this.i18n.t('onboarding.interests.title'),
      description: this.i18n.t('onboarding.interests.description'),
      helper: this.i18n.t('onboarding.interests.helper'),
      aria: this.i18n.t('onboarding.interests.aria'),
      continue: this.i18n.t('onboarding.interests.continue'),
      loading: this.i18n.t('onboarding.interests.loading'),
      saving: this.i18n.t('onboarding.interests.saving'),
      retry: this.i18n.t('onboarding.interests.retry'),
    };
  });

  ngOnInit(): void {
    this.loadInterests();
  }

  loadInterests(): void {
    this.isLoading.set(true);
    this.loadFailed.set(false);
    this.errorMessage.set(null);

    this.profileApi.getInterests().subscribe({
      next: (interests) => {
        this.interests.set(interests);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, this.i18n.t('onboarding.interests.loadError')));
        this.loadFailed.set(true);
        this.isLoading.set(false);
      },
    });
  }

  toggleInterest(interestId: string): void {
    this.selectedInterestIds.update((selected) =>
      selected.includes(interestId) ? selected.filter((id) => id !== interestId) : [...selected, interestId],
    );
  }

  isSelected(interestId: string): boolean {
    return this.selectedInterestIds().includes(interestId);
  }

  continue(): void {
    const interestIds = this.selectedInterestIds();

    if (interestIds.length === 0 || this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    this.profileApi.saveMyInterests({ interestIds }).subscribe({
      next: () => {
        this.isSaving.set(false);
        void this.router.navigateByUrl(`/${APP_ROUTES.onboardingLoading}`);
      },
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, this.i18n.t('onboarding.interests.saveError')));
        this.isSaving.set(false);
      },
    });
  }
}
