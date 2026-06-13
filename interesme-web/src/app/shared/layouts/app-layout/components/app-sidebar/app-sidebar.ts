import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { APP_ROUTES } from '../../../../../core/constants/routes.constants';
import { AuthService } from '../../../../../modules/auth/services/auth.service';

type SidebarRouteItem = {
  label: string;
  icon: string;
  path: string;
  exact?: boolean;
};

type SidebarPlaceholderItem = {
  label: string;
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

  readonly navItems: SidebarItem[] = [
    {
      label: 'Home',
      icon: '/assets/icons/home.svg',
      path: `/${APP_ROUTES.home}`,
      exact: true,
    },
    {
      label: 'Initiatives',
      icon: '/assets/icons/home.svg',
      path: `/${APP_ROUTES.initiatives}`,
      exact: false,
    },
    {
      label: 'Interests',
      icon: '/assets/icons/profile.svg',
      disabled: true,
    },
    {
      label: 'Messages',
      icon: '/assets/icons/message.svg',
      disabled: true,
    },
    {
      label: 'Portfolio',
      icon: '/assets/icons/portfolio.svg',
      disabled: true,
    },
    {
      label: 'Profile',
      icon: '/assets/icons/profile.svg',
      path: `/${APP_ROUTES.profile}`,
      exact: true,
    },
    {
      label: 'Settings',
      icon: '/assets/icons/settings.svg',
      disabled: true,
    },
  ];

  isRouteItem(item: SidebarItem): item is SidebarRouteItem {
    return 'path' in item;
  }

  logout(): void {
    this.authService.logout();
  }
}