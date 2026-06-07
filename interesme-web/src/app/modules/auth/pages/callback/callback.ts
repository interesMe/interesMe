import { Component, inject, OnInit, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
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

  readonly message = signal('Completing GitHub sign in...');
  readonly loginPath = `/${APP_ROUTES.login}`;

  ngOnInit(): void {
    const error = this.route.snapshot.queryParamMap.get('error');

    if (error) {
      this.message.set('GitHub sign in was cancelled or failed.');
      return;
    }

    const sessionCode = this.route.snapshot.queryParamMap.get('githubSessionCode');

    if (!sessionCode) {
      this.message.set('GitHub sign in did not return a session.');
      return;
    }

    this.authService.completeGithubSession({ sessionCode }).subscribe({
      next: () => {
        const returnUrl = this.readAndClearReturnUrl();
        void this.router.navigateByUrl(returnUrl);
      },
      error: (authError: unknown) => {
        this.message.set(this.readErrorMessage(authError, 'GitHub sign in failed.'));
      },
    });
  }

  private readAndClearReturnUrl(): string {
    if (typeof sessionStorage === 'undefined') {
      return `/${APP_ROUTES.profile}`;
    }

    const returnUrl = sessionStorage.getItem(GITHUB_RETURN_URL_KEY) ?? `/${APP_ROUTES.profile}`;
    sessionStorage.removeItem(GITHUB_RETURN_URL_KEY);

    return returnUrl;
  }

  private readErrorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && typeof error.error?.message === 'string') {
      return error.error.message;
    }

    return fallback;
  }
}
