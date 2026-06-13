import { Component, inject, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { Router } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';

const ONBOARDING_INTERESTS_KEY = 'interesme_onboarding_interests';

@Component({
  selector: 'app-onboarding-interests-page',
  standalone: true,
  imports: [NgClass],
  templateUrl: './interests.html',
  styleUrl: './interests.scss',
})
export class OnboardingInterestsPage {
  private readonly router = inject(Router);

  readonly interests = [
    'Startup',
    'Music',
    'GameDev',
    'Design',
    'Sport',
    'Volunteering',
    'Science',
    'Photography',
    'Travel',
    'Education',
  ];

  readonly selectedInterests = signal<string[]>([]);
  readonly helperText = 'Choose at least one interest to continue.';

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
