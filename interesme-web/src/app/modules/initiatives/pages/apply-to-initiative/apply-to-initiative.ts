import { Component, inject, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translations';
import { getApiErrorMessage, normalizeApiError } from '../../../../core/utils/api-error.util';
import { Initiative, InitiativeJoinRequestStatus } from '../../models';
import { InitiativesApiService } from '../../services/initiatives-api.service';

@Component({
  selector: 'app-apply-to-initiative-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <main class="placeholder-page">
      @if (initiative(); as item) {
        <a [routerLink]="detailsPath(item)">{{ t('initiative.common.details') }}</a>
      } @else {
        <a [routerLink]="listPath">{{ t('initiative.common.backToList') }}</a>
      }

      <h1>{{ t('initiative.apply.title') }}</h1>

      @if (isLoading()) {
        <p>{{ t('initiative.common.loading') }}</p>
      } @else if (isNotFound()) {
        <p>{{ t('initiative.common.notFound') }}</p>
      } @else if (successMessage()) {
        <p class="status-box is-success" role="status">{{ successMessage() }}</p>
        @if (initiative(); as item) {
          <a class="button button-secondary" [routerLink]="detailsPath(item)">{{ t('initiative.common.details') }}</a>
        }
      } @else if (activeRequestMessage()) {
        <p class="status-box" role="status">{{ activeRequestMessage() }}</p>
        @if (initiative(); as item) {
          <a class="button button-secondary" [routerLink]="detailsPath(item)">{{ t('initiative.common.details') }}</a>
        }
      } @else if (initiative(); as item) {
        <h2>{{ item.title }}</h2>
        <p>{{ item.shortDescription }}</p>

        @if (errorMessage()) {
          <p class="form-error">{{ errorMessage() }}</p>
        }

        <form class="initiative-form" [formGroup]="form" (ngSubmit)="submit(item)" novalidate>
          <label>
            {{ t('initiative.apply.role') }}
            <select formControlName="roleId">
              <option [ngValue]="null">{{ t('initiative.apply.noRole') }}</option>
              @for (role of item.roles; track role.id) {
                <option [ngValue]="role.id">{{ role.name }}</option>
              }
            </select>
          </label>
          <label>{{ t('initiative.apply.message') }}<textarea formControlName="message" rows="3" maxlength="500"></textarea></label>
          <label>{{ t('initiative.apply.motivation') }}<textarea formControlName="motivation" rows="3" maxlength="1000"></textarea></label>
          <label>{{ t('initiative.apply.experience') }}<textarea formControlName="experience" rows="3" maxlength="1000"></textarea></label>
          <label>{{ t('initiative.apply.contribution') }}<textarea formControlName="contribution" rows="3" maxlength="1000"></textarea></label>
          <label>{{ t('initiative.apply.availability') }}<textarea formControlName="availability" rows="3" maxlength="1000"></textarea></label>

          <button class="button button-primary" type="submit" [disabled]="isSubmitting()">
            {{ isSubmitting() ? t('initiative.common.submitting') : t('initiative.common.submit') }}
          </button>
        </form>
      }
    </main>
  `,
  styles: [
    `
      .initiative-form, label { display: grid; gap: 10px; }
      .initiative-form { margin-top: 20px; }
      textarea, select { border: 1px solid var(--im-border); border-radius: var(--im-radius-sm); padding: 10px; }
      .form-error { color: var(--im-error-text, #b91c1c); }
      .status-box { border-radius: 12px; padding: 12px; background: rgba(245, 158, 11, 0.14); color: #b45309; font-weight: 600; }
      .status-box.is-success { background: rgba(20, 184, 166, 0.14); color: var(--im-primary-hover); }
    `,
  ],
})
export class ApplyToInitiativePage implements OnInit {
  private readonly initiativesApi = inject(InitiativesApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly i18n = inject(I18nService);

  readonly listPath = `/${APP_ROUTES.initiatives}`;
  readonly initiative = signal<Initiative | null>(null);
  readonly isLoading = signal(true);
  readonly isNotFound = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  /** Shown instead of the form when the user already has a pending/accepted request or applying is closed. */
  readonly activeRequestMessage = signal<string | null>(null);

  readonly form = this.formBuilder.group({
    roleId: this.formBuilder.control<string | null>(null),
    message: ['', [Validators.maxLength(500)]],
    motivation: ['', [Validators.maxLength(1000)]],
    experience: ['', [Validators.maxLength(1000)]],
    contribution: ['', [Validators.maxLength(1000)]],
    availability: ['', [Validators.maxLength(1000)]],
  });

  ngOnInit(): void {
    this.loadInitiative();
  }

  submit(initiative: Initiative): void {
    if (this.isSubmitting()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage.set(this.t('initiative.apply.tooLong'));
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const formValue = this.form.getRawValue();
    this.initiativesApi
      .createJoinRequest(initiative.id, {
        roleId: formValue.roleId,
        message: formValue.message.trim() || null,
        motivation: formValue.motivation.trim() || null,
        experience: formValue.experience.trim() || null,
        contribution: formValue.contribution.trim() || null,
        availability: formValue.availability.trim() || null,
      })
      .subscribe({
        next: () => {
          this.successMessage.set(this.t('initiative.apply.success'));
          this.isSubmitting.set(false);
        },
        error: (error: unknown) => {
          const apiError = normalizeApiError(error, this.t('initiative.apply.errorFallback'));
          this.isSubmitting.set(false);

          switch (apiError.code) {
            case 'initiative.apply.already_exists':
              this.activeRequestMessage.set(this.t('initiative.myRequest.pending'));
              return;
            case 'initiative.apply.already_member':
              this.activeRequestMessage.set(this.t('initiative.myRequest.accepted'));
              return;
            case 'initiative.apply.closed':
              this.activeRequestMessage.set(this.t('initiative.details.closed'));
              return;
            default:
              this.errorMessage.set(apiError.message);
          }
        },
      });
  }

  detailsPath(initiative: Initiative): string {
    return `/${APP_ROUTES.initiatives}/${initiative.id}`;
  }

  t(key: TranslationKey, params?: Record<string, string | number | boolean | null | undefined>): string {
    return this.i18n.t(key, params);
  }

  private loadInitiative(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.isLoading.set(false);
      this.isNotFound.set(true);
      return;
    }

    this.initiativesApi.getById(id).subscribe({
      next: (initiative) => {
        this.initiative.set(initiative);
        const status = initiative.myJoinRequest?.status;
        if (status === InitiativeJoinRequestStatus.Pending) {
          this.activeRequestMessage.set(this.t('initiative.myRequest.pending'));
        } else if (status === InitiativeJoinRequestStatus.Accepted) {
          this.activeRequestMessage.set(this.t('initiative.myRequest.accepted'));
        }
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        const apiError = normalizeApiError(error, this.t('initiative.common.loadFallback'));
        this.isNotFound.set(apiError.status === 404);
        this.errorMessage.set(apiError.status === 404 ? null : getApiErrorMessage(error, this.t('initiative.common.loadFallback')));
        this.isLoading.set(false);
      },
    });
  }
}
