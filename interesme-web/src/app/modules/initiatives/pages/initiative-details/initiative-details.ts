import { NgClass } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translations';
import { getApiErrorMessage, normalizeApiError } from '../../../../core/utils/api-error.util';
import { AuthService } from '../../../auth/services/auth.service';
import {
  Initiative,
  InitiativeGoalType,
  InitiativeJoinRequestStatus,
  InitiativeStatus,
  InitiativeVisibility,
} from '../../models';
import { InitiativesApiService } from '../../services/initiatives-api.service';

/**
 * IME-14: initiative details — description, requirements (interests, roles), owner and current team.
 * IME-15: shows the viewer's application status instead of the apply button when a request exists.
 */
@Component({
  selector: 'app-initiative-details-page',
  standalone: true,
  imports: [NgClass, RouterLink],
  templateUrl: './initiative-details.html',
  styleUrl: './initiative-details.scss',
})
export class InitiativeDetailsPage implements OnInit {
  private readonly initiativesApi = inject(InitiativesApiService);
  private readonly authService = inject(AuthService);
  private readonly i18n = inject(I18nService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly initiative = signal<Initiative | null>(null);
  readonly isLoading = signal(true);
  readonly isNotFound = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly copyMessage = signal<string | null>(null);
  readonly confirmingDelete = signal(false);
  readonly isDeleting = signal(false);
  readonly deleteError = signal<string | null>(null);
  readonly listPath = `/${APP_ROUTES.initiatives}`;

  readonly isOwner = computed(() => {
    const userId = this.authService.user()?.id;
    const ownerUserId = this.initiative()?.ownerUserId;
    return Boolean(userId && ownerUserId && userId === ownerUserId);
  });

  /** Team = owner + accepted members. */
  readonly teamCount = computed(() => (this.initiative()?.memberCount ?? 0) + 1);
  readonly isTeamFull = computed(() => {
    const teamSize = this.initiative()?.teamSize;
    return teamSize != null && this.teamCount() >= teamSize;
  });
  readonly isClosed = computed(() => {
    const status = this.initiative()?.status;
    return status === InitiativeStatus.Completed || status === InitiativeStatus.Archived;
  });
  /** A pending or accepted request blocks a new application; a rejected one allows re-applying. */
  readonly activeRequest = computed(() => {
    const request = this.initiative()?.myJoinRequest;
    return request && request.status !== InitiativeJoinRequestStatus.Rejected ? request : null;
  });
  readonly canApply = computed(
    () => !this.isOwner() && !this.activeRequest() && !this.isClosed() && !this.isTeamFull(),
  );

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
        this.errorMessage.set(
          apiError.status === 404 ? null : getApiErrorMessage(error, this.t('initiative.common.loadFallback')),
        );
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

  askDelete(): void {
    this.deleteError.set(null);
    this.confirmingDelete.set(true);
  }

  cancelDelete(): void {
    this.confirmingDelete.set(false);
  }

  confirmDelete(initiative: Initiative): void {
    if (this.isDeleting()) {
      return;
    }

    this.isDeleting.set(true);
    this.deleteError.set(null);

    this.initiativesApi.delete(initiative.id).subscribe({
      next: () => void this.router.navigateByUrl(`/${APP_ROUTES.myInitiatives}`),
      error: (error: unknown) => {
        this.deleteError.set(getApiErrorMessage(error, this.t('initiative.details.deleteError')));
        this.isDeleting.set(false);
      },
    });
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

  profilePath(userId: string): string[] {
    return ['/', 'u', userId];
  }

  statusLabel(status: InitiativeStatus): string {
    const keys: Record<InitiativeStatus, TranslationKey> = {
      [InitiativeStatus.Idea]: 'initiative.status.idea',
      [InitiativeStatus.Active]: 'initiative.status.active',
      [InitiativeStatus.Completed]: 'initiative.status.completed',
      [InitiativeStatus.Archived]: 'initiative.status.archived',
    };
    return this.t(keys[status]);
  }

  requestStatusLabel(status: InitiativeJoinRequestStatus): string {
    const keys: Record<InitiativeJoinRequestStatus, TranslationKey> = {
      [InitiativeJoinRequestStatus.Pending]: 'initiative.myRequest.pending',
      [InitiativeJoinRequestStatus.Accepted]: 'initiative.myRequest.accepted',
      [InitiativeJoinRequestStatus.Rejected]: 'initiative.myRequest.rejected',
    };
    return this.t(keys[status]);
  }

  requestStatusClass(status: InitiativeJoinRequestStatus): string {
    return status === InitiativeJoinRequestStatus.Accepted
      ? 'is-accepted'
      : status === InitiativeJoinRequestStatus.Rejected
        ? 'is-rejected'
        : 'is-pending';
  }

  isPrivate(initiative: Initiative): boolean {
    return initiative.visibility === InitiativeVisibility.Private;
  }

  initial(name: string): string {
    return name.trim().charAt(0).toUpperCase() || 'I';
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
