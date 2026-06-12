import { Component, computed, inject, signal } from '@angular/core';
import { ReactiveFormsModule, Validators, NonNullableFormBuilder } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { APP_ENVIRONMENT } from '../../../../core/constants/app-environment.constants';
import { AUTH_API_ENDPOINTS } from '../../../../core/constants/api.constants';
import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translations';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { isValidReturnUrl, normalizeReturnUrl } from '../../../../core/utils/return-url.util';
import { GithubButton } from '../../components/github-button/github-button';
import { GoogleButton } from '../../components/google-button/google-button';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, GoogleButton, GithubButton],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class RegisterComponent {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly i18n = inject(I18nService);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly loginPath = computed(() => `/${APP_ROUTES.login}`);
  readonly returnUrlParam = computed(() => this.route.snapshot.queryParamMap.get('returnUrl'));
  readonly loginQueryParams = computed(() => {
    const returnUrl = this.returnUrlParam();
    return isValidReturnUrl(returnUrl) ? { returnUrl } : null;
  });
  readonly googleClientId = APP_ENVIRONMENT.googleClientId;
  readonly githubLoginUrl = AUTH_API_ENDPOINTS.githubLogin;
  readonly returnUrl = computed(() => normalizeReturnUrl(this.returnUrlParam()));
  readonly hasReturnUrl = computed(() => isValidReturnUrl(this.returnUrlParam()));

  readonly form = this.formBuilder.group({
    displayName: ['', [Validators.required, Validators.maxLength(80)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const request = this.form.getRawValue();

    if (request.password !== request.confirmPassword) {
      this.errorMessage.set(this.t('auth.register.passwordMismatch'));
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.authService.register(request).subscribe({
      next: () => void this.router.navigateByUrl(this.returnUrl()),
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, this.t('auth.register.errorFallback')));
        this.isSubmitting.set(false);
      },
    });
  }

  continueWithGoogle(idToken: string): void {
    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.authService.googleLogin({ idToken }).subscribe({
      next: () => {
        void this.router.navigateByUrl(this.returnUrl());
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

  showRequiredError(controlName: 'displayName' | 'email' | 'password' | 'confirmPassword'): boolean {
    const control = this.form.controls[controlName];
    return control.touched && control.invalid;
  }

  t(key: TranslationKey, params?: Record<string, string | number | boolean | null | undefined>): string {
    return this.i18n.t(key, params);
  }
}
