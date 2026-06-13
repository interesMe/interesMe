import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { Router } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';

@Component({
  selector: 'app-onboarding-loading-page',
  standalone: true,
  imports: [NgClass],
  templateUrl: './loading.html',
  styleUrl: './loading.scss',
})
export class OnboardingLoadingPage implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);
  private readonly timers: ReturnType<typeof setTimeout>[] = [];

  readonly activeStep = signal(0);
  readonly text = computed(() => {
    this.i18n.currentLocale();

    return {
      eyebrow: this.i18n.t('onboarding.loading.eyebrow'),
      title: this.i18n.t('onboarding.loading.title'),
      description: this.i18n.t('onboarding.loading.description'),
      progressAria: this.i18n.t('onboarding.loading.progressAria'),
    };
  });

  readonly steps = computed(() => {
    this.i18n.currentLocale();

    return [
      this.i18n.t('onboarding.loading.step.interests'),
      this.i18n.t('onboarding.loading.step.searching'),
      this.i18n.t('onboarding.loading.step.preparing'),
    ];
  });

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
