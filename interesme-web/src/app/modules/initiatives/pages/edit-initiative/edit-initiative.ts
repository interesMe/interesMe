import { Component, inject, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translations';
import { getApiErrorMessage, normalizeApiError } from '../../../../core/utils/api-error.util';
import { InterestResponse } from '../../../profile/models';
import { ProfileApiService } from '../../../profile/services/profile-api.service';
import { Initiative, InitiativeGoalType, InitiativeVisibility } from '../../models';
import { InitiativesApiService } from '../../services/initiatives-api.service';

@Component({
  selector: 'app-edit-initiative-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <main class="placeholder-page">
      @if (initiative(); as item) {
        <a [routerLink]="detailsPath(item)">{{ t('initiative.common.details') }}</a>
      } @else {
        <a [routerLink]="listPath">{{ t('initiative.common.backToList') }}</a>
      }

      <h1>{{ t('initiative.edit.title') }}</h1>
      <p>{{ t('initiative.edit.description') }}</p>

      @if (isLoading()) {
        <p>{{ t('initiative.common.loading') }}</p>
      } @else if (isNotFound()) {
        <p>{{ t('initiative.common.notFound') }}</p>
      } @else {
        @if (initiative()?.slug) {
          <p><strong>{{ t('initiative.edit.publicLink') }}:</strong> {{ publicLink() }}</p>
        }

        @if (errorMessage()) {
          <p class="form-error">{{ errorMessage() }}</p>
        }

        <form class="initiative-form" [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <label>{{ t('initiative.common.title') }}<input type="text" formControlName="title" /></label>
          <label>{{ t('initiative.common.shortDescription') }}<textarea formControlName="shortDescription" rows="4"></textarea></label>

          <label>
            {{ t('initiative.common.goalType') }}
            <select formControlName="goalType">
              @for (goal of goals; track goal.value) {
                <option [ngValue]="goal.value">{{ goal.label }}</option>
              }
            </select>
          </label>

          <label>
            {{ t('initiative.common.visibility') }}
            <select formControlName="visibility">
              <option [ngValue]="visibility.Public">{{ t('initiative.common.public') }}</option>
              <option [ngValue]="visibility.Private">{{ t('initiative.common.private') }}</option>
            </select>
          </label>

          <label>{{ t('initiative.common.university') }}<input type="text" formControlName="university" /></label>
          <label>{{ t('initiative.common.teamSize') }}<input type="number" min="1" formControlName="teamSize" /></label>

          <fieldset>
            <legend>{{ t('initiative.common.interests') }}</legend>
            <div class="chips">
              @for (interest of interests(); track interest.id) {
                <label class="chip">
                  <input type="checkbox" [checked]="isInterestSelected(interest.id)" (change)="toggleInterest(interest.id)" />
                  {{ interest.name }}
                </label>
              }
            </div>
          </fieldset>

          <fieldset>
            <legend>{{ t('initiative.common.roles') }}</legend>
            <div class="role-row">
              <input #roleInput type="text" [placeholder]="t('initiative.common.rolePlaceholder')" />
              <button class="button button-secondary" type="button" (click)="addRole(roleInput.value); roleInput.value = ''">
                {{ t('initiative.common.addRole') }}
              </button>
            </div>
            <div class="chips">
              @for (role of roles(); track role) {
                <button class="chip" type="button" (click)="removeRole(role)">{{ role }} ×</button>
              }
            </div>
          </fieldset>

          <button class="button button-primary" type="submit" [disabled]="isSubmitting()">
            {{ isSubmitting() ? t('initiative.common.saving') : t('initiative.common.save') }}
          </button>
        </form>
      }
    </main>
  `,
  styles: [
    `
      .initiative-form, label, fieldset { display: grid; gap: 10px; }
      .initiative-form { margin-top: 20px; }
      input, textarea, select { border: 1px solid var(--im-border); border-radius: var(--im-radius-sm); padding: 10px; }
      fieldset { border: 1px solid var(--im-border); border-radius: var(--im-radius-md); padding: 14px; }
      .chips, .role-row { display: flex; flex-wrap: wrap; gap: 10px; }
      .chip { border: 1px solid var(--im-border); border-radius: 999px; padding: 6px 10px; background: var(--im-surface-soft); }
      .form-error { color: var(--im-connect); }
    `,
  ],
})
export class EditInitiativePage implements OnInit {
  private readonly initiativesApi = inject(InitiativesApiService);
  private readonly profileApi = inject(ProfileApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);

  readonly listPath = `/${APP_ROUTES.initiatives}`;
  readonly visibility = InitiativeVisibility;
  readonly initiative = signal<Initiative | null>(null);
  readonly interests = signal<InterestResponse[]>([]);
  readonly selectedInterestIds = signal<Set<string>>(new Set<string>());
  readonly roles = signal<string[]>([]);
  readonly isLoading = signal(true);
  readonly isNotFound = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.formBuilder.group({
    title: ['', [Validators.required, Validators.maxLength(140)]],
    shortDescription: ['', [Validators.required, Validators.maxLength(500)]],
    goalType: [InitiativeGoalType.Explore, [Validators.required]],
    visibility: [InitiativeVisibility.Public, [Validators.required]],
    university: [''],
    teamSize: this.formBuilder.control<number | null>(null),
  });

  get goals(): Array<{ value: InitiativeGoalType; label: string }> {
    return [
      { value: InitiativeGoalType.Connect, label: this.t('publicInitiative.goal.connect') },
      { value: InitiativeGoalType.Learn, label: this.t('publicInitiative.goal.learn') },
      { value: InitiativeGoalType.Build, label: this.t('publicInitiative.goal.build') },
      { value: InitiativeGoalType.Play, label: this.t('publicInitiative.goal.play') },
      { value: InitiativeGoalType.Explore, label: this.t('publicInitiative.goal.explore') },
    ];
  }

  ngOnInit(): void {
    this.loadPage();
  }

  submit(): void {
    const initiative = this.initiative();
    if (!initiative || this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      this.errorMessage.set(this.t('initiative.common.required'));
      return;
    }

    const formValue = this.form.getRawValue();
    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.initiativesApi
      .update(initiative.id, {
        title: formValue.title,
        shortDescription: formValue.shortDescription,
        goalType: formValue.goalType,
        visibility: formValue.visibility,
        university: formValue.university || null,
        teamSize: formValue.teamSize,
        interestIds: [...this.selectedInterestIds()],
        roles: this.roles(),
      })
      .subscribe({
        next: (updatedInitiative) => void this.router.navigateByUrl(`/${APP_ROUTES.initiatives}/${updatedInitiative.id}`),
        error: (error: unknown) => {
          this.errorMessage.set(getApiErrorMessage(error, this.t('initiative.edit.errorFallback')));
          this.isSubmitting.set(false);
        },
      });
  }

  addRole(role: string): void {
    const normalizedRole = role.trim();
    if (!normalizedRole || this.roles().includes(normalizedRole)) {
      return;
    }
    this.roles.update((roles) => [...roles, normalizedRole]);
  }

  removeRole(role: string): void {
    this.roles.update((roles) => roles.filter((currentRole) => currentRole !== role));
  }

  toggleInterest(interestId: string): void {
    this.selectedInterestIds.update((currentIds) => {
      const nextIds = new Set(currentIds);
      nextIds.has(interestId) ? nextIds.delete(interestId) : nextIds.add(interestId);
      return nextIds;
    });
  }

  isInterestSelected(interestId: string): boolean {
    return this.selectedInterestIds().has(interestId);
  }

  publicLink(): string {
    const slug = this.initiative()?.slug;
    return slug ? `${globalThis.location?.origin ?? ''}/i/${slug}` : '';
  }

  detailsPath(initiative: Initiative): string {
    return `/${APP_ROUTES.initiatives}/${initiative.id}`;
  }

  t(key: TranslationKey, params?: Record<string, string | number | boolean | null | undefined>): string {
    return this.i18n.t(key, params);
  }

  private loadPage(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.isLoading.set(false);
      this.isNotFound.set(true);
      return;
    }

    forkJoin({
      initiative: this.initiativesApi.getById(id),
      interests: this.profileApi.getInterests(),
    }).subscribe({
      next: ({ initiative, interests }) => {
        this.initiative.set(initiative);
        this.interests.set(interests);
        this.selectedInterestIds.set(new Set(initiative.interests.map((interest) => interest.id)));
        this.roles.set(initiative.roles.map((role) => role.name));
        this.form.patchValue({
          title: initiative.title,
          shortDescription: initiative.shortDescription,
          goalType: initiative.goalType,
          visibility: initiative.visibility,
          university: initiative.university ?? '',
          teamSize: initiative.teamSize,
        });
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
