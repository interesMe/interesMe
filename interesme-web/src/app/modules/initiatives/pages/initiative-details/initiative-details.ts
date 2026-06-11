import { NgClass } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translations';
import { getApiErrorMessage, normalizeApiError } from '../../../../core/utils/api-error.util';
import { AuthService } from '../../../auth/services/auth.service';
import { Initiative, InitiativeGoalType } from '../../models';
import { InitiativesApiService } from '../../services/initiatives-api.service';

@Component({
  selector: 'app-initiative-details-page',
  standalone: true,
  imports: [NgClass, RouterLink],
  template: `
    <main class="placeholder-page">
      <a [routerLink]="listPath">{{ t('initiative.common.backToList') }}</a>

      @if (isLoading()) {
        <p>{{ t('initiative.common.loading') }}</p>
      } @else if (isNotFound()) {
        <h1>{{ t('initiative.common.notFound') }}</h1>
      } @else if (errorMessage()) {
        <h1>{{ t('initiative.common.error') }}</h1>
        <p>{{ errorMessage() }}</p>
        <button class="button button-primary" type="button" (click)="loadInitiative()">{{ t('initiative.common.retry') }}</button>
      } @else if (initiative(); as item) {
        <header class="page-stack">
          <span class="goal-badge" [ngClass]="goalClass(item.goalType)">{{ goalLabel(item.goalType) }}</span>
          <h1>{{ item.title }}</h1>
          <p>{{ item.shortDescription }}</p>
        </header>

        <section class="page-stack">
          @if (item.university) {
            <p><strong>{{ t('initiative.common.university') }}:</strong> {{ item.university }}</p>
          }
          @if (item.teamSize) {
            <p><strong>{{ t('initiative.common.teamSize') }}:</strong> {{ item.teamSize }}</p>
          }
          @if (item.slug) {
            <button class="button button-secondary" type="button" (click)="copyPublicLink(item)">
              {{ copyMessage() || t('initiative.common.copyLink') }}
            </button>
          }
        </section>

        <section class="page-stack">
          <h2>{{ t('initiative.common.interests') }}</h2>
          <div class="chips">
            @for (interest of item.interests; track interest.id) {
              <span class="chip">{{ interest.name }}</span>
            }
          </div>
        </section>

        <section class="page-stack">
          <h2>{{ t('initiative.common.roles') }}</h2>
          <div class="chips">
            @for (role of item.roles; track role.id) {
              <span class="chip">{{ role.name }}</span>
            }
          </div>
        </section>

        @if (isOwner()) {
          <section class="page-stack">
            <h2>{{ t('initiative.details.ownerActions') }}</h2>
            <p>{{ t('initiative.details.ownerActionsNote') }}</p>
            <div class="actions">
              <a class="button button-secondary" [routerLink]="editPath(item)">{{ t('initiative.common.edit') }}</a>
              <a class="button button-secondary" [routerLink]="applicationsPath(item)">
                {{ t('initiative.common.applications') }}
              </a>
            </div>
            <p>{{ t('initiative.details.archiveDeletePlaceholder') }}</p>
          </section>
        } @else {
          <a class="button button-primary" [routerLink]="applyPath(item)">{{ t('initiative.common.apply') }}</a>
        }
      }
    </main>
  `,
  styles: [
    `
      .page-stack { display: grid; gap: 12px; margin-top: 20px; }
      .actions, .chips { display: flex; flex-wrap: wrap; gap: 10px; }
      .goal-badge, .chip { display: inline-flex; width: fit-content; border-radius: 999px; padding: 6px 12px; background: var(--im-surface-soft); }
      .goal-connect { color: var(--im-connect); }
      .goal-learn { color: #15803d; }
      .goal-build { color: var(--im-primary-hover); }
      .goal-play { color: #b45309; }
      .goal-explore { color: #0284c7; }
    `,
  ],
})
export class InitiativeDetailsPage implements OnInit {
  private readonly initiativesApi = inject(InitiativesApiService);
  private readonly authService = inject(AuthService);
  private readonly i18n = inject(I18nService);
  private readonly route = inject(ActivatedRoute);

  readonly initiative = signal<Initiative | null>(null);
  readonly isLoading = signal(true);
  readonly isNotFound = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly copyMessage = signal<string | null>(null);
  readonly listPath = `/${APP_ROUTES.initiatives}`;

  readonly isOwner = computed(() => {
    const userId = this.authService.user()?.id;
    const ownerUserId = this.initiative()?.ownerUserId;
    return Boolean(userId && ownerUserId && userId === ownerUserId);
  });

  ngOnInit(): void {
    this.loadInitiative();
  }

  loadInitiative(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.isLoading.set(false);
      this.isNotFound.set(true);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.isNotFound.set(false);

    this.initiativesApi.getById(id).subscribe({
      next: (initiative) => {
        this.initiative.set(initiative);
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

  copyPublicLink(initiative: Initiative): void {
    const link = `${globalThis.location?.origin ?? ''}/i/${initiative.slug}`;
    void globalThis.navigator?.clipboard?.writeText(link);
    this.copyMessage.set(this.t('initiative.common.linkCopied'));
    window.setTimeout(() => this.copyMessage.set(null), 1600);
  }

  applyPath(initiative: Initiative): string {
    return `/${APP_ROUTES.initiatives}/${initiative.id}/apply`;
  }

  editPath(initiative: Initiative): string {
    return `/${APP_ROUTES.initiatives}/${initiative.id}/edit`;
  }

  applicationsPath(initiative: Initiative): string {
    return `/${APP_ROUTES.initiatives}/${initiative.id}/applications`;
  }

  t(key: TranslationKey, params?: Record<string, string | number | boolean | null | undefined>): string {
    return this.i18n.t(key, params);
  }

  goalLabel(goalType: InitiativeGoalType): string {
    return this.t(`publicInitiative.goal.${this.goalKey(goalType)}` as TranslationKey);
  }

  goalClass(goalType: InitiativeGoalType): string {
    return `goal-${this.goalKey(goalType)}`;
  }

  private goalKey(goalType: InitiativeGoalType): string {
    switch (goalType) {
      case InitiativeGoalType.Connect:
        return 'connect';
      case InitiativeGoalType.Learn:
        return 'learn';
      case InitiativeGoalType.Build:
        return 'build';
      case InitiativeGoalType.Play:
        return 'play';
      case InitiativeGoalType.Explore:
        return 'explore';
      default:
        return 'explore';
    }
  }
}
