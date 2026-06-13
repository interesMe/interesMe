import { Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
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
  private readonly i18n = inject(I18nService);

  readonly text = computed(() => {
    this.i18n.currentLocale();

    return {
      eyebrow: this.i18n.t('onboarding.hope.eyebrow'),
      title: this.i18n.t('onboarding.hope.title'),
      description: this.i18n.t('onboarding.hope.description'),
      start: this.i18n.t('onboarding.hope.start'),
      enter: this.i18n.t('onboarding.hope.enter'),
    };
  });

  enterInteresMe(): void {
    this.onboardingRedirect.completeOnboarding();
    void this.router.navigateByUrl(`/${APP_ROUTES.home}`);
  }
}
