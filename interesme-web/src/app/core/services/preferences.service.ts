import { Injectable, inject, signal } from '@angular/core';

import { AppLocale } from '../i18n/app-locale';
import { I18nService } from '../i18n/i18n.service';

export type ThemePreference = 'light' | 'dark' | 'system';
export type LanguagePreference = 'uk' | 'en';

const THEME_STORAGE_KEY = 'interesme_theme';
const LANGUAGE_STORAGE_KEY = 'interesme_language';
const THEME_VALUES: readonly ThemePreference[] = ['light', 'dark', 'system'];
const LANGUAGE_VALUES: readonly LanguagePreference[] = ['uk', 'en'];

@Injectable({
  providedIn: 'root',
})
export class PreferencesService {
  private readonly i18n = inject(I18nService);
  private readonly systemThemeQuery = this.getSystemThemeQuery();

  private readonly themePreference = signal<ThemePreference>(this.readThemePreference());
  private readonly languagePreference = signal<LanguagePreference>(this.readLanguagePreference());

  readonly theme = this.themePreference.asReadonly();
  readonly language = this.languagePreference.asReadonly();

  constructor() {
    this.applyTheme(this.themePreference());
    this.applyLanguage(this.languagePreference());
    this.systemThemeQuery?.addEventListener('change', () => {
      if (this.themePreference() === 'system') {
        this.applyTheme('system');
      }
    });
  }

  setTheme(theme: ThemePreference): void {
    this.themePreference.set(theme);
    this.setStorageValue(THEME_STORAGE_KEY, theme);
    this.applyTheme(theme);
  }

  setLanguage(language: LanguagePreference): void {
    this.languagePreference.set(language);
    this.setStorageValue(LANGUAGE_STORAGE_KEY, language);
    this.applyLanguage(language);
  }

  private applyTheme(theme: ThemePreference): void {
    const resolvedTheme = theme === 'system' ? this.getSystemTheme() : theme;
    const root = globalThis.document?.documentElement;

    if (!root) {
      return;
    }

    root.dataset['themePreference'] = theme;
    root.dataset['theme'] = resolvedTheme;
  }

  private applyLanguage(language: LanguagePreference): void {
    this.i18n.setLocale(language as AppLocale);
  }

  private readThemePreference(): ThemePreference {
    const storedTheme = this.getStorageValue(THEME_STORAGE_KEY);
    return this.isThemePreference(storedTheme) ? storedTheme : 'system';
  }

  private readLanguagePreference(): LanguagePreference {
    const storedLanguage = this.getStorageValue(LANGUAGE_STORAGE_KEY);

    if (this.isLanguagePreference(storedLanguage)) {
      return storedLanguage;
    }

    const currentLocale = this.i18n.currentLocale();
    return this.isLanguagePreference(currentLocale) ? currentLocale : 'en';
  }

  private getSystemTheme(): 'light' | 'dark' {
    return this.systemThemeQuery?.matches ? 'dark' : 'light';
  }

  private getSystemThemeQuery(): MediaQueryList | null {
    return globalThis.matchMedia?.('(prefers-color-scheme: dark)') ?? null;
  }

  private getStorageValue(key: string): string | null {
    try {
      return globalThis.localStorage?.getItem(key) ?? null;
    } catch {
      return null;
    }
  }

  private setStorageValue(key: string, value: string): void {
    try {
      globalThis.localStorage?.setItem(key, value);
    } catch {
      // Preference persistence is best-effort; the in-memory state is already updated.
    }
  }

  private isThemePreference(value: string | null | undefined): value is ThemePreference {
    return THEME_VALUES.includes(value as ThemePreference);
  }

  private isLanguagePreference(value: string | null | undefined): value is LanguagePreference {
    return LANGUAGE_VALUES.includes(value as LanguagePreference);
  }
}
