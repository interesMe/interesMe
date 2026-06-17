import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { APP_ROUTES } from '../../../../../core/constants/routes.constants';
import { I18nService } from '../../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../../core/i18n/translations';
import { AuthService } from '../../../../../modules/auth/services/auth.service';

type SidebarRouteItem = {
  labelKey: TranslationKey;
  icon: string;
  path: readonly string[];
  exact?: boolean;
};

type SidebarPlaceholderItem = {
  labelKey: TranslationKey;
  icon: string;
  disabled: true;
};

type SidebarItem = SidebarRouteItem | SidebarPlaceholderItem;

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './app-sidebar.html',
  styleUrl: './app-sidebar.scss',
})
export class AppSidebar {
  private readonly authService = inject(AuthService);
  private readonly i18n = inject(I18nService);

  readonly navItems: SidebarItem[] = [
    {
      labelKey: 'appSidebar.nav.home',
      icon: '/assets/icons/home.svg',
      path: ['/', APP_ROUTES.home],
      exact: true,
    },
    {
      labelKey: 'appSidebar.nav.initiatives',
      icon: '/assets/icons/home.svg',
      path: ['/', APP_ROUTES.initiatives],
      exact: false,
    },
    {
      labelKey: 'appSidebar.nav.interests',
      icon: '/assets/icons/profile.svg',
      disabled: true,
    },
    {
      labelKey: 'appSidebar.nav.messages',
      icon: '/assets/icons/message.svg',
      path: ['/', APP_ROUTES.chat],
      exact: false,
    },
    {
      labelKey: 'appSidebar.nav.portfolio',
      icon: '/assets/icons/portfolio.svg',
      disabled: true,
    },
    {
      labelKey: 'appSidebar.nav.profile',
      icon: '/assets/icons/profile.svg',
      path: ['/', APP_ROUTES.profile],
      exact: true,
    },
    {
      labelKey: 'appSidebar.nav.settings',
      icon: '/assets/icons/settings.svg',
      path: ['/', APP_ROUTES.settings],
      exact: true,
    },
  ];

  readonly text = computed(() => {
    this.i18n.currentLocale();

    return {
      productNavigationAria: this.i18n.t('appSidebar.aria.productNavigation'),
      homeAria: this.i18n.t('appSidebar.aria.home'),
      protectedPagesAria: this.i18n.t('appSidebar.aria.protectedPages'),
      promoAria: this.i18n.t('appSidebar.promo.aria'),
      promoTitle: this.i18n.t('appSidebar.promo.title'),
      promoDescription: this.i18n.t('appSidebar.promo.description'),
      createInitiative: this.i18n.t('appSidebar.promo.createInitiative'),
      currentUserAria: this.i18n.t('appSidebar.aria.currentUser'),
      logout: this.i18n.t('appSidebar.logout'),
    };
  });

  isRouteItem(item: SidebarItem): item is SidebarRouteItem {
    return 'path' in item;
  }

  label(item: SidebarItem): string {
    return this.i18n.t(item.labelKey);
  }

  logout(): void {
    this.authService.logout();
  }
}
