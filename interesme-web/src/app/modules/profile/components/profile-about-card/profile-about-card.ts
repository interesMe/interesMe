import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { I18nService } from '../../../../core/i18n/i18n.service';
import { BasicProfile, ProfileActivityStatus } from '../../models';

@Component({
  selector: 'app-profile-about-card',
  standalone: true,
  templateUrl: './profile-about-card.html',
  styleUrl: './profile-about-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileAboutCardComponent {
  private readonly i18n = inject(I18nService);

  readonly profile = input.required<BasicProfile>();
  readonly text = computed(() => {
    this.i18n.currentLocale();
    return {
      title: this.i18n.t('profileView.about.title'),
      bioEmpty: this.i18n.t('profileView.about.bioEmpty'),
      city: this.i18n.t('profileView.about.city'),
      memberSince: this.i18n.t('profileView.about.memberSince'),
      socialLinks: this.i18n.t('profileView.about.socialLinks'),
      socialEmpty: this.i18n.t('profileView.about.socialEmpty'),
    };
  });

  activityStatusLabel(status: ProfileActivityStatus): string {
    const keys = {
      online: 'profileView.status.online',
      recently_active: 'profileView.status.recentlyActive',
      offline: 'profileView.status.offline',
      unknown: 'profileView.status.unavailable',
    } as const;
    return this.i18n.t(keys[status]);
  }

  socialIcon(type: string): string {
    switch (type.toLowerCase()) {
      case 'github':
        return 'GH';
      case 'instagram':
        return 'IG';
      case 'telegram':
        return 'TG';
      default:
        return '↗';
    }
  }

  formatDate(value: string): string {
    const locale = this.i18n.currentLocale() === 'uk' ? 'uk-UA' : 'en-US';
    return new Intl.DateTimeFormat(locale, { month: 'long', year: 'numeric' }).format(new Date(value));
  }
}
