import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';

import { APP_ENVIRONMENT } from '../../../../core/constants/app-environment.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { AuthService } from '../../../auth/services/auth.service';
import { InterestResponse, ProfileRequest, ProfileResponse } from '../../models';
import { ProfileApiService } from '../../services/profile-api.service';

@Component({
  selector: 'app-profile-edit',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './profile-edit.html',
  styleUrl: './profile-edit.scss',
})
export class ProfileEditComponent implements OnInit, OnDestroy {
  private static readonly allowedAvatarTypes = new Set(['image/jpeg', 'image/png', 'image/webp']);
  private static readonly maxAvatarSizeBytes = 2 * 1024 * 1024;

  private readonly authService = inject(AuthService);
  private readonly profileApi = inject(ProfileApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly i18n = inject(I18nService);
  private objectAvatarPreviewUrl: string | null = null;

  readonly user = this.authService.user;
  readonly isLoading = signal(true);
  readonly isSavingProfile = signal(false);
  readonly isSavingInterests = signal(false);
  readonly profileExists = signal(false);
  readonly interests = signal<InterestResponse[]>([]);
  readonly selectedInterestIds = signal<Set<string>>(new Set<string>());
  readonly selectedAvatarFile = signal<File | null>(null);
  readonly avatarUrl = signal<string | null>(null);
  readonly resolvedAvatarUrl = signal<string | null>(null);
  readonly avatarPreviewUrl = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly text = computed(() => {
    this.i18n.currentLocale();

    return {
      eyebrow: this.i18n.t('profile.header.eyebrow'),
      title: this.i18n.t('profile.header.title'),
      subtitle: this.i18n.t('profile.header.subtitle'),
      defaultUser: this.i18n.t('profile.account.defaultUser'),
      signOut: this.i18n.t('profile.account.signOut'),
      loading: this.i18n.t('profile.loading'),
      updateProfile: this.i18n.t('profile.form.updateTitle'),
      createProfile: this.i18n.t('profile.form.createTitle'),
      updateDescription: this.i18n.t('profile.form.updateDescription'),
      createDescription: this.i18n.t('profile.form.createDescription'),
      avatarAlt: this.i18n.t('profile.form.avatarAlt'),
      avatarImage: this.i18n.t('profile.form.avatarImage'),
      avatarHint: this.i18n.t('profile.form.avatarHint'),
      displayName: this.i18n.t('profile.form.displayName'),
      displayNamePlaceholder: this.i18n.t('profile.form.displayNamePlaceholder'),
      displayNameHint: this.i18n.t('profile.form.displayNameHint'),
      city: this.i18n.t('profile.form.city'),
      cityPlaceholder: this.i18n.t('profile.form.cityPlaceholder'),
      cityHint: this.i18n.t('profile.form.cityHint'),
      birthDate: this.i18n.t('profile.form.birthDate'),
      savingProfile: this.i18n.t('profile.form.saving'),
      saveProfile: this.i18n.t('profile.form.save'),
      interestsTitle: this.i18n.t('profile.interests.title'),
      interestsDescription: this.i18n.t('profile.interests.description'),
      interestsEmpty: this.i18n.t('profile.interests.empty'),
      interestsAria: this.i18n.t('profile.interests.aria'),
      savingInterests: this.i18n.t('profile.interests.saving'),
      saveInterests: this.i18n.t('profile.interests.save'),
    };
  });

  readonly form = this.formBuilder.group({
    displayName: ['', [Validators.required, Validators.maxLength(80)]],
    city: ['', [Validators.maxLength(120)]],
    birthDate: [''],
  });

  ngOnInit(): void {
    this.loadProfilePage();
  }

  ngOnDestroy(): void {
    this.revokeObjectAvatarPreviewUrl();
  }

  logout(): void {
    this.authService.logout();
  }

  saveProfile(): void {
    if (this.form.invalid || this.isSavingProfile()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSavingProfile.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const request = this.buildProfileRequest();
    const avatarFile = this.selectedAvatarFile();
    const saveRequest = this.profileExists()
      ? avatarFile
        ? this.profileApi.updateMyProfileWithAvatar(request, avatarFile)
        : this.profileApi.updateMyProfile(request)
      : avatarFile
        ? this.profileApi.createMyProfileWithAvatar(request, avatarFile)
        : this.profileApi.createMyProfile(request);

    saveRequest.subscribe({
      next: (profile) => {
        this.applyProfile(profile);
        this.profileExists.set(true);
        this.selectedAvatarFile.set(null);
        this.successMessage.set(this.i18n.t('profile.messages.profileSaved'));
        this.isSavingProfile.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, this.i18n.t('profile.messages.profileSaveError')));
        this.isSavingProfile.set(false);
      },
    });
  }

  toggleInterest(interestId: string): void {
    this.selectedInterestIds.update((currentIds) => {
      const nextIds = new Set(currentIds);

      if (nextIds.has(interestId)) {
        nextIds.delete(interestId);
      } else {
        nextIds.add(interestId);
      }

      return nextIds;
    });
  }

  isInterestSelected(interestId: string): boolean {
    return this.selectedInterestIds().has(interestId);
  }

  saveInterests(): void {
    if (this.isSavingInterests()) {
      return;
    }

    this.isSavingInterests.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.profileApi.saveMyInterests({ interestIds: [...this.selectedInterestIds()] }).subscribe({
      next: (savedInterests) => {
        this.selectedInterestIds.set(new Set(savedInterests.map((interest) => interest.id)));
        this.successMessage.set(this.i18n.t('profile.messages.interestsSaved'));
        this.isSavingInterests.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, this.i18n.t('profile.messages.interestsSaveError')));
        this.isSavingInterests.set(false);
      },
    });
  }

  selectAvatar(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (!file) {
      this.selectedAvatarFile.set(null);
      this.setAvatarPreviewFromProfile();
      return;
    }

    const validation = this.validateAvatarFile(file);

    if (validation) {
      input.value = '';
      this.selectedAvatarFile.set(null);
      this.setAvatarPreviewFromProfile();
      this.errorMessage.set(validation);
      return;
    }

    this.selectedAvatarFile.set(file);
    this.revokeObjectAvatarPreviewUrl();
    this.objectAvatarPreviewUrl = URL.createObjectURL(file);
    this.avatarPreviewUrl.set(this.objectAvatarPreviewUrl);
  }

  private loadProfilePage(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    forkJoin({
      profile: this.profileApi.getMyProfile(),
      interests: this.profileApi.getInterests(),
    }).subscribe({
      next: ({ profile, interests }) => {
        if (profile) {
          this.applyProfile(profile);
          this.profileExists.set(true);
        } else {
          this.profileExists.set(false);
          this.form.reset({
            displayName: this.user()?.displayName ?? '',
            city: '',
            birthDate: '',
          });
          this.avatarUrl.set(null);
          this.resolvedAvatarUrl.set(null);
          this.setAvatarPreviewFromProfile();
        }

        this.interests.set(interests);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, this.i18n.t('profile.messages.loadError')));
        this.isLoading.set(false);
      },
    });
  }

  private applyProfile(profile: ProfileResponse): void {
    this.form.patchValue({
      displayName: profile.displayName,
      city: profile.city ?? '',
      birthDate: profile.birthDate ?? '',
    });
    this.avatarUrl.set(profile.avatarUrl);
    this.resolvedAvatarUrl.set(this.resolveAvatarUrl(profile.avatarUrl));
    this.setAvatarPreviewFromProfile();
  }

  private buildProfileRequest(): ProfileRequest {
    const value = this.form.getRawValue();

    return {
      displayName: value.displayName.trim(),
      city: this.normalizeOptional(value.city),
      avatarUrl: this.avatarUrl(),
      birthDate: this.normalizeOptional(value.birthDate),
    };
  }

  private normalizeOptional(value: string): string | null {
    const normalized = value.trim();

    return normalized.length > 0 ? normalized : null;
  }

  private validateAvatarFile(file: File): string | null {
    if (!ProfileEditComponent.allowedAvatarTypes.has(file.type)) {
      return this.i18n.t('profile.messages.avatarTypeError');
    }

    if (file.size > ProfileEditComponent.maxAvatarSizeBytes) {
      return this.i18n.t('profile.messages.avatarSizeError');
    }

    return null;
  }

  private setAvatarPreviewFromProfile(): void {
    this.revokeObjectAvatarPreviewUrl();
    this.avatarPreviewUrl.set(this.resolvedAvatarUrl());
  }

  private resolveAvatarUrl(avatarUrl: string | null): string | null {
    if (!avatarUrl) {
      return null;
    }

    const trimmedAvatarUrl = avatarUrl.trim();

    if (!trimmedAvatarUrl) {
      return null;
    }

    if (/^https?:\/\//i.test(avatarUrl)) {
      return trimmedAvatarUrl;
    }

    if (!trimmedAvatarUrl.startsWith('/uploads')) {
      return trimmedAvatarUrl;
    }

    const backendBaseUrl = this.resolveBackendBaseUrl();

    return `${backendBaseUrl}${trimmedAvatarUrl}`;
  }

  private resolveBackendBaseUrl(): string {
    const apiBaseUrl = APP_ENVIRONMENT.apiBaseUrl.replace(/\/$/, '');
    const apiBaseWithoutApiPath = apiBaseUrl.replace(/\/api$/, '');

    if (/^https?:\/\//i.test(apiBaseWithoutApiPath)) {
      return apiBaseWithoutApiPath;
    }

    return globalThis.location?.origin ?? '';
  }

  private revokeObjectAvatarPreviewUrl(): void {
    if (this.objectAvatarPreviewUrl) {
      URL.revokeObjectURL(this.objectAvatarPreviewUrl);
      this.objectAvatarPreviewUrl = null;
    }
  }
}
