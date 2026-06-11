import { Component, computed, inject, signal } from '@angular/core';
import { ReactiveFormsModule, Validators, NonNullableFormBuilder } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { APP_ENVIRONMENT } from '../../../../core/constants/app-environment.constants';
import { AUTH_API_ENDPOINTS } from '../../../../core/constants/api.constants';
import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
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

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly registerPath = computed(() => `/${APP_ROUTES.register}`);
  readonly googleClientId = APP_ENVIRONMENT.googleClientId;
  readonly githubLoginUrl = AUTH_API_ENDPOINTS.githubLogin;
  readonly returnUrl = computed(() => this.route.snapshot.queryParamMap.get('returnUrl') ?? `/${APP_ROUTES.initiatives}`);

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
        void this.router.navigateByUrl(this.returnUrl());
      },
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to sign in with those credentials.'));
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
        this.errorMessage.set(getApiErrorMessage(error, 'Google sign in failed.'));
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
}
