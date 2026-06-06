import { Component, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ReactiveFormsModule, Validators, NonNullableFormBuilder } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { APP_ENVIRONMENT } from '../../../../core/constants/app-environment.constants';
import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { AuthService } from '../../services/auth.service';

interface GoogleCredentialResponse {
  credential?: string;
}

interface GoogleIdentityServices {
  accounts: {
    id: {
      initialize: (config: { client_id: string; callback: (response: GoogleCredentialResponse) => void }) => void;
      prompt: () => void;
    };
  };
}

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
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
  readonly isGoogleConfigured = Boolean(APP_ENVIRONMENT.googleClientId);

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

  continueWithGoogle(): void {
    if (!APP_ENVIRONMENT.googleClientId) {
      this.errorMessage.set('Google sign in is not configured yet.');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.loadGoogleIdentityScript()
      .then((google) => {
        google.accounts.id.initialize({
          client_id: APP_ENVIRONMENT.googleClientId,
          callback: (response) => this.handleGoogleCredential(response),
        });
        google.accounts.id.prompt();
      })
      .catch(() => {
        this.errorMessage.set('Google sign in could not be loaded.');
        this.isSubmitting.set(false);
      });
  }

  private handleGoogleCredential(response: GoogleCredentialResponse): void {
    if (!response.credential) {
      this.errorMessage.set('Google sign in did not return a credential.');
      this.isSubmitting.set(false);
      return;
    }

    this.authService.googleLogin({ idToken: response.credential }).subscribe({
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

  private loadGoogleIdentityScript(): Promise<GoogleIdentityServices> {
    const googleWindow = window as Window & { google?: GoogleIdentityServices };

    if (googleWindow.google) {
      return Promise.resolve(googleWindow.google);
    }

    return new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.onload = () => {
        if (googleWindow.google) {
          resolve(googleWindow.google);
        } else {
          reject(new Error('Google Identity Services was not available.'));
        }
      };
      script.onerror = () => reject(new Error('Google Identity Services failed to load.'));
      document.head.appendChild(script);
    });
  }

  private readErrorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && typeof error.error?.message === 'string') {
      return error.error.message;
    }

    return fallback;
  }
}
