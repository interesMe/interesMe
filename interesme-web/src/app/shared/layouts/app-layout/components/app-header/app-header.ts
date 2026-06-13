import { Component, computed, inject } from '@angular/core';

import { I18nService } from '../../../../../core/i18n/i18n.service';

@Component({
  selector: 'app-header',
  standalone: true,
  templateUrl: './app-header.html',
  styleUrl: './app-header.scss',
})
export class AppHeader {
  private readonly i18n = inject(I18nService);

  readonly text = computed(() => {
    this.i18n.currentLocale();

    return {
      eyebrow: this.i18n.t('appHeader.eyebrow'),
      title: this.i18n.t('appHeader.title'),
    };
  });
}
