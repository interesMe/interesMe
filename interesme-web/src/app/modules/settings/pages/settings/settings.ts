import { Component, computed, inject } from '@angular/core';
import { NgClass } from '@angular/common';

import {
  LanguagePreference,
  PreferencesService,
  ThemePreference,
} from '../../../../core/services/preferences.service';
import { I18nService } from '../../../../core/i18n/i18n.service';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [NgClass],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
})
export class SettingsPage {
  private readonly preferences = inject(PreferencesService);
  private readonly i18n = inject(I18nService);

  readonly theme = this.preferences.theme;
  readonly language = this.preferences.language;

  readonly settingsText = computed(() => {
    this.i18n.currentLocale();

    return {
      headerEyebrow: this.i18n.t('settings.header.eyebrow'),
      headerTitle: this.i18n.t('settings.header.title'),
      headerDescription: this.i18n.t('settings.header.description'),
      appearanceTitle: this.i18n.t('settings.appearance.title'),
      appearanceDescription: this.i18n.t('settings.appearance.description'),
      appearanceAria: this.i18n.t('settings.appearance.aria'),
      languageTitle: this.i18n.t('settings.language.title'),
      languageDescription: this.i18n.t('settings.language.description'),
      languageAria: this.i18n.t('settings.language.aria'),
    };
  });

  readonly themeOptions = computed<{ value: ThemePreference; label: string }[]>(() => {
    this.i18n.currentLocale();

    return [
      { value: 'light', label: this.i18n.t('settings.theme.light') },
      { value: 'dark', label: this.i18n.t('settings.theme.dark') },
      { value: 'system', label: this.i18n.t('settings.theme.system') },
    ];
  });

  readonly languageOptions = computed<{ value: LanguagePreference; label: string }[]>(() => {
    this.i18n.currentLocale();

    return [
      { value: 'uk', label: this.i18n.t('settings.language.ukrainian') },
      { value: 'en', label: this.i18n.t('settings.language.english') },
    ];
  });

  setTheme(theme: ThemePreference): void {
    this.preferences.setTheme(theme);
  }

  setLanguage(language: LanguagePreference): void {
    this.preferences.setLanguage(language);
  }
}
