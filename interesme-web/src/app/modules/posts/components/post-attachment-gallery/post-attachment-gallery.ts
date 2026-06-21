import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { APP_ENVIRONMENT } from '../../../../core/constants/app-environment.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { PostAttachment } from '../../models';

@Component({
  selector: 'app-post-attachment-gallery',
  standalone: true,
  templateUrl: './post-attachment-gallery.html',
  styleUrl: './post-attachment-gallery.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostAttachmentGallery {
  private readonly i18n = inject(I18nService);

  readonly attachments = input.required<readonly PostAttachment[]>();
  readonly orderedAttachments = computed(() =>
    [...this.attachments()].sort((left, right) => left.order - right.order),
  );
  readonly imageAlt = computed(() => {
    this.i18n.currentLocale();
    return this.i18n.t('posts.attachments.imageAlt');
  });

  imageUrl(url: string): string {
    if (/^https?:\/\//i.test(url) || !url.startsWith('/uploads')) {
      return url;
    }

    return `${APP_ENVIRONMENT.apiBaseUrl.replace(/\/api\/?$/, '')}${url}`;
  }
}
