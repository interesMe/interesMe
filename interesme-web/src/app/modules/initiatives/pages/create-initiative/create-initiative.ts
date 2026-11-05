import { Component, inject, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translations';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { InterestResponse } from '../../../profile/models';
import { ProfileApiService } from '../../../profile/services/profile-api.service';
import { InitiativeGoalType, InitiativeVisibility } from '../../models';
import { InitiativesApiService } from '../../services/initiatives-api.service';

@Component({
  selector: 'app-create-initiative-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <main class="create-initiative-page">
      <a class="back-link" [routerLink]="listPath">{{ t('initiative.common.backToList') }}</a>

      <header class="create-hero">
        <p class="eyebrow">{{ t('initiative.create.eyebrow') }}</p>
        <h1>{{ t('initiative.create.title') }}</h1>
        <p>{{ t('initiative.create.description') }}</p>
      </header>

      @if (errorMessage()) {
        <p class="form-error">{{ errorMessage() }}</p>
      }

      <form class="initiative-form" [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <section class="form-section">
          <div class="section-heading">
            <span class="section-number">1</span>
            <h2>{{ t('initiative.create.intent.title') }}</h2>
          </div>

          <div class="option-grid goal-grid" role="radiogroup" [attr.aria-label]="t('initiative.create.intent.title')">
            @for (goal of goals; track goal.value) {
              <button
                class="option-card goal-card"
                [class.is-selected]="form.controls.goalType.value === goal.value"
                [class.goal-connect]="goal.value === goalType.Connect"
                [class.goal-learn]="goal.value === goalType.Learn"
                [class.goal-build]="goal.value === goalType.Build"
                [class.goal-play]="goal.value === goalType.Play"
                [class.goal-explore]="goal.value === goalType.Explore"
                type="button"
                role="radio"
                [attr.aria-checked]="form.controls.goalType.value === goal.value"
                (click)="selectGoal(goal.value)"
              >
                <span class="option-title">{{ goal.label }}</span>
                <span class="option-copy">{{ goal.copy }}</span>
              </button>
            }
          </div>
        </section>

        <section class="form-section">
          <div class="section-heading">
            <span class="section-number">2</span>
            <h2>{{ t('initiative.create.describe.title') }}</h2>
          </div>

          <label class="field">
            <span>{{ t('initiative.create.describe.titleLabel') }}</span>
            <input
              type="text"
              formControlName="title"
              maxlength="120"
              [placeholder]="t('initiative.create.describe.titlePlaceholder')"
            />
            @if (fieldError('title'); as error) {
              <span class="field-error" role="alert">{{ error }}</span>
            }
          </label>

          <label class="field">
            <span>{{ t('initiative.create.describe.descriptionLabel') }}</span>
            <textarea
              formControlName="shortDescription"
              rows="5"
              maxlength="500"
              [placeholder]="t('initiative.create.describe.descriptionPlaceholder')"
            ></textarea>
            <small>{{ t('initiative.create.describe.descriptionHelper') }}</small>
            @if (fieldError('shortDescription'); as error) {
              <span class="field-error" role="alert">{{ error }}</span>
            }
          </label>
        </section>

        <section class="form-section">
          <div class="section-heading">
            <span class="section-number">3</span>
            <h2>{{ t('initiative.create.people.title') }}</h2>
          </div>

          <div class="field">
            <span>{{ t('initiative.create.people.rolesLabel') }}</span>
            <div class="role-row">
              <input
                #roleInput
                type="text"
                maxlength="80"
                [placeholder]="t('initiative.create.people.rolesPlaceholder')"
                (keydown.enter)="$event.preventDefault(); addRole(roleInput)"
              />
              <button class="button button-secondary" type="button" (click)="addRole(roleInput)">
                {{ t('initiative.common.addRole') }}
              </button>
            </div>
            <p class="examples">{{ t('initiative.create.people.rolesExamples') }}</p>
            @if (rolesError(); as error) {
              <span class="field-error" role="alert">{{ error }}</span>
            }
            @if (roles().length > 0) {
              <div class="chips" [attr.aria-label]="t('initiative.create.people.rolesLabel')">
                @for (role of roles(); track role) {
                  <button class="chip selected-chip" type="button" (click)="removeRole(role)">{{ role }} x</button>
                }
              </div>
            }
          </div>

          <fieldset class="field">
            <legend>{{ t('initiative.create.people.interestsLabel') }}</legend>
          @if (isLoadingInterests()) {
            <p>{{ t('initiative.common.loading') }}</p>
          }
          <div class="chips interest-chips">
            @for (interest of interests(); track interest.id) {
              <label class="chip selectable-chip" [class.is-selected]="isInterestSelected(interest.id)">
                <input
                  type="checkbox"
                  [checked]="isInterestSelected(interest.id)"
                  (change)="toggleInterest(interest.id)"
                />
                {{ interest.name }}
              </label>
            }
          </div>
          @if (interestsError(); as error) {
            <span class="field-error" role="alert">{{ error }}</span>
          }
          </fieldset>
        </section>

        <section class="form-section">
          <div class="section-heading">
            <span class="section-number">4</span>
            <h2>{{ t('initiative.create.context.title') }}</h2>
          </div>

          <div class="context-grid">
            <label class="field">
              <span>{{ t('initiative.create.context.universityLabel') }}</span>
              <input type="text" formControlName="university" maxlength="160" [placeholder]="t('initiative.create.context.universityPlaceholder')" />
            </label>

            <label class="field">
              <span>{{ t('initiative.create.context.teamSizeLabel') }}</span>
              <input type="number" min="1" max="100" formControlName="teamSize" [placeholder]="t('initiative.create.context.teamSizePlaceholder')" />
              @if (fieldError('teamSize'); as error) {
                <span class="field-error" role="alert">{{ error }}</span>
              }
            </label>
          </div>
        </section>

        <section class="form-section">
          <div class="section-heading">
            <span class="section-number">5</span>
            <h2>{{ t('initiative.create.visibility.title') }}</h2>
          </div>

          <div class="option-grid visibility-grid" role="radiogroup" [attr.aria-label]="t('initiative.create.visibility.title')">
            @for (option of visibilityOptions; track option.value) {
              <button
                class="option-card visibility-card"
                [class.is-selected]="form.controls.visibility.value === option.value"
                type="button"
                role="radio"
                [attr.aria-checked]="form.controls.visibility.value === option.value"
                (click)="selectVisibility(option.value)"
              >
                <span class="option-title">{{ option.label }}</span>
                <span class="option-copy">{{ option.copy }}</span>
              </button>
            }
          </div>
        </section>

        <footer class="form-actions">
          <a class="button button-secondary" [routerLink]="listPath">{{ t('initiative.create.cancel') }}</a>
          <button class="button button-primary" type="submit" [disabled]="isSubmitting()">
            {{ isSubmitting() ? t('initiative.common.submitting') : t('initiative.create.submit') }}
          </button>
        </footer>
      </form>
    </main>
  `,
  styles: [
    `
      .create-initiative-page {
        width: min(980px, 100%);
        margin: 0 auto;
        padding: 28px;
      }

      .back-link {
        display: inline-flex;
        margin-bottom: 22px;
        color: var(--im-muted);
        font-weight: 700;
        text-decoration: none;
      }

      .back-link:hover {
        color: var(--im-primary-hover);
      }

      .create-hero {
        display: grid;
        gap: 10px;
        margin-bottom: 26px;
      }

      .eyebrow {
        margin: 0;
        color: var(--im-primary-hover);
        font-size: 13px;
        font-weight: 900;
        letter-spacing: 0.08em;
        text-transform: uppercase;
      }

      .create-hero h1 {
        max-width: 720px;
        margin: 0;
        color: var(--im-text);
        font-size: clamp(34px, 7vw, 58px);
        line-height: 1;
      }

      .create-hero p {
        max-width: 620px;
        margin: 0;
        color: var(--im-muted);
        font-size: 18px;
        line-height: 1.55;
      }

      .initiative-form {
        display: grid;
        gap: 18px;
      }

      .form-section {
        display: grid;
        gap: 18px;
        padding: 22px;
        border: 1px solid var(--im-border);
        border-radius: var(--im-radius-lg);
        background: rgba(255, 255, 255, 0.96);
        box-shadow: var(--im-shadow-card);
      }

      .section-heading {
        display: flex;
        align-items: center;
        gap: 12px;
      }

      .section-number {
        display: grid;
        flex: 0 0 auto;
        place-items: center;
        width: 34px;
        height: 34px;
        border-radius: 12px;
        background: rgba(20, 184, 166, 0.12);
        color: var(--im-primary-hover);
        font-weight: 900;
      }

      h2 {
        margin: 0;
        font-size: 22px;
        line-height: 1.2;
      }

      .option-grid {
        display: grid;
        gap: 12px;
      }

      .goal-grid {
        grid-template-columns: repeat(5, minmax(0, 1fr));
      }

      .visibility-grid,
      .context-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }

      .option-card {
        display: grid;
        gap: 8px;
        min-height: 128px;
        border: 1px solid var(--im-border);
        border-radius: var(--im-radius-md);
        padding: 16px;
        background: var(--im-surface);
        color: var(--im-text);
        cursor: pointer;
        font: inherit;
        text-align: left;
      }

      .option-card:hover,
      .option-card.is-selected {
        border-color: var(--option-color, var(--im-primary));
        box-shadow: 0 10px 24px rgba(17, 17, 17, 0.08);
        transform: translateY(-1px);
      }

      .option-card.is-selected {
        background: color-mix(in srgb, var(--option-color, var(--im-primary)) 10%, white);
      }

      .goal-connect { --option-color: var(--im-connect); }
      .goal-learn { --option-color: var(--im-learn); }
      .goal-build { --option-color: var(--im-build); }
      .goal-play { --option-color: var(--im-play); }
      .goal-explore { --option-color: var(--im-explore); }

      .option-title {
        color: var(--option-color, var(--im-primary-hover));
        font-size: 18px;
        font-weight: 900;
      }

      .option-copy {
        color: var(--im-muted);
        font-size: 14px;
        line-height: 1.45;
      }

      .field {
        display: grid;
        gap: 8px;
        min-width: 0;
        margin: 0;
        border: 0;
        padding: 0;
      }

      .field > span,
      legend {
        color: var(--im-text);
        font-weight: 900;
      }

      input,
      textarea {
        width: 100%;
        border: 1px solid var(--im-border);
        border-radius: var(--im-radius-md);
        padding: 13px 14px;
        background: var(--im-surface);
        color: var(--im-text);
        font: inherit;
      }

      textarea {
        resize: vertical;
      }

      input:focus,
      textarea:focus {
        border-color: var(--im-primary);
        box-shadow: 0 0 0 3px rgba(20, 184, 166, 0.14);
        outline: none;
      }

      small,
      .examples {
        margin: 0;
        color: var(--im-muted);
        line-height: 1.45;
      }

      .role-row {
        display: grid;
        grid-template-columns: minmax(0, 1fr) auto;
        gap: 10px;
      }

      .chips {
        display: flex;
        flex-wrap: wrap;
        gap: 10px;
      }

      .chip {
        display: inline-flex;
        align-items: center;
        min-height: 38px;
        border: 1px solid var(--im-border);
        border-radius: 999px;
        padding: 8px 12px;
        background: var(--im-surface-soft);
        color: var(--im-text);
        font: inherit;
        font-weight: 700;
      }

      .selectable-chip {
        position: relative;
        cursor: pointer;
      }

      /* Visually hidden checkbox: it must not inherit the generic full-width input size
         (previously caused ~1400px of horizontal page overflow). */
      .selectable-chip input {
        position: absolute;
        top: 0;
        left: 0;
        width: 1px;
        height: 1px;
        margin: 0;
        padding: 0;
        opacity: 0;
        pointer-events: none;
      }

      .selectable-chip.is-selected,
      .selected-chip {
        border-color: var(--im-primary);
        background: rgba(20, 184, 166, 0.12);
        color: var(--im-primary-hover);
      }

      .visibility-card {
        --option-color: var(--im-primary);
      }

      .form-actions {
        display: flex;
        justify-content: flex-end;
        gap: 12px;
        padding-top: 2px;
      }

      .form-error,
      .field-error {
        color: var(--im-connect);
        font-weight: 800;
      }

      .form-error {
        margin: 0 0 16px;
      }

      .field-error {
        font-size: 14px;
      }

      @media (max-width: 860px) {
        .goal-grid {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
      }

      @media (max-width: 640px) {
        .create-initiative-page {
          padding: 18px;
        }

        .form-section {
          padding: 18px;
        }

        .goal-grid,
        .visibility-grid,
        .context-grid,
        .role-row {
          grid-template-columns: 1fr;
        }

        .option-card {
          min-height: auto;
        }

        .form-actions {
          display: grid;
        }

        .form-actions .button,
        .role-row .button {
          width: 100%;
        }
      }
    `,
  ],
})
export class CreateInitiativePage implements OnInit {
  private readonly initiativesApi = inject(InitiativesApiService);
  private readonly profileApi = inject(ProfileApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);

  readonly listPath = `/${APP_ROUTES.initiatives}`;
  readonly visibility = InitiativeVisibility;
  readonly goalType = InitiativeGoalType;
  readonly interests = signal<InterestResponse[]>([]);
  readonly selectedInterestIds = signal<Set<string>>(new Set<string>());
  readonly roles = signal<string[]>([]);
  readonly isLoadingInterests = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly rolesError = signal<string | null>(null);
  readonly interestsError = signal<string | null>(null);

  static readonly maxRoles = 10;
  static readonly maxRoleLength = 80;
  static readonly maxInterests = 10;

  readonly form = this.formBuilder.group({
    // Limits mirror the backend validator (InitiativeRequestValidator).
    title: ['', [Validators.required, Validators.maxLength(120), Validators.pattern(/\S/)]],
    shortDescription: ['', [Validators.required, Validators.maxLength(500), Validators.pattern(/\S/)]],
    goalType: [InitiativeGoalType.Explore, [Validators.required]],
    visibility: [InitiativeVisibility.Public, [Validators.required]],
    university: ['', [Validators.maxLength(160)]],
    teamSize: this.formBuilder.control<number | null>(null, [Validators.min(1), Validators.max(100)]),
  });

  get goals(): Array<{ value: InitiativeGoalType; label: string; copy: string }> {
    return [
      {
        value: InitiativeGoalType.Connect,
        label: this.t('publicInitiative.goal.connect'),
        copy: this.t('initiative.create.intent.connectCopy'),
      },
      {
        value: InitiativeGoalType.Learn,
        label: this.t('publicInitiative.goal.learn'),
        copy: this.t('initiative.create.intent.learnCopy'),
      },
      {
        value: InitiativeGoalType.Build,
        label: this.t('publicInitiative.goal.build'),
        copy: this.t('initiative.create.intent.buildCopy'),
      },
      {
        value: InitiativeGoalType.Play,
        label: this.t('publicInitiative.goal.play'),
        copy: this.t('initiative.create.intent.playCopy'),
      },
      {
        value: InitiativeGoalType.Explore,
        label: this.t('publicInitiative.goal.explore'),
        copy: this.t('initiative.create.intent.exploreCopy'),
      },
    ];
  }

  get visibilityOptions(): Array<{ value: InitiativeVisibility; label: string; copy: string }> {
    return [
      {
        value: InitiativeVisibility.Public,
        label: this.t('initiative.common.public'),
        copy: this.t('initiative.create.visibility.publicCopy'),
      },
      {
        value: InitiativeVisibility.Private,
        label: this.t('initiative.common.private'),
        copy: this.t('initiative.create.visibility.privateCopy'),
      },
    ];
  }

  ngOnInit(): void {
    this.loadInterests();
  }

  submit(): void {
    if (this.isSubmitting()) {
      return;
    }

    const rolesMissing = this.roles().length === 0;
    const interestsMissing = this.selectedInterestIds().size === 0;
    this.rolesError.set(rolesMissing ? this.t('initiative.create.validation.rolesRequired') : null);
    this.interestsError.set(interestsMissing ? this.t('initiative.create.validation.interestsRequired') : null);

    if (this.form.invalid || rolesMissing || interestsMissing) {
      this.form.markAllAsTouched();
      this.errorMessage.set(this.t('initiative.common.required'));
      return;
    }

    const formValue = this.form.getRawValue();
    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.initiativesApi
      .create({
        title: formValue.title.trim(),
        shortDescription: formValue.shortDescription.trim(),
        goalType: formValue.goalType,
        visibility: formValue.visibility,
        university: formValue.university || null,
        teamSize: formValue.teamSize,
        interestIds: [...this.selectedInterestIds()],
        roles: this.roles(),
      })
      .subscribe({
        next: (initiative) => void this.router.navigateByUrl(`/${APP_ROUTES.initiatives}/${initiative.id}`),
        error: (error: unknown) => {
          this.errorMessage.set(getApiErrorMessage(error, this.t('initiative.create.errorFallback')));
          this.isSubmitting.set(false);
        },
      });
  }

  selectGoal(goalType: InitiativeGoalType): void {
    this.form.controls.goalType.setValue(goalType);
    this.form.controls.goalType.markAsDirty();
  }

  selectVisibility(visibility: InitiativeVisibility): void {
    this.form.controls.visibility.setValue(visibility);
    this.form.controls.visibility.markAsDirty();
  }

  addRole(input: HTMLInputElement): void {
    const normalizedRole = input.value.trim();

    if (!normalizedRole) {
      return;
    }

    if (normalizedRole.length > CreateInitiativePage.maxRoleLength) {
      this.rolesError.set(this.t('initiative.create.validation.roleTooLong'));
      return;
    }

    if (this.roles().some((role) => role.toLowerCase() === normalizedRole.toLowerCase())) {
      this.rolesError.set(this.t('initiative.create.validation.roleDuplicate'));
      return;
    }

    if (this.roles().length >= CreateInitiativePage.maxRoles) {
      this.rolesError.set(this.t('initiative.create.validation.rolesLimit'));
      return;
    }

    this.roles.update((roles) => [...roles, normalizedRole]);
    this.rolesError.set(null);
    input.value = '';
  }

  removeRole(role: string): void {
    this.roles.update((roles) => roles.filter((currentRole) => currentRole !== role));
  }

  toggleInterest(interestId: string): void {
    const selected = this.selectedInterestIds();

    if (!selected.has(interestId) && selected.size >= CreateInitiativePage.maxInterests) {
      this.interestsError.set(this.t('initiative.create.validation.interestsLimit'));
      return;
    }

    this.interestsError.set(null);
    this.selectedInterestIds.update((currentIds) => {
      const nextIds = new Set(currentIds);
      nextIds.has(interestId) ? nextIds.delete(interestId) : nextIds.add(interestId);
      return nextIds;
    });
  }

  isInterestSelected(interestId: string): boolean {
    return this.selectedInterestIds().has(interestId);
  }

  fieldError(controlName: 'title' | 'shortDescription' | 'teamSize'): string | null {
    const control = this.form.controls[controlName];

    if (!control.touched && !control.dirty) {
      return null;
    }

    if (control.hasError('required') || control.hasError('pattern')) {
      return controlName === 'title'
        ? this.t('initiative.create.validation.titleRequired')
        : this.t('initiative.create.validation.descriptionRequired');
    }

    if (control.hasError('maxlength')) {
      return controlName === 'title'
        ? this.t('initiative.create.validation.titleTooLong')
        : this.t('initiative.create.validation.descriptionTooLong');
    }

    if (control.hasError('min') || control.hasError('max')) {
      return this.t('initiative.create.validation.teamSizeRange');
    }

    return null;
  }

  t(key: TranslationKey, params?: Record<string, string | number | boolean | null | undefined>): string {
    return this.i18n.t(key, params);
  }

  private loadInterests(): void {
    this.isLoadingInterests.set(true);
    this.profileApi.getInterests().subscribe({
      next: (interests) => {
        this.interests.set(interests);
        this.isLoadingInterests.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, this.t('initiative.common.loadInterestsFallback')));
        this.isLoadingInterests.set(false);
      },
    });
  }
}
