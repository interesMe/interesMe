import { Component, computed, inject, signal } from '@angular/core';
import { ReactiveFormsModule, Validators, NonNullableFormBuilder } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { APP_ENVIRONMENT } from '../../../../core/constants/app-environment.constants';
import { AUTH_API_ENDPOINTS } from '../../../../core/constants/api.constants';
import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translations';
import { OnboardingRedirectService } from '../../../../core/services/onboarding-redirect.service';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { isValidReturnUrl, normalizeReturnUrl } from '../../../../core/utils/return-url.util';
import { GithubButton } from '../../components/github-button/github-button';
import { GoogleButton } from '../../components/google-button/google-button';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, GoogleButton, GithubButton],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly i18n = inject(I18nService);
  private readonly onboardingRedirect = inject(OnboardingRedirectService);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly registerPath = computed(() => `/${APP_ROUTES.register}`);
  readonly returnUrlParam = computed(() => this.route.snapshot.queryParamMap.get('returnUrl'));
  readonly registerQueryParams = computed(() => {
    const returnUrl = this.returnUrlParam();
    return isValidReturnUrl(returnUrl) ? { returnUrl } : null;
  });
  readonly googleClientId = APP_ENVIRONMENT.googleClientId;
  readonly githubLoginUrl = AUTH_API_ENDPOINTS.githubLogin;
  readonly returnUrl = computed(() => normalizeReturnUrl(this.returnUrlParam()));
  readonly hasReturnUrl = computed(() => isValidReturnUrl(this.returnUrlParam()));

  readonly form = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => {
        void this.router.navigateByUrl(this.onboardingRedirect.getPostAuthRedirectUrl(this.returnUrlParam()));
      },
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, this.t('auth.login.errorFallback')));
        this.isSubmitting.set(false);
      },
    });
  }

  continueWithGoogle(idToken: string): void {
    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.authService.googleLogin({ idToken }).subscribe({
      next: () => {
        void this.router.navigateByUrl(this.onboardingRedirect.getPostAuthRedirectUrl(this.returnUrlParam()));
      },
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, this.t('auth.login.googleErrorFallback')));
        this.isSubmitting.set(false);
      },
    });
  }

  setGoogleLoading(isLoading: boolean): void {
    this.isSubmitting.set(isLoading);
    this.errorMessage.set(null);
  }

  showGoogleError(message: string): void {
    this.errorMessage.set(message);
    this.isSubmitting.set(false);
  }

  setGithubLoading(isLoading: boolean): void {
    this.isSubmitting.set(isLoading);
    this.errorMessage.set(null);
  }

  showGithubError(message: string): void {
    this.errorMessage.set(message);
    this.isSubmitting.set(false);
  }

  showRequiredError(controlName: 'email' | 'password'): boolean {
    const control = this.form.controls[controlName];
    return control.touched && control.invalid;
  }

  t(key: TranslationKey, params?: Record<string, string | number | boolean | null | undefined>): string {
    return this.i18n.t(key, params);
  }
}
