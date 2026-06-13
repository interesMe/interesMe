import { Injectable } from '@angular/core';

import { APP_ROUTES } from '../constants/routes.constants';
import { isValidReturnUrl, normalizeReturnUrl } from '../utils/return-url.util';

const ONBOARDING_COMPLETED_KEY = 'interesme_onboarding_completed';

@Injectable({
  providedIn: 'root',
})
export class OnboardingRedirectService {
  getPostAuthRedirectUrl(returnUrl?: string | null): string {
    if (!this.isOnboardingCompleted()) {
      return `/${APP_ROUTES.onboardingInterests}`;
    }

    return isValidReturnUrl(returnUrl) ? normalizeReturnUrl(returnUrl) : `/${APP_ROUTES.home}`;
  }

  completeOnboarding(): void {
    if (this.hasStorage()) {
      localStorage.setItem(ONBOARDING_COMPLETED_KEY, 'true');
    }
  }

  isOnboardingCompleted(): boolean {
    return this.hasStorage() && localStorage.getItem(ONBOARDING_COMPLETED_KEY) === 'true';
  }

  private hasStorage(): boolean {
    return typeof localStorage !== 'undefined';
  }
}
