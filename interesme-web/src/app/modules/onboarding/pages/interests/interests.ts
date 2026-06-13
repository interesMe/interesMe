import { Component, inject, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { Router } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { OnboardingRedirectService } from '../../../../core/services/onboarding-redirect.service';

@Component({
  selector: 'app-onboarding-interests-page',
  standalone: true,
  imports: [NgClass],
  templateUrl: './interests.html',
  styleUrl: './interests.scss',
})
export class OnboardingInterestsPage {
  private readonly onboardingRedirect = inject(OnboardingRedirectService);
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
    this.onboardingRedirect.completeOnboarding();
    void this.router.navigateByUrl(`/${APP_ROUTES.home}`);
  }
}
