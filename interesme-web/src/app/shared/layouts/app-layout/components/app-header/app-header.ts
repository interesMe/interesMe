import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { APP_ROUTES } from '../../../../../core/constants/routes.constants';
import { I18nService } from '../../../../../core/i18n/i18n.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './app-header.html',
  styleUrl: './app-header.scss',
})
export class AppHeader {
  private readonly i18n = inject(I18nService);
  readonly createPostRoute = ['/', APP_ROUTES.posts, 'new'];

  readonly text = computed(() => {
    this.i18n.currentLocale();

    return {
      eyebrow: this.i18n.t('appHeader.eyebrow'),
      title: this.i18n.t('appHeader.title'),
      createPost: this.i18n.t('appHeader.createPost'),
    };
  });
}
