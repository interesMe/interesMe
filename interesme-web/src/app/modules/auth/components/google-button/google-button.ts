import { Component, computed, input, output, signal } from '@angular/core';

interface GoogleCredentialResponse {
  credential?: string;
}

interface GooglePromptMomentNotification {
  isNotDisplayed: () => boolean;
  isSkippedMoment: () => boolean;
}

interface GoogleIdentityServices {
  accounts: {
    id: {
      initialize: (config: { client_id: string; callback: (response: GoogleCredentialResponse) => void }) => void;
      prompt: (callback?: (notification: GooglePromptMomentNotification) => void) => void;
    };
  };
}

type GoogleWindow = Window & { google?: GoogleIdentityServices };

@Component({
  selector: 'app-google-button',
  imports: [],
  templateUrl: './google-button.html',
  styleUrl: './google-button.scss',
})
export class GoogleButton {
  private static scriptPromise: Promise<GoogleIdentityServices> | null = null;

  readonly clientId = input('');
  readonly disabled = input(false);

  readonly idToken = output<string>();
  readonly error = output<string>();
  readonly loadingChange = output<boolean>();

  readonly isLoading = signal(false);
  readonly isDisabled = computed(() => this.disabled() || this.isLoading() || !this.clientId());

  continueWithGoogle(): void {
    if (this.isDisabled()) {
      if (!this.clientId()) {
        this.error.emit('Google sign in is not configured yet.');
      }

      return;
    }

    this.setLoading(true);

    this.loadGoogleIdentityScript()
      .then((google) => {
        google.accounts.id.initialize({
          client_id: this.clientId(),
          callback: (response) => this.handleCredential(response),
        });

        google.accounts.id.prompt((notification) => {
          if (notification.isNotDisplayed() || notification.isSkippedMoment()) {
            this.setLoading(false);
          }
        });
      })
      .catch(() => {
        this.error.emit('Google sign in could not be loaded.');
        this.setLoading(false);
      });
  }

  private handleCredential(response: GoogleCredentialResponse): void {
    this.setLoading(false);

    if (!response.credential) {
      this.error.emit('Google sign in did not return a credential.');
      return;
    }

    this.idToken.emit(response.credential);
  }

  private loadGoogleIdentityScript(): Promise<GoogleIdentityServices> {
    const googleWindow = window as GoogleWindow;

    if (googleWindow.google) {
      return Promise.resolve(googleWindow.google);
    }

    GoogleButton.scriptPromise ??= new Promise((resolve, reject) => {
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

    return GoogleButton.scriptPromise;
  }

  private setLoading(isLoading: boolean): void {
    this.isLoading.set(isLoading);
    this.loadingChange.emit(isLoading);
  }
}
