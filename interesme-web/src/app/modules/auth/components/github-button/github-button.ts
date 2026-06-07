import { Component, computed, input, output, signal } from '@angular/core';

const GITHUB_RETURN_URL_KEY = 'interesme.github.returnUrl';

@Component({
  selector: 'app-github-button',
  imports: [],
  templateUrl: './github-button.html',
  styleUrl: './github-button.scss',
})
export class GithubButton {
  readonly authUrl = input('');
  readonly disabled = input(false);
  readonly returnUrl = input<string | null>(null);

  readonly error = output<string>();
  readonly loadingChange = output<boolean>();

  readonly isLoading = signal(false);
  readonly isDisabled = computed(() => this.disabled() || this.isLoading() || !this.authUrl());

  continueWithGithub(): void {
    if (this.isDisabled()) {
      if (!this.authUrl()) {
        this.error.emit('GitHub sign in is not configured yet.');
      }

      return;
    }

    this.setLoading(true);
    this.storeReturnUrl();
    window.location.assign(this.authUrl());
  }

  private storeReturnUrl(): void {
    const returnUrl = this.returnUrl();

    if (!returnUrl || typeof sessionStorage === 'undefined') {
      return;
    }

    sessionStorage.setItem(GITHUB_RETURN_URL_KEY, returnUrl);
  }

  private setLoading(isLoading: boolean): void {
    this.isLoading.set(isLoading);
    this.loadingChange.emit(isLoading);
  }
}

export { GITHUB_RETURN_URL_KEY };
