import { Injectable, signal } from '@angular/core';

import { AppLocale, DEFAULT_LOCALE, SUPPORTED_LOCALES } from './app-locale';
import { TRANSLATIONS, TranslationKey } from './translations';

type TranslationParams = Record<string, string | number | boolean | null | undefined>;

const LOCALE_STORAGE_KEY = 'interesme_language';
const LEGACY_LOCALE_STORAGE_KEY = 'interesme.locale';

@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly locale = signal<AppLocale>(this.getInitialLocale());

  readonly currentLocale = this.locale.asReadonly();

  setLocale(locale: AppLocale): void {
    if (!SUPPORTED_LOCALES.includes(locale)) {
      return;
    }

    this.locale.set(locale);
    this.persistLocale(locale);
  }

  translate(key: TranslationKey, params?: TranslationParams): string {
    const locale = this.locale();
    const value = TRANSLATIONS[locale][key] ?? TRANSLATIONS[DEFAULT_LOCALE][key] ?? key;
    return this.interpolate(value, params);
  }

  t(key: TranslationKey, params?: TranslationParams): string {
    return this.translate(key, params);
  }

  private getInitialLocale(): AppLocale {
    const storedLocale = this.getStoredLocale();

    if (storedLocale) {
      return storedLocale;
    }

    const browserLanguage = globalThis.navigator?.language;
    return browserLanguage?.toLowerCase().startsWith('uk') ? 'uk' : DEFAULT_LOCALE;
  }

  private getStoredLocale(): AppLocale | null {
    try {
      const storedLocale = globalThis.localStorage?.getItem(LOCALE_STORAGE_KEY);
      if (this.isSupportedLocale(storedLocale)) {
        return storedLocale;
      }

      const legacyStoredLocale = globalThis.localStorage?.getItem(LEGACY_LOCALE_STORAGE_KEY);
      return this.isSupportedLocale(legacyStoredLocale) ? legacyStoredLocale : null;
    } catch {
      return null;
    }
  }

  private persistLocale(locale: AppLocale): void {
    try {
      globalThis.localStorage?.setItem(LOCALE_STORAGE_KEY, locale);
    } catch {
      // Locale persistence is a convenience; the in-memory signal still works.
    }
  }

  private isSupportedLocale(locale: string | null | undefined): locale is AppLocale {
    return SUPPORTED_LOCALES.includes(locale as AppLocale);
  }

  private interpolate(value: string, params?: TranslationParams): string {
    if (!params) {
      return value;
    }

    return value.replace(/\{(\w+)}/g, (match, key: string) => {
      const replacement = params[key];
      return replacement === null || replacement === undefined ? match : String(replacement);
    });
  }
}
