import { Component, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ReactiveFormsModule, Validators, NonNullableFormBuilder } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { APP_ENVIRONMENT } from '../../../../core/constants/app-environment.constants';
import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { GoogleButton } from '../../components/google-button/google-button';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, GoogleButton],
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
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? `/${APP_ROUTES.profile}`;
        void this.router.navigateByUrl(returnUrl);
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.readErrorMessage(error, 'Unable to sign in with those credentials.'));
        this.isSubmitting.set(false);
      },
    });
  }

  continueWithGoogle(idToken: string): void {
    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.authService.googleLogin({ idToken }).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? `/${APP_ROUTES.profile}`;
        void this.router.navigateByUrl(returnUrl);
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.readErrorMessage(error, 'Google sign in failed.'));
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

  private readErrorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && typeof error.error?.message === 'string') {
      return error.error.message;
    }

    return fallback;
  }
}
