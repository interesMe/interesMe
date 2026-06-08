import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';

import { APP_ENVIRONMENT } from '../../../../core/constants/app-environment.constants';
import { AuthService } from '../../../auth/services/auth.service';
import { InterestResponse, ProfileRequest, ProfileResponse } from '../../models';
import { ProfileApiService } from '../../services/profile-api.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class ProfileComponent implements OnInit, OnDestroy {
  private static readonly allowedAvatarTypes = new Set(['image/jpeg', 'image/png', 'image/webp']);
  private static readonly maxAvatarSizeBytes = 2 * 1024 * 1024;

  private readonly authService = inject(AuthService);
  private readonly profileApi = inject(ProfileApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
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
        this.successMessage.set('Profile saved.');
        this.isSavingProfile.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.readErrorMessage(error, 'Unable to save profile.'));
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
        this.successMessage.set('Interests saved.');
        this.isSavingInterests.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.readErrorMessage(error, 'Unable to save interests.'));
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
        this.errorMessage.set(this.readErrorMessage(error, 'Unable to load profile page.'));
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
    if (!ProfileComponent.allowedAvatarTypes.has(file.type)) {
      return 'Avatar must be a JPG, PNG, or WEBP image.';
    }

    if (file.size > ProfileComponent.maxAvatarSizeBytes) {
      return 'Avatar must be 2 MB or smaller.';
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

  private readErrorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && typeof error.error?.message === 'string') {
      return error.error.message;
    }

    return fallback;
  }
}
