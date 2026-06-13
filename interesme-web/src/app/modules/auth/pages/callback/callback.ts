import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translations';
import { OnboardingRedirectService } from '../../../../core/services/onboarding-redirect.service';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { GITHUB_RETURN_URL_KEY } from '../../components/github-button/github-button';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-callback',
  imports: [RouterLink],
  templateUrl: './callback.html',
  styleUrl: './callback.scss',
})
export class CallbackComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);
  private readonly onboardingRedirect = inject(OnboardingRedirectService);

  readonly message = signal(this.t('auth.callback.loading'));
  readonly loginPath = `/${APP_ROUTES.login}`;

  ngOnInit(): void {
    const error = this.route.snapshot.queryParamMap.get('error');

    if (error) {
      this.message.set(this.t('auth.callback.cancelled'));
      return;
    }

    const sessionCode = this.route.snapshot.queryParamMap.get('githubSessionCode');

    if (!sessionCode) {
      this.message.set(this.t('auth.callback.missingSession'));
      return;
    }

    this.authService.completeGithubSession({ sessionCode }).subscribe({
      next: () => {
        const returnUrl = this.readAndClearReturnUrl();
        void this.router.navigateByUrl(this.onboardingRedirect.getPostAuthRedirectUrl(returnUrl));
      },
      error: (authError: unknown) => {
        this.message.set(getApiErrorMessage(authError, this.t('auth.callback.errorFallback')));
      },
    });
  }

  t(key: TranslationKey, params?: Record<string, string | number | boolean | null | undefined>): string {
    return this.i18n.t(key, params);
  }

  private readAndClearReturnUrl(): string | null {
    if (typeof sessionStorage === 'undefined') {
      return null;
    }

    const returnUrl = sessionStorage.getItem(GITHUB_RETURN_URL_KEY);
    sessionStorage.removeItem(GITHUB_RETURN_URL_KEY);

    return returnUrl;
  }
}
