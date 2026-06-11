import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translations';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { AuthService } from '../../../auth/services/auth.service';
import { Initiative } from '../../models';
import { InitiativesApiService } from '../../services/initiatives-api.service';

@Component({
  selector: 'app-my-initiatives-page',
  standalone: true,
  imports: [RouterLink],
  template: `
    <main class="placeholder-page">
      <h1>{{ t('initiative.my.title') }}</h1>
      <p>{{ t('initiative.my.description') }}</p>

      @if (!currentUserId()) {
        <p>{{ t('initiative.my.noUser') }}</p>
      } @else if (isLoading()) {
        <p>{{ t('initiative.common.loading') }}</p>
      } @else if (errorMessage()) {
        <p class="form-error">{{ errorMessage() }}</p>
        <button class="button button-primary" type="button" (click)="loadInitiatives()">{{ t('initiative.common.retry') }}</button>
      } @else if (ownedInitiatives().length === 0) {
        <p>{{ t('initiative.my.empty') }}</p>
        <a class="button button-primary" [routerLink]="createPath">{{ t('initiativesList.header.create') }}</a>
      } @else {
        <section class="cards">
          @for (initiative of ownedInitiatives(); track initiative.id) {
            <article class="card">
              <h2>{{ initiative.title }}</h2>
              <p>{{ initiative.shortDescription }}</p>
              <div class="actions">
                <a class="button button-secondary" [routerLink]="detailsPath(initiative)">{{ t('initiative.common.details') }}</a>
                <a class="button button-secondary" [routerLink]="editPath(initiative)">{{ t('initiative.common.edit') }}</a>
                <a class="button button-secondary" [routerLink]="applicationsPath(initiative)">
                  {{ t('initiative.common.applications') }}
                </a>
                @if (initiative.slug) {
                  <button class="button button-secondary" type="button" (click)="copyPublicLink(initiative)">
                    {{ copyMessage() || t('initiative.common.copyLink') }}
                  </button>
                }
              </div>
            </article>
          }
        </section>
      }
    </main>
  `,
  styles: [
    `
      .cards { display: grid; gap: 14px; margin-top: 20px; }
      .card { display: grid; gap: 10px; border: 1px solid var(--im-border); border-radius: var(--im-radius-md); padding: 16px; }
      .actions { display: flex; flex-wrap: wrap; gap: 10px; }
      .form-error { color: var(--im-connect); }
    `,
  ],
})
export class MyInitiativesPage implements OnInit {
  private readonly initiativesApi = inject(InitiativesApiService);
  private readonly authService = inject(AuthService);
  private readonly i18n = inject(I18nService);

  readonly initiatives = signal<Initiative[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly copyMessage = signal<string | null>(null);
  readonly createPath = `/${APP_ROUTES.initiatives}/new`;
  readonly currentUserId = computed(() => this.authService.user()?.id ?? null);
  readonly ownedInitiatives = computed(() => {
    const userId = this.currentUserId();
    return userId ? this.initiatives().filter((initiative) => initiative.ownerUserId === userId) : [];
  });

  ngOnInit(): void {
    if (this.currentUserId()) {
      this.loadInitiatives();
    } else {
      this.isLoading.set(false);
    }
  }

  loadInitiatives(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.initiativesApi.getAll().subscribe({
      next: (initiatives) => {
        this.initiatives.set(initiatives);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, this.t('initiativesList.error.fallback')));
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

  detailsPath(initiative: Initiative): string {
    return `/${APP_ROUTES.initiatives}/${initiative.id}`;
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
}
