import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';

@Component({
  selector: 'app-callback',
  imports: [],
  templateUrl: './callback.html',
  styleUrl: './callback.scss',
})
export class CallbackComponent implements OnInit {
  private readonly router = inject(Router);

  readonly message = signal('Google sign in is handled from the login page.');

  ngOnInit(): void {
    void this.router.navigate([APP_ROUTES.login]);
  }
}
