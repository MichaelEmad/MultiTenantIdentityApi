import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';

export type Language = 'en' | 'ar';

interface TranslationData {
  [key: string]: any;
}

@Injectable({
  providedIn: 'root'
})
export class TranslationService {
  private translations: { [lang: string]: TranslationData } = {};
  private currentLanguageSubject = new BehaviorSubject<Language>('en');

  currentLanguage = signal<Language>('en');
  currentLanguage$ = this.currentLanguageSubject.asObservable();

  constructor(private http: HttpClient) {
    this.initLanguage();
  }

  private initLanguage(): void {
    const savedLang = localStorage.getItem('language') as Language;
    const defaultLang: Language = savedLang || 'en';
    this.setLanguage(defaultLang);
  }

  setLanguage(lang: Language): void {
    this.loadTranslations(lang).subscribe({
      next: (translations) => {
        this.translations[lang] = translations;
        this.currentLanguage.set(lang);
        this.currentLanguageSubject.next(lang);
        localStorage.setItem('language', lang);

        // Set document direction for RTL support
        document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
        document.documentElement.lang = lang;
      },
      error: (error) => {
        console.error(`Failed to load translations for ${lang}:`, error);
      }
    });
  }

  private loadTranslations(lang: Language): Observable<TranslationData> {
    return this.http.get<TranslationData>(`/assets/i18n/${lang}.json`);
  }

  translate(key: string, params?: { [key: string]: string | number }): string {
    const lang = this.currentLanguage();
    const translation = this.getTranslation(key, lang);

    if (!translation) {
      console.warn(`Translation not found for key: ${key}`);
      return key;
    }

    // Replace parameters in translation
    if (params) {
      return this.interpolate(translation, params);
    }

    return translation;
  }

  private getTranslation(key: string, lang: Language): string | null {
    const keys = key.split('.');
    let result: any = this.translations[lang];

    for (const k of keys) {
      if (result && typeof result === 'object') {
        result = result[k];
      } else {
        return null;
      }
    }

    return typeof result === 'string' ? result : null;
  }

  private interpolate(text: string, params: { [key: string]: string | number }): string {
    return text.replace(/\{\{(\w+)\}\}/g, (match, key) => {
      return params[key]?.toString() || match;
    });
  }

  getLanguage(): Language {
    return this.currentLanguage();
  }

  isRTL(): boolean {
    return this.currentLanguage() === 'ar';
  }
}
