import { Component, computed, inject, signal } from '@angular/core';
import { ReactiveFormsModule, Validators, NonNullableFormBuilder } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class RegisterComponent {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly loginPath = computed(() => `/${APP_ROUTES.login}`);
  readonly returnUrl = computed(() => this.route.snapshot.queryParamMap.get('returnUrl') ?? `/${APP_ROUTES.initiatives}`);

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
      this.errorMessage.set('Passwords do not match.');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.authService.register(request).subscribe({
      next: () => void this.router.navigateByUrl(this.returnUrl()),
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, 'Unable to create your account right now.'));
        this.isSubmitting.set(false);
      },
    });
  }
}
