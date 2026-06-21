import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnDestroy,
  signal,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { getApiErrorMessage, normalizeApiError } from '../../../../core/utils/api-error.util';
import { PostsApiService } from '../../services/posts-api.service';

const POST_BODY_MAX_LENGTH = 1200;
const MAX_IMAGE_COUNT = 4;
const MAX_IMAGE_SIZE_BYTES = 5 * 1024 * 1024;
const MAX_TOTAL_IMAGE_SIZE_BYTES = 20 * 1024 * 1024;
const IMAGE_TYPE_BY_EXTENSION: Readonly<Record<string, string>> = {
  jpg: 'image/jpeg',
  jpeg: 'image/jpeg',
  png: 'image/png',
  webp: 'image/webp',
};

type SelectedPostImage = {
  id: string;
  file: File;
  previewUrl: string;
};

@Component({
  selector: 'app-create-post-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './create-post.html',
  styleUrl: './create-post.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreatePostPage implements OnDestroy {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly postsApi = inject(PostsApiService);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);

  readonly maxBodyLength = POST_BODY_MAX_LENGTH;
  readonly profileRoute = ['/', APP_ROUTES.profile];
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly attachmentError = signal<string | null>(null);
  readonly selectedImages = signal<SelectedPostImage[]>([]);
  readonly form = this.formBuilder.group({
    body: ['', [Validators.required, Validators.maxLength(POST_BODY_MAX_LENGTH)]],
  });
  readonly text = computed(() => {
    this.i18n.currentLocale();

    return {
      eyebrow: this.i18n.t('createPost.eyebrow'),
      title: this.i18n.t('createPost.title'),
      description: this.i18n.t('createPost.description'),
      bodyLabel: this.i18n.t('createPost.body.label'),
      bodyPlaceholder: this.i18n.t('createPost.body.placeholder'),
      bodyHint: this.i18n.t('createPost.body.hint'),
      required: this.i18n.t('createPost.validation.required'),
      tooLong: this.i18n.t('createPost.validation.tooLong', { max: POST_BODY_MAX_LENGTH }),
      publish: this.i18n.t('createPost.action.publish'),
      publishing: this.i18n.t('createPost.action.publishing'),
      cancel: this.i18n.t('createPost.action.cancel'),
      errorFallback: this.i18n.t('createPost.error.fallback'),
      futureTitle: this.i18n.t('createPost.future.title'),
      futureDescription: this.i18n.t('createPost.future.description'),
      textOnly: this.i18n.t('createPost.future.textOnly'),
      imagesLabel: this.i18n.t('createPost.attachments.label'),
      imagesAction: this.i18n.t('createPost.attachments.action'),
      imagesHint: this.i18n.t('createPost.attachments.hint'),
      removeImage: this.i18n.t('createPost.attachments.remove'),
      countError: this.i18n.t('createPost.attachments.countError', { max: MAX_IMAGE_COUNT }),
      typeError: this.i18n.t('createPost.attachments.typeError'),
      emptyError: this.i18n.t('createPost.attachments.emptyError'),
      sizeError: this.i18n.t('createPost.attachments.sizeError'),
      totalSizeError: this.i18n.t('createPost.attachments.totalSizeError'),
    };
  });

  ngOnDestroy(): void {
    this.revokeImagePreviews(this.selectedImages());
  }

  submit(): void {
    if (this.isSubmitting()) {
      return;
    }

    const body = this.form.controls.body.value.trim();
    if (!body) {
      this.form.controls.body.setErrors({ required: true });
      this.form.controls.body.markAsTouched();
      return;
    }

    if (body.length > POST_BODY_MAX_LENGTH) {
      this.form.controls.body.setErrors({ maxlength: true });
      this.form.controls.body.markAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.attachmentError.set(null);
    this.isSubmitting.set(true);

    this.postsApi
      .create(
        { body },
        this.selectedImages().map((image) => image.file),
      )
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.clearSelectedImages();
          this.form.reset();
          void this.router.navigate(this.profileRoute);
        },
        error: (error: unknown) => {
          const apiError = normalizeApiError(error, this.text().errorFallback);
          const bodyValidationError =
            apiError.validationErrors?.['Body']?.[0] ?? apiError.validationErrors?.['body']?.[0];
          this.errorMessage.set(
            bodyValidationError ?? getApiErrorMessage(error, this.text().errorFallback),
          );
        },
      });
  }

  selectImages(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';

    if (files.length === 0 || this.isSubmitting()) {
      return;
    }

    const currentImages = this.selectedImages();
    if (currentImages.length + files.length > MAX_IMAGE_COUNT) {
      this.attachmentError.set(this.text().countError);
      return;
    }

    for (const file of files) {
      const extension = file.name.split('.').pop()?.toLowerCase() ?? '';
      if (IMAGE_TYPE_BY_EXTENSION[extension] !== file.type) {
        this.attachmentError.set(this.text().typeError);
        return;
      }

      if (file.size <= 0) {
        this.attachmentError.set(this.text().emptyError);
        return;
      }

      if (file.size > MAX_IMAGE_SIZE_BYTES) {
        this.attachmentError.set(this.text().sizeError);
        return;
      }
    }

    const totalSize = [...currentImages.map((image) => image.file), ...files].reduce(
      (total, file) => total + file.size,
      0,
    );
    if (totalSize > MAX_TOTAL_IMAGE_SIZE_BYTES) {
      this.attachmentError.set(this.text().totalSizeError);
      return;
    }

    const selected = files.map((file, index) => ({
      id: `${file.name}-${file.lastModified}-${currentImages.length + index}`,
      file,
      previewUrl: URL.createObjectURL(file),
    }));

    this.attachmentError.set(null);
    this.selectedImages.update((images) => [...images, ...selected]);
  }

  removeImage(id: string): void {
    if (this.isSubmitting()) {
      return;
    }

    const image = this.selectedImages().find((item) => item.id === id);
    if (image) {
      URL.revokeObjectURL(image.previewUrl);
    }

    this.selectedImages.update((images) => images.filter((item) => item.id !== id));
    this.attachmentError.set(null);
  }

  formatFileSize(sizeBytes: number): string {
    return `${(sizeBytes / (1024 * 1024)).toFixed(1)} MiB`;
  }

  bodyValidationMessage(): string | null {
    const control = this.form.controls.body;
    if (!control.touched) {
      return null;
    }

    if (control.hasError('required') || control.value.trim().length === 0) {
      return this.text().required;
    }

    return control.hasError('maxlength') ? this.text().tooLong : null;
  }

  private clearSelectedImages(): void {
    this.revokeImagePreviews(this.selectedImages());
    this.selectedImages.set([]);
    this.attachmentError.set(null);
  }

  private revokeImagePreviews(images: readonly SelectedPostImage[]): void {
    for (const image of images) {
      URL.revokeObjectURL(image.previewUrl);
    }
  }
}
