import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { Router } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';

@Component({
  selector: 'app-onboarding-loading-page',
  standalone: true,
  imports: [NgClass],
  templateUrl: './loading.html',
  styleUrl: './loading.scss',
})
export class OnboardingLoadingPage implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly timers: ReturnType<typeof setTimeout>[] = [];

  readonly activeStep = signal(0);
  readonly steps = [
    'Interests selected',
    'Searching active initiatives',
    'Preparing your first recommendations',
  ];

  ngOnInit(): void {
    this.timers.push(
      setTimeout(() => this.activeStep.set(1), 800),
      setTimeout(() => this.activeStep.set(2), 1600),
      setTimeout(() => void this.router.navigateByUrl(`/${APP_ROUTES.onboardingHope}`), 2400),
    );
  }

  ngOnDestroy(): void {
    this.timers.forEach((timer) => clearTimeout(timer));
  }
}
