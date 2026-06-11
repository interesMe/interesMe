import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
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
        this.message.set(getApiErrorMessage(authError, 'GitHub sign in failed.'));
      },
    });
  }

  private readAndClearReturnUrl(): string {
    if (typeof sessionStorage === 'undefined') {
      return `/${APP_ROUTES.initiatives}`;
    }

    const returnUrl = sessionStorage.getItem(GITHUB_RETURN_URL_KEY) ?? `/${APP_ROUTES.initiatives}`;
    sessionStorage.removeItem(GITHUB_RETURN_URL_KEY);

    return returnUrl;
  }
}
