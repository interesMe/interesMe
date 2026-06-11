import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translations';
import { getApiErrorMessage, normalizeApiError } from '../../../../core/utils/api-error.util';
import { Initiative, InitiativeJoinRequest } from '../../models';
import { InitiativesApiService } from '../../services/initiatives-api.service';

@Component({
  selector: 'app-applications-review-page',
  standalone: true,
  imports: [RouterLink],
  template: `
    <main class="placeholder-page">
      @if (initiative(); as item) {
        <a [routerLink]="detailsPath(item)">{{ t('initiative.common.details') }}</a>
      } @else {
        <a [routerLink]="listPath">{{ t('initiative.common.backToList') }}</a>
      }

      <h1>{{ t('initiative.applications.title') }}</h1>

      @if (isLoading()) {
        <p>{{ t('initiative.common.loading') }}</p>
      } @else if (isNotFound()) {
        <p>{{ t('initiative.common.notFound') }}</p>
      } @else if (errorMessage()) {
        <p class="form-error">{{ errorMessage() }}</p>
        <button class="button button-primary" type="button" (click)="loadPage()">{{ t('initiative.common.retry') }}</button>
      } @else if (joinRequests().length === 0) {
        <p>{{ t('initiative.applications.empty') }}</p>
      } @else {
        <section class="applications">
          @for (request of joinRequests(); track request.id) {
            <article class="application-card">
              <p><strong>{{ t('initiative.applications.status') }}:</strong> {{ request.status }}</p>
              @if (request.roleId) {
                <p><strong>{{ t('initiative.apply.role') }}:</strong> {{ roleName(request.roleId) }}</p>
              }
              @if (request.message) {
                <p><strong>{{ t('initiative.apply.message') }}:</strong> {{ request.message }}</p>
              }
              @if (request.motivation) {
                <p><strong>{{ t('initiative.apply.motivation') }}:</strong> {{ request.motivation }}</p>
              }
              @if (request.experience) {
                <p><strong>{{ t('initiative.apply.experience') }}:</strong> {{ request.experience }}</p>
              }
              @if (request.contribution) {
                <p><strong>{{ t('initiative.apply.contribution') }}:</strong> {{ request.contribution }}</p>
              }
              @if (request.availability) {
                <p><strong>{{ t('initiative.apply.availability') }}:</strong> {{ request.availability }}</p>
              }
              <div class="actions">
                <button class="button button-secondary" type="button" (click)="accept(request)">
                  {{ t('initiative.applications.accept') }}
                </button>
                <button class="button button-secondary" type="button" (click)="reject(request)">
                  {{ t('initiative.applications.reject') }}
                </button>
              </div>
            </article>
          }
        </section>
      }
    </main>
  `,
  styles: [
    `
      .applications { display: grid; gap: 14px; margin-top: 20px; }
      .application-card { display: grid; gap: 10px; border: 1px solid var(--im-border); border-radius: var(--im-radius-md); padding: 16px; }
      .actions { display: flex; flex-wrap: wrap; gap: 10px; }
      .form-error { color: var(--im-connect); }
    `,
  ],
})
export class ApplicationsReviewPage implements OnInit {
  private readonly initiativesApi = inject(InitiativesApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly i18n = inject(I18nService);

  readonly listPath = `/${APP_ROUTES.initiatives}`;
  readonly initiative = signal<Initiative | null>(null);
  readonly joinRequests = signal<InitiativeJoinRequest[]>([]);
  readonly isLoading = signal(true);
  readonly isNotFound = signal(false);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadPage();
  }

  loadPage(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.isLoading.set(false);
      this.isNotFound.set(true);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      initiative: this.initiativesApi.getById(id),
      joinRequests: this.initiativesApi.getJoinRequests(id),
    }).subscribe({
      next: ({ initiative, joinRequests }) => {
        this.initiative.set(initiative);
        this.joinRequests.set(joinRequests);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        const apiError = normalizeApiError(error, this.t('initiative.applications.errorFallback'));
        this.isNotFound.set(apiError.status === 404);
        this.errorMessage.set(apiError.status === 404 ? null : getApiErrorMessage(error, this.t('initiative.applications.errorFallback')));
        this.isLoading.set(false);
      },
    });
  }

  accept(request: InitiativeJoinRequest): void {
    this.updateRequest(request, 'accept');
  }

  reject(request: InitiativeJoinRequest): void {
    this.updateRequest(request, 'reject');
  }

  roleName(roleId: string): string {
    return this.initiative()?.roles.find((role) => role.id === roleId)?.name ?? roleId;
  }

  detailsPath(initiative: Initiative): string {
    return `/${APP_ROUTES.initiatives}/${initiative.id}`;
  }

  t(key: TranslationKey, params?: Record<string, string | number | boolean | null | undefined>): string {
    return this.i18n.t(key, params);
  }

  private updateRequest(request: InitiativeJoinRequest, action: 'accept' | 'reject'): void {
    const initiative = this.initiative();
    if (!initiative) {
      return;
    }

    const operation =
      action === 'accept'
        ? this.initiativesApi.acceptJoinRequest(initiative.id, request.id)
        : this.initiativesApi.rejectJoinRequest(initiative.id, request.id);

    operation.subscribe({
      next: (updatedRequest) => {
        this.joinRequests.update((requests) =>
          requests.map((currentRequest) => (currentRequest.id === updatedRequest.id ? updatedRequest : currentRequest)),
        );
      },
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, this.t('initiative.applications.actionErrorFallback')));
      },
    });
  }
}
