import { Component, inject } from '@angular/core';

import { AuthService } from '../../../auth/services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class ProfileComponent {
  private readonly authService = inject(AuthService);

  readonly user = this.authService.user;

  logout(): void {
    this.authService.logout();
  }
}
