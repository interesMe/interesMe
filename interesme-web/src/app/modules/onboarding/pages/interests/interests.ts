import { Component, computed, inject, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { Router } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translations';

const ONBOARDING_INTERESTS_KEY = 'interesme_onboarding_interests';

type InterestOption = {
  value: string;
  labelKey: TranslationKey;
};

@Component({
  selector: 'app-onboarding-interests-page',
  standalone: true,
  imports: [NgClass],
  templateUrl: './interests.html',
  styleUrl: './interests.scss',
})
export class OnboardingInterestsPage {
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);

  private readonly interestOptions: InterestOption[] = [
    { value: 'Startup', labelKey: 'onboarding.interests.option.startup' },
    { value: 'Music', labelKey: 'onboarding.interests.option.music' },
    { value: 'GameDev', labelKey: 'onboarding.interests.option.gameDev' },
    { value: 'Design', labelKey: 'onboarding.interests.option.design' },
    { value: 'Sport', labelKey: 'onboarding.interests.option.sport' },
    { value: 'Volunteering', labelKey: 'onboarding.interests.option.volunteering' },
    { value: 'Science', labelKey: 'onboarding.interests.option.science' },
    { value: 'Photography', labelKey: 'onboarding.interests.option.photography' },
    { value: 'Travel', labelKey: 'onboarding.interests.option.travel' },
    { value: 'Education', labelKey: 'onboarding.interests.option.education' },
  ];

  readonly selectedInterests = signal<string[]>([]);
  readonly text = computed(() => {
    this.i18n.currentLocale();

    return {
      eyebrow: this.i18n.t('onboarding.interests.eyebrow'),
      title: this.i18n.t('onboarding.interests.title'),
      description: this.i18n.t('onboarding.interests.description'),
      helper: this.i18n.t('onboarding.interests.helper'),
      aria: this.i18n.t('onboarding.interests.aria'),
      continue: this.i18n.t('onboarding.interests.continue'),
    };
  });

  readonly interests = computed(() => {
    this.i18n.currentLocale();

    return this.interestOptions.map((interest) => ({
      value: interest.value,
      label: this.i18n.t(interest.labelKey),
    }));
  });

  toggleInterest(interest: string): void {
    const selectedInterests = this.selectedInterests();

    this.selectedInterests.set(
      selectedInterests.includes(interest)
        ? selectedInterests.filter((selectedInterest) => selectedInterest !== interest)
        : [...selectedInterests, interest],
    );
  }

  isSelected(interest: string): boolean {
    return this.selectedInterests().includes(interest);
  }

  continue(): void {
    const selectedInterests = this.selectedInterests();

    if (selectedInterests.length === 0) {
      return;
    }

    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(ONBOARDING_INTERESTS_KEY, JSON.stringify(selectedInterests));
    }

    void this.router.navigateByUrl(`/${APP_ROUTES.onboardingLoading}`);
  }
}
