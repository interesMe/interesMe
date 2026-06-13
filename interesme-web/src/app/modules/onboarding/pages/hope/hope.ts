import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { OnboardingRedirectService } from '../../../../core/services/onboarding-redirect.service';

@Component({
  selector: 'app-onboarding-hope-page',
  standalone: true,
  templateUrl: './hope.html',
  styleUrl: './hope.scss',
})
export class OnboardingHopePage {
  private readonly onboardingRedirect = inject(OnboardingRedirectService);
  private readonly router = inject(Router);

  enterInteresMe(): void {
    this.onboardingRedirect.completeOnboarding();
    void this.router.navigateByUrl(`/${APP_ROUTES.home}`);
  }
}
